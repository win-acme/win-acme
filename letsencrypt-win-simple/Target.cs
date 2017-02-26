using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LetsEncrypt.ACME.Simple.Configuration;
using Serilog;

namespace LetsEncrypt.ACME.Simple
{
    public class Target
    {
        public static Dictionary<string, Plugin> Plugins = new Dictionary<string, Plugin>();

        static Target()
        {
            foreach (
                var pluginType in
                    (from t in Assembly.GetExecutingAssembly().GetTypes() where t.BaseType == typeof(Plugin) select t))
            {
                AddPlugin(pluginType);
            }
        }

        static void AddPlugin(Type type)
        {
            var plugin = type.GetConstructor(new Type[] { }).Invoke(null) as Plugin;
            Plugins.Add(plugin.Name, plugin);
        }

        public string Host { get; set; }
        public string WebRootPath { get; set; }
        public long SiteId { get; set; }
        public List<string> AlternativeNames { get; set; }
        public string PluginName { get; set; } = "IIS";
        public Plugin Plugin => Plugins[PluginName];

        public override string ToString() => $"{PluginName} {Host} ({WebRootPath})";

        public static List<Target> GetTargetsSorted()
        {
            var targets = new List<Target>();
            if (!string.IsNullOrEmpty(App.Options.ManualHost))
                return targets;

            foreach (var plugin in Plugins.Values)
            {
                targets.AddRange(!App.Options.San ? plugin.GetTargets() : plugin.GetSites());
            }

            return targets.OrderBy(p => p.ToString()).ToList();
        }

        public static void WriteBindings(List<Target> targets)
        {
            if (targets.Count == 0 && string.IsNullOrEmpty(App.Options.ManualHost))
                Log.Error("No targets found.");
            else
            {
                int hostsPerPage = App.Options.HostsPerPage;

                if (targets.Count > hostsPerPage)
                    WriteBindingsFromTargetsPaged(targets, hostsPerPage, 1);
                else
                    WriteBindingsFromTargetsPaged(targets, targets.Count, 1);
            }
        }

        private static void WriteBindingsFromTargetsPaged(List<Target> targets, int pageSize, int fromNumber)
        {
            do
            {
                int toNumber = fromNumber + pageSize;
                if (toNumber <= targets.Count)
                    fromNumber = WriteBindingsFomTargets(targets, toNumber, fromNumber);
                else
                    fromNumber = WriteBindingsFomTargets(targets, targets.Count + 1, fromNumber);

                if (fromNumber < targets.Count)
                {
                    App.ConsoleService.WriteQuitCommandInformation();
                    string command = App.ConsoleService.ReadCommandFromConsole();
                    switch (command)
                    {
                        case "q":
                            throw new Exception($"Requested to quit application");
                        default:
                            break;
                    }
                }
            } while (fromNumber < targets.Count);
        }

        private static int WriteBindingsFomTargets(List<Target> targets, int toNumber, int fromNumber)
        {
            for (int i = fromNumber; i < toNumber; i++)
            {
                if (!App.Options.San)
                {
                    Log.Information($" {i}: {targets[i - 1]}");
                }
                else
                {
                    Log.Information($" {targets[i - 1].SiteId}: SAN - {targets[i - 1]}");
                }
                fromNumber++;
            }

            return fromNumber;
        }

        public static void ProcessDefaultCommand(List<Target> targets, string command)
        {
            var targetId = 0;
            if (Int32.TryParse(command, out targetId))
            {
                App.CertificateService.GetCertificateForTargetId(targets, targetId);
                return;
            }

            HandleMenuResponseForPlugins(targets, command);
        }

        private static void HandleMenuResponseForPlugins(List<Target> targets, string command)
        {
            // Only run the plugin specified in the config
            if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
            {
                var plugin = Target.Plugins.Values.FirstOrDefault(x => string.Equals(x.Name, App.Options.Plugin, StringComparison.InvariantCultureIgnoreCase));
                if (plugin != null)
                    plugin.HandleMenuResponse(command, targets);
                else
                {
                    Log.Information("Plugin '{AppOptionsPlugin}' could not be found.", App.Options.Plugin);
                    App.ConsoleService.PromptEnter("Press enter to exit");
                }
            }
            else
            {
                foreach (var plugin in Target.Plugins.Values)
                    plugin.HandleMenuResponse(command, targets);
            }
        }
    }
}