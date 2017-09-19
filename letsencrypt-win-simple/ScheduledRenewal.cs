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
        //public AzureOptions AzureOptions { get; set; }

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

            if (result.Binding.Plugin == null) {
                Program.Log.Error("Plugin {plugin} not found", result.Binding.PluginName);
                return null;
            }

            if (result.Binding.HostIsDns == null)
            {
                result.Binding.HostIsDns = !result.San;
            }

            try {
                ITargetPlugin target = result.GetTargetPlugin();
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
            } catch (Exception ex) {
                Program.Log.Warning("Error refreshing renewal for {host} - {@ex}", result.Binding.Host, ex);
            }

			return result;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        internal ITargetPlugin GetTargetPlugin()
        {
            switch (Binding.PluginName) {
                case IISPlugin.PluginName:
                    if (Binding.HostIsDns == false) {
                        return Program.Plugins.GetByName(Program.Plugins.Target, nameof(IISSite));
                    } else {
                        return Program.Plugins.GetByName(Program.Plugins.Target, nameof(IISBinding));
                    }
                case IISSiteServerPlugin.PluginName:
                    return Program.Plugins.GetByName(Program.Plugins.Target, nameof(IISSites));
                case nameof(Manual):
                    return Program.Plugins.GetByName(Program.Plugins.Target, nameof(Manual));
                default:
                    return null;
            }
        }
    }
}
