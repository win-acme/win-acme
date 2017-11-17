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
    public class PluginService
    {
        public readonly List<ITargetPluginFactory> Target;
        public readonly List<IValidationPluginFactory> Validation;
        public readonly List<IStorePluginFactory> Store;
        public readonly List<IInstallationPluginFactory> Installation;

        public readonly List<Type> TargetInstance;
        public readonly List<Type> ValidationInstance;
        public readonly List<Type> StoreInstance;
        public readonly List<Type> InstallationInstance;

        public IValidationPluginFactory GetValidationPlugin(string full)
        {
            var split = full.Split('.');
            var name = split[1];
            var type = split[0];
            return Validation.
                Where(x => x.Match(name)).
                Where(x => string.Equals(x.ChallengeType, type, StringComparison.InvariantCultureIgnoreCase)).
                FirstOrDefault();
        }

        public T GetByName<T>(IEnumerable<T> list, string name) where T: IHasName
        {
            return list.FirstOrDefault(x => x.Match(name));
        }

        public PluginService()
        {
            Target = GetPlugins<ITargetPluginFactory>();
            Validation = GetPlugins<IValidationPluginFactory>();
            Store = GetPlugins<IStorePluginFactory>();
            Installation = GetPlugins<IInstallationPluginFactory>(true);

            TargetInstance = GetResolvable<ITargetPlugin>();
            ValidationInstance = GetResolvable<IValidationPlugin>();
            StoreInstance = GetResolvable<IStorePlugin>();
            InstallationInstance = GetResolvable<IInstallationPlugin>();
        }

        private List<T> GetPlugins<T>(bool allowNull = false) {
            var ret = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract);
            if (!allowNull)
            {
                ret = ret.Where(type => !typeof(INull).IsAssignableFrom(type));
            }
            return ret.
                Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null)).
                Cast<T>().
                ToList();
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
