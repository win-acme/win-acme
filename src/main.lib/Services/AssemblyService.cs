using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace PKISharp.WACS.Services
{
    public class AssemblyService
    {
        private readonly List<Type> _allTypes;
        internal readonly ILogService _log;

        public AssemblyService(ILogService logger)
        {
            _log = logger;
            _allTypes = GetTypes();
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
                if (!string.IsNullOrEmpty(assembly.FullName) && 
                    assembly.FullName.ToLower().Contains("wacs"))
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
            "mscordbi.dll",
            "mscordaccore.dll"
        };

        protected List<Type> LoadFromDisk(List<Assembly> scanned)
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

        public List<Type> GetResolvable<T>()
        {
            return _allTypes.
                AsEnumerable().
                Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract).
                ToList();
        }
    }
}
