using LetsEncrypt.ACME.Simple.Plugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Services
{
    class PluginService
    {
        public readonly List<Plugin> Legacy;
        public readonly List<ITargetPlugin> Target;
        public readonly List<IValidationPlugin> Validation;

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
