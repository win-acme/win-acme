using LetsEncrypt.ACME.Simple.Plugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
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

        public T GetByName<T>(IEnumerable<T> list, string name) where T: IHasName
        {
            return list.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public PluginService()
        {
            Legacy = Assembly.GetExecutingAssembly()
                                .GetTypes()
                                .Where(type => typeof(Plugin) != type && typeof(Plugin).IsAssignableFrom(type))
                                .Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null))
                                .Cast<Plugin>()
                                .ToList();

            Target = Assembly.GetExecutingAssembly()
                                .GetTypes()
                                .Where(type => typeof(ITargetPlugin) != type && typeof(ITargetPlugin).IsAssignableFrom(type))
                                .Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null))
                                .Cast<ITargetPlugin>()
                                .ToList();
        }
    }
}
