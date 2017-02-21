using Newtonsoft.Json;
using System;

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
        public string ManualHost { get; internal set; }
        public bool Renew { get; set; } = false;

        public override string ToString() => $"{Binding} Renew After {Date.ToShortDateString()}";

        internal string Save()
        {
            return JsonConvert.SerializeObject(this);
        }

        internal static ScheduledRenewal Load(string renewal)
        {
            return JsonConvert.DeserializeObject<ScheduledRenewal>(renewal);
        }
    }
}