using Autofac;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Services
{
    public class PluginService
    {
        private readonly List<Type> _allTypes;

        private readonly List<Type> _optionProviders;

        private readonly List<Type> _targetOptionFactories;
        private readonly List<Type> _validationOptionFactories;
        private readonly List<Type> _csrOptionFactories;
        private readonly List<Type> _storeOptionFactories;
        private readonly List<Type> _installationOptionFactories;

        private readonly List<Type> _target;
        private readonly List<Type> _validation;
        private readonly List<Type> _csr;
        private readonly List<Type> _store;
        private readonly List<Type> _installation;

        private readonly ILogService _log;

        public List<IArgumentsProvider> OptionProviders()
        {
            return _optionProviders.Select(x =>
            {
                var c = x.GetConstructor(new Type[] { });
                return (IArgumentsProvider)c.Invoke(new object[] { });
            }).ToList();
        }

        public List<ITargetPluginOptionsFactory> TargetPluginFactories(ILifetimeScope scope)
        {
            return GetFactories<ITargetPluginOptionsFactory>(_targetOptionFactories, scope);
        }

        public List<IValidationPluginOptionsFactory> ValidationPluginFactories(ILifetimeScope scope)
        {
            return GetFactories<IValidationPluginOptionsFactory>(_validationOptionFactories, scope);
        }

        public List<ICsrPluginOptionsFactory> CsrPluginOptionsFactories(ILifetimeScope scope)
        {
            return GetFactories<ICsrPluginOptionsFactory>(_csrOptionFactories, scope);
        }

        public List<IStorePluginOptionsFactory> StorePluginFactories(ILifetimeScope scope)
        {
            return GetFactories<IStorePluginOptionsFactory>(_storeOptionFactories, scope);
        }

        public List<IInstallationPluginOptionsFactory> InstallationPluginFactories(ILifetimeScope scope)
        {
            return GetFactories<IInstallationPluginOptionsFactory>(_installationOptionFactories, scope);
        }

        public ITargetPluginOptionsFactory TargetPluginFactory(ILifetimeScope scope, string name)
        {
            return GetByName<ITargetPluginOptionsFactory>(_targetOptionFactories, name, scope);
        }

        public IValidationPluginOptionsFactory ValidationPluginFactory(ILifetimeScope scope, string type, string name)
        {
            return _validationOptionFactories.
                Select(scope.Resolve).
                OfType<IValidationPluginOptionsFactory>().
                FirstOrDefault(x => x.Match(name) && string.Equals(type, x.ChallengeType, StringComparison.InvariantCultureIgnoreCase));
        }

        public ICsrPluginOptionsFactory CsrPluginFactory(ILifetimeScope scope, string name)
        {
            return GetByName<ICsrPluginOptionsFactory>(_csrOptionFactories, name, scope);
        }

        public IStorePluginOptionsFactory StorePluginFactory(ILifetimeScope scope, string name)
        {
            return GetByName<IStorePluginOptionsFactory>(_storeOptionFactories, name, scope);
        }

        public IInstallationPluginOptionsFactory InstallationPluginFactory(ILifetimeScope scope, string name)
        {
            return GetByName<IInstallationPluginOptionsFactory>(_installationOptionFactories, name, scope);
        }

        public List<Type> PluginOptionTypes<T>() where T: PluginOptions
        {
            return GetResolvable<T>();
        }

        internal void Configure(ContainerBuilder builder)
        {
            _targetOptionFactories.ForEach(t => { builder.RegisterType(t).SingleInstance(); });
            _validationOptionFactories.ForEach(t => { builder.RegisterType(t).SingleInstance(); });
            _csrOptionFactories.ForEach(t => { builder.RegisterType(t).SingleInstance(); });
            _storeOptionFactories.ForEach(t => { builder.RegisterType(t).SingleInstance(); });
            _installationOptionFactories.ForEach(t => { builder.RegisterType(t).SingleInstance();});

            _target.ForEach(ip => { builder.RegisterType(ip); });
            _validation.ForEach(ip => { builder.RegisterType(ip); });
            _csr.ForEach(ip => { builder.RegisterType(ip); });
            _store.ForEach(ip => { builder.RegisterType(ip); });
            _installation.ForEach(ip => { builder.RegisterType(ip); });
        }

        private List<T> GetFactories<T>(List<Type> source, ILifetimeScope scope) where T : IPluginOptionsFactory
        {
            return source.Select(scope.Resolve).OfType<T>().OrderBy(x => x.Order).ToList();
        }

        private T GetByName<T>(IEnumerable<Type> list, string name, ILifetimeScope scope) where T: IPluginOptionsFactory
        {
            return list.Select(scope.Resolve).OfType<T>().FirstOrDefault(x => x.Match(name));
        }

        public PluginService(ILogService logger)
        {
            _log = logger;
            _allTypes = GetTypes();

            _optionProviders = GetResolvable<IArgumentsProvider>();

            _targetOptionFactories = GetResolvable<ITargetPluginOptionsFactory>();
            _validationOptionFactories = GetResolvable<IValidationPluginOptionsFactory>();
            _csrOptionFactories = GetResolvable<ICsrPluginOptionsFactory>();
            _storeOptionFactories = GetResolvable<IStorePluginOptionsFactory>();
            _installationOptionFactories = GetResolvable<IInstallationPluginOptionsFactory>(true);

            _target = GetResolvable<ITargetPlugin>();
            _validation = GetResolvable<IValidationPlugin>();
            _csr = GetResolvable<ICsrPlugin>();
            _store = GetResolvable<IStorePlugin>();
            _installation = GetResolvable<IInstallationPlugin>();
        }

        private List<Type> GetTypes()
        {
            var ret = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("wacs"))
                {
                    IEnumerable<Type> types = new List<Type>();
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException rex)
                    {
                        types = rex.Types;
                        foreach (var lex in rex.LoaderExceptions)
                        {
                            _log.Error(lex, "Error loading type from {assembly}", assembly.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Error loading types from assembly {assembly}: {@ex}", assembly.FullName, ex);
                    }
                    ret.AddRange(types);
                }
            }
         
            // Try loading additional dlls in the current dir to attempt to find plugin types in them
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var allFiles = Directory.EnumerateFileSystemEntries(baseDir, "*.dll", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                IEnumerable<Type> types = new List<Type>();
                try
                {
                    var name = AssemblyName.GetAssemblyName(file);
                    var assembly = Assembly.Load(name);
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException rex)
                {
                    types = rex.Types;
                    foreach (var lex in rex.LoaderExceptions)
                    {
                        _log.Error(lex, "Error loading type from {assembly}", file);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("Error loading types from assembly {assembly}: {@ex}", file, ex);
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
    }
}
