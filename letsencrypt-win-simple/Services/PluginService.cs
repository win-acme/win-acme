using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
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

        private readonly List<Type> _targetFactories;
        private readonly List<Type> _validationFactories;
        private readonly List<Type> _storeFactories;
        private readonly List<Type> _installationFactories;

        private readonly List<Type> _target;
        private readonly List<Type> _validation;
        private readonly List<Type> _store;
        private readonly List<Type> _installation;

        private readonly ILogService _log;

        public List<ITargetPluginFactory> TargetPluginFactories(ILifetimeScope scope)
        {
            return GetFactories<ITargetPluginFactory>(_targetFactories, scope);
        }

        public List<IValidationPluginFactory> ValidationPluginFactories(ILifetimeScope scope)
        {
            return GetFactories<IValidationPluginFactory>(_validationFactories, scope);
        }

        public List<Type> StorePluginOptionTypes()
        {
            return GetResolvable<StorePluginOptions>();
        }

        public List<IStorePluginFactory> StorePluginFactories(ILifetimeScope scope)
        {
            return GetFactories<IStorePluginFactory>(_storeFactories, scope);
        }

        public List<IInstallationPluginFactory> InstallationPluginFactories(ILifetimeScope scope)
        {
            return GetFactories<IInstallationPluginFactory>(_installationFactories, scope);
        }

        public ITargetPluginFactory TargetPluginFactory(ILifetimeScope scope, string name)
        {
            return GetByName<ITargetPluginFactory>(_targetFactories, name, scope);
        }

        public IValidationPluginFactory ValidationPluginFactory(ILifetimeScope scope, string full)
        {
            var split = full.Split('.');
            var name = split[1];
            var type = split[0];
            return _validationFactories.
                Select(scope.Resolve).
                OfType<IValidationPluginFactory>().
                FirstOrDefault(x => x.Match(name) && string.Equals(type, x.ChallengeType, StringComparison.InvariantCultureIgnoreCase));
        }

        public IStorePluginFactory StorePluginFactory(ILifetimeScope scope, string name)
        {
            return GetByName<IStorePluginFactory>(_storeFactories, name, scope);
        }

        public IInstallationPluginFactory InstallationPluginFactory(ILifetimeScope scope, string name)
        {
            return GetByName<IInstallationPluginFactory>(_installationFactories, name, scope);
        }

        internal void Configure(ContainerBuilder builder)
        {
            _targetFactories.ForEach(t => { builder.RegisterType(t).SingleInstance(); });
            _validationFactories.ForEach(t => { builder.RegisterType(t).SingleInstance(); });
            _storeFactories.ForEach(t => { builder.RegisterType(t).SingleInstance(); });
            _installationFactories.ForEach(t => { builder.RegisterType(t).SingleInstance();});

            _target.ForEach(ip => { builder.RegisterType(ip); });
            _validation.ForEach(ip => { builder.RegisterType(ip); });
            _store.ForEach(ip => { builder.RegisterType(ip); });
            _installation.ForEach(ip => { builder.RegisterType(ip); });
        }

        private List<T> GetFactories<T>(List<Type> source, ILifetimeScope scope) where T : IHasName, IHasType
        {
            return source.Select(scope.Resolve).OfType<T>().ToList();
        }

        private T GetByName<T>(IEnumerable<Type> list, string name, ILifetimeScope scope) where T: IHasName
        {
            return list.Select(scope.Resolve).OfType<T>().FirstOrDefault(x => x.Match(name));
        }

        public PluginService(ILogService logger)
        {
            _log = logger;
            _allTypes = GetTypes();

            _targetFactories = GetResolvable<ITargetPluginFactory>();
            _validationFactories = GetResolvable<IValidationPluginFactory>();
            _storeFactories = GetResolvable<IStorePluginFactory>();
            _installationFactories = GetResolvable<IInstallationPluginFactory>(true);

            _target = GetResolvable<ITargetPlugin>();
            _validation = GetResolvable<IValidationPlugin>();
            _store = GetResolvable<IStorePlugin>();
            _installation = GetResolvable<IInstallationPlugin>();
        }

        private List<Type> GetTypes()
        {
            var ret = new List<Type>();
            var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
            try
            {
                // Try loading additional dlls in the current dir to attempt to find plugin types in them
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var allFiles = Directory.EnumerateFileSystemEntries(baseDir, "*.dll", SearchOption.TopDirectoryOnly);
                assemblies.AddRange(allFiles.Select(AssemblyName.GetAssemblyName).Select(Assembly.Load));
            }
            catch (Exception ex)
            {
                _log.Error("Error loading assemblies for plugins.", ex);
            }

            IEnumerable<Type> types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException rex)
                {
                    types = rex.Types;
                    _log.Error("Error loading some types from assembly {assembly}: {@ex}", assembly.FullName, rex);
                }
                catch (Exception ex)
                {
                    _log.Error("Error loading any types from assembly {assembly}: {@ex}", assembly.FullName, ex);
                }
                ret.AddRange(types);
            }
            return ret;
        }

        private List<Type> GetResolvable<T>(bool allowNull = false)
        {
            var ret = _allTypes.AsEnumerable();
            ret = ret.Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract);
            if (!allowNull)
            {
                ret = ret.Where(type => !typeof(INull).IsAssignableFrom(type));
            }
            return ret.ToList();
        }
    }
}
