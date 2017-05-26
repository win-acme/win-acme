using Newtonsoft.Json;
using System;

namespace letsencrypt.Support
{
    internal class ScheduledRenewal
    {
        public DateTime Date { get; set; }
        public Target Binding { get; set; }
        public string CentralSsl { get; set; }
        public string San { get; set; }
        public string KeepExisting { get; set; }
        public string Script { get; set; }
        public string ScriptParameters { get; set; }
        public bool Warmup { get; set; }

        public override string ToString() => $"{Binding}: {R.Renewafter} {Date.ToShortDateString()}";
    }
}