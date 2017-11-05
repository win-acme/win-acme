using LetsEncrypt.ACME.Simple.Plugins;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LetsEncrypt.ACME.Simple.Services
{
    class PluginService
    {
        public readonly List<ITargetPlugin> Target;
        public readonly List<IValidationPlugin> Validation;

        public readonly List<Type> Store;

        public readonly List<IInstallationPluginFactory> Installation;
        public readonly List<Type> InstallationInstance;

        public IValidationPlugin GetValidationPlugin(string full)
        {
            var split = full.Split('.');
            var name = split[1];
            var type = split[0];
            return Validation.
                Where(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase)).
                Where(x => string.Equals(x.ChallengeType, type, StringComparison.InvariantCultureIgnoreCase)).
                FirstOrDefault();
        }

        public T GetByName<T>(IEnumerable<T> list, string name) where T: IHasName
        {
            return list.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public PluginService()
        {
            Target = GetPlugins<ITargetPlugin>();
            Validation = GetPlugins<IValidationPlugin>();
            Store = GetResolvable<IStorePlugin>();
            Installation = GetPlugins<IInstallationPluginFactory>();
            InstallationInstance = GetResolvable<IInstallationPlugin>();
        }

        private List<T> GetPlugins<T>() {
            return Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract)
                        .Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null))
                        .Cast<T>()
                        .ToList();
        }

        private List<Type> GetResolvable<T>()
        {
            return Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract)
                        .ToList();
        }

    }
}
