using Autofac;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace PKISharp.WACS.Services
{
    public class PluginService : IPluginService
    {
        private readonly List<Type> _allTypes;
        private readonly List<Type> _argumentGroups;
        private readonly List<Type> _optionFactories;
        private readonly List<Type> _plugins;
        
        internal readonly ILogService _log;

        public IEnumerable<IArgumentsProvider> ArgumentsProviders()
        {
            if (_argumentsProviderCache == null)
            {
                _argumentsProviderCache = new List<IArgumentsProvider>();
                _argumentsProviderCache.AddRange(_argumentGroups.
                    Select(x =>
                    {
                        var type = typeof(BaseArgumentsProvider<>).MakeGenericType(x);
                        var constr = type.GetConstructor(Array.Empty<Type>());
                        if (constr == null)
                        {
                            throw new Exception("IArgumentsProvider should have parameterless constructor");
                        }
                        var ret = (IArgumentsProvider)constr.Invoke(Array.Empty<object>());
                        ret.Log = _log;
                        return ret;
                    }));
            }
            return _argumentsProviderCache;
        }
        private List<IArgumentsProvider>? _argumentsProviderCache = null;


        public IEnumerable<Type> PluginOptionTypes<T>() where T : PluginOptions => GetResolvable<T>();

        internal void Configure(ContainerBuilder builder)
        {
            _optionFactories.ForEach(t => builder.RegisterType(t).SingleInstance());
            _plugins.ForEach(ip => builder.RegisterType(ip));
        }

        public IEnumerable<T> GetFactories<T>(ILifetimeScope scope) where T : IPluginOptionsFactory => _optionFactories.Select(scope.Resolve).OfType<T>().OrderBy(x => x.Order).ToList();

        private IEnumerable<T> GetByName<T>(string name, ILifetimeScope scope) where T : IPluginOptionsFactory => GetFactories<T>(scope).Where(x => x.Match(name));

        public PluginService(ILogService logger)
        {
            _log = logger;
            _allTypes = GetTypes();
            _argumentGroups = GetResolvable<IArguments>();
            _optionFactories = GetResolvable<IPluginOptionsFactory>(true);
            _plugins = new List<Type>();
            void AddPluginType<T>(string name)
            {
                var temp = GetResolvable<T>();
                ListPlugins(temp, name);
                _plugins.AddRange(temp);
            }
            AddPluginType<ITargetPlugin>("target");
            AddPluginType<IValidationPlugin>("validation");
            AddPluginType<IOrderPlugin>("order");
            AddPluginType<ICsrPlugin>("csr");
            AddPluginType<IStorePlugin>("store");
            AddPluginType<IInstallationPlugin>("installation");
        }

        private void ListPlugins(IEnumerable<Type> list, string type)
        {
            _ = list.Where(x => x.Assembly != typeof(PluginService).Assembly).
                All(x =>
                {
                    _log.Verbose("Loaded {type} plugin {name} from {location}", type, x.Name, x.Assembly.Location);
                    return false;
                });
        }

        internal IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly.DefinedTypes == null)
            {
                return new List<Type>();
            }
            return assembly.DefinedTypes.
                Where(x =>
                {
                    if (!string.IsNullOrEmpty(x.FullName) &&
                        x.FullName.StartsWith("PKISharp"))
                    {
                        return true;
                    }
                    if (x.ImplementedInterfaces != null)
                    {
                        if (x.ImplementedInterfaces.Any(x =>
                            !string.IsNullOrEmpty(x.FullName) &&
                            x.FullName.StartsWith("PKISharp")))
                        {
                            return true;
                        }

                    }
                    return false;
                }
                ).
                Select(x =>
                {
                    try
                    {
                        return x.AsType();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error loading type {x}", x.FullName);
                        throw;
                    }
                }
                );
        }

        internal virtual List<Type> GetTypes()
        {
            var scanned = new List<Assembly>();
            var ret = new List<Type>();

            // Load from the current app domain
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.IsNullOrEmpty(assembly.FullName) && assembly.FullName.Contains("wacs"))
                {
                    IEnumerable<Type> types = new List<Type>();
                    try
                    {
                        types = GetTypesFromAssembly(assembly).ToList();
                    }
                    catch (ReflectionTypeLoadException rex)
                    {
                        types = rex.Types.OfType<Type>();
                        foreach (var lex in rex.LoaderExceptions.OfType<Exception>())
                        {
                            _log.Error(lex, "Error loading type from {assembly}: {reason}", assembly.FullName, lex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Error loading types from assembly {assembly}: {@ex}", assembly.FullName, ex);
                    }
                    ret.AddRange(types);
                }
                scanned.Add(assembly);
            }

            // Load external plugins from additional .dll files
            // put in the application folder by the user
            if (!string.IsNullOrEmpty(VersionService.PluginPath))
            {
                ret.AddRange(LoadFromDisk(scanned));
            } 

            return ret;
        }

        private static readonly List<string> IgnoreLibraries = new() { 
            "clrcompression.dll", 
            "clrjit.dll",
            "coreclr.dll",
            "mscordaccore.dll"
        };

        private List<Type> LoadFromDisk(List<Assembly> scanned)
        {
            var pluginDirectory = new DirectoryInfo(VersionService.PluginPath);
            if (!pluginDirectory.Exists)
            {
                return new List<Type>();
            }
            var dllFiles = pluginDirectory.
                EnumerateFiles("*.dll", SearchOption.AllDirectories).
                Where(x => !IgnoreLibraries.Contains(x.Name));
            if (!VersionService.Pluggable)
            {
                if (dllFiles.Any())
                {
                    _log.Error("This version of the program does not support external plugins, please download the pluggable version.");
                }
                return new List<Type>();
            }

            var allAssemblies = new List<Assembly>();
            foreach (var file in dllFiles)
            {
                try
                {
                    allAssemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName));
                }
                catch (BadImageFormatException)
                {
                    // Not a .NET Assembly (likely runtime)
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error loading assembly {assembly}", file);
                }
            }

            var ret = new List<Type>();
            foreach (var assembly in allAssemblies)
            {
                IEnumerable<Type> types = new List<Type>();
                try
                {
                    if (!scanned.Contains(assembly))
                    {
                        types = GetTypesFromAssembly(assembly).ToList();
                    }
                }
                catch (ReflectionTypeLoadException rex)
                {
                    types = rex.Types.OfType<Type>();
                    foreach (var lex in rex.LoaderExceptions.OfType<Exception>())
                    {
                        _log.Error(lex, "Error loading type from {assembly}", assembly.FullName);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error loading types from assembly {assembly}", assembly.FullName);
                }
                ret.AddRange(types);
            }
            return ret;
        }

        private List<Type> GetResolvable<T>(bool allowNull = false)
        {
            var ret = _allTypes.AsEnumerable();
            ret = ret.Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract);
            ret = ret.Where(type => !typeof(IIgnore).IsAssignableFrom(type));
            if (!allowNull)
            {
                ret = ret.Where(type => !typeof(INull).IsAssignableFrom(type));
            }
            return ret.ToList();
        }

        public T? GetFactory<T>(ILifetimeScope scope, string name, string? parameter = null) where T : IPluginOptionsFactory
        {
            var plugins = GetByName<T>(name, scope);
            if (typeof(T) == typeof(IValidationPluginOptionsFactory))
            {
                plugins = plugins.Where(x => string.Equals(parameter, (x as IValidationPluginOptionsFactory)?.ChallengeType, StringComparison.InvariantCultureIgnoreCase));
            }
            return plugins.FirstOrDefault();
        }
    }
}
