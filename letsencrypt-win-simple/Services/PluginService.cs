using LetsEncrypt.ACME.Simple.Plugins;
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
        public readonly List<Plugin> Legacy;
        public readonly List<ITargetPlugin> Target;
        public readonly List<IValidationPlugin> Validation;

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
            Legacy = GetPlugins<Plugin>();
            Target = GetPlugins<ITargetPlugin>();
            Validation = GetPlugins<IValidationPlugin>();
        }

        private List<T> GetPlugins<T>() {
            return Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(type => typeof(T) != type && typeof(T).IsAssignableFrom(type) && !type.IsAbstract)
                        .Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null))
                        .Cast<T>()
                        .ToList();
        }
            
    }
}
