using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;

namespace LetsEncrypt.ACME.Simple
{
    public class ScheduledRenewal
    {
        public DateTime Date { get; set; }
        public Target Binding { get; set; }
        public string CentralSsl { get; set; }
        public bool? San { get; set; }
        public string KeepExisting { get; set; }
        public string Script { get; set; }
        public string ScriptParameters { get; set; }
        public bool Warmup { get; set; }

        public override string ToString() => $"{Binding?.Host ?? "[unknown]"} - renew after {Date.ToString(Properties.Settings.Default.FileDateFormat)}";

        internal string Save()
        {
            return JsonConvert.SerializeObject(this);
        }

        internal static ScheduledRenewal Load(string renewal)
        {
            var result = JsonConvert.DeserializeObject<ScheduledRenewal>(renewal);

			if (result == null || result.Binding == null) {
                Program.Log.Error("Unable to deserialize renewal {renewal}", renewal);
                return null;
            }

            if (result.Binding.AlternativeNames == null)
            {
                result.Binding.AlternativeNames = new List<string>();
            }

            if (result.Binding.HostIsDns == null)
            {
                result.Binding.HostIsDns = !result.San;
            }

            if (result.Binding.IIS == null)
            {
                result.Binding.IIS = !(result.Binding.PluginName == ManualPlugin.PluginName);
            }

            try {
                ITargetPlugin target = result.Binding.GetTargetPlugin();
                if (target != null)
                {
                    result.Binding = target.Refresh(Program.Options, result.Binding);
                    if (result.Binding == null)
                    {
                        // No match, return nothing, effectively cancelling the renewal
                        Program.Log.Error("Target for {result} no longer found, cancelling renewal", result);
                        return null;
                    }
                }
                else
                {
                    Program.Log.Error("TargetPlugin not found {PluginName} {TargetPluginName}", result.Binding.PluginName, result.Binding.TargetPluginName);
                    return null;
                }
            } catch (Exception ex) {
                Program.Log.Warning("Error refreshing renewal for {host} - {@ex}", result.Binding.Host, ex);
            }

			return result;
        }
    }
}
