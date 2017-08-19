using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple
{
    public class ScheduledRenewal
    {
        public DateTime Date { get; set; }
        public Target Binding { get; set; }
        public string CentralSsl { get; set; }
        public string San { get; set; }
        public string KeepExisting { get; set; }
        public string Script { get; set; }
        public string ScriptParameters { get; set; }
        public bool Warmup { get; set; }
        //public AzureOptions AzureOptions { get; set; }

        public override string ToString() => $"{Binding} - renew after {Date.ToShortDateString()}";

        internal string Save()
        {
            return JsonConvert.SerializeObject(this);
        }

        internal static ScheduledRenewal Load(string renewal)
        {
            var result = JsonConvert.DeserializeObject<ScheduledRenewal>(renewal);

			if (result == null || result.Binding == null) {
                Log.Error("Unable to deserialize renewal {renewal}", renewal);
                return null;
            }

            if (result.Binding.AlternativeNames == null)
            {
                result.Binding.AlternativeNames = new List<string>();
            }

            if (result.Binding.Plugin == null) {
                Log.Error("Plugin {plugin} not found", result.Binding.PluginName);
                return null;
            }

            try {
                result.Binding.Plugin.Refresh(result);
            } catch (Exception ex) {
                Log.Warning("Error refreshing renewal for {host} - {@ex}", result.Binding.Host, ex);
            }

			return result;
        }
    }
}
