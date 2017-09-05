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
        private readonly IReadOnlyDictionary<string, Plugin> Plugins;
        private readonly IReadOnlyDictionary<string, ITargetPlugin> TargetPlugins;

        public void ForEach(Action<Plugin> action)
        {
            Plugins.Values.ToList().ForEach(action);
        }

        public Plugin GetByName(string name)
        {
            return Plugins.Values.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public PluginService()
        {
            Plugins = Assembly.GetExecutingAssembly()
                                .GetTypes()
                                .Where(type => type.BaseType == typeof(Plugin))
                                .Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null))
                                .Cast<Plugin>()
                                .ToDictionary(plugin => plugin.Name);

            TargetPlugins = Assembly.GetExecutingAssembly()
                                .GetTypes()
                                .Where(type => type.GetInterfaces().Contains(typeof(ITargetPlugin)))
                                .Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null))
                                .Cast<ITargetPlugin>()
                                .ToDictionary(plugin => plugin.Name);
        }
    }
}
