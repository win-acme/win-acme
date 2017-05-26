using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace letsencrypt.Support
{
    public class Target
    {
        public static Dictionary<string, Plugin> Plugins = new Dictionary<string, Plugin>();

        static Target()
        {
            if (Plugins.Count == 0)
            {
                var pluginTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Plugin)));
                foreach (var pluginType in pluginTypes)
                {
                    AddPlugin(pluginType);
                }
            }
        }

        static void AddPlugin(Type type)
        {
            var plugin = type.GetConstructor(new Type[] {}).Invoke(null) as Plugin;
            Plugins.Add(plugin.Name, plugin);
        }

        public string Host { get; set; }
        public string WebRootPath { get; set; }
        public long SiteId { get; set; }
        public List<string> AlternativeNames { get; set; }
        public string PluginName { get; set; } = "IIS";

        public override string ToString() => $"{PluginName} {Host} ({WebRootPath})";
    }
}