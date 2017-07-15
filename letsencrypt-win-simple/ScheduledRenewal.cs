using Newtonsoft.Json;
using System;
using System.Linq;

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

        public override string ToString() => $"{Binding} Renew After {Date.ToShortDateString()}";

        internal string Save()
        {
            return JsonConvert.SerializeObject(this);
        }

        internal static ScheduledRenewal Load(string renewal)
        {
            var result = JsonConvert.DeserializeObject<ScheduledRenewal>(renewal);

			if (result == null || result.Binding == null)
				return result;

			if (result.Binding.PluginName == "IIS" && !Directory.Exists(result.Binding.WebRootPath)) // Web root path has changed since the initial creation of the certificate, get current path from IIS
			{
				var plugin = new IISPlugin();
				var bindings = san ? plugin.GetSites() : plugin.GetTargets();
				var matchingBinding = bindings.FirstOrDefault(binding => binding.Host == result.Binding.Host);
				if (matchingBinding != null)
					result.Binding.WebRootPath = matchingBinding.WebRootPath;
			}

			return result;
        }
    }
}
