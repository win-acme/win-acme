using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Clients;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using LetsEncrypt.ACME.Simple.Extensions;

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
        [JsonIgnore] public List<RenewResult> History { get; set; }

        public override string ToString() => $"{Binding?.Host ?? "[unknown]"} - renew after {Date.ToUserString()}";

        internal string Save(string path)
        {
            // Save history to file system
            File.WriteAllText(HistoryFile(Binding, path).FullName, JsonConvert.SerializeObject(History));
            return JsonConvert.SerializeObject(this);
        }

        internal static FileInfo HistoryFile(Target target, string configPath)
        {
            return new FileInfo(Path.Combine(configPath, $"{target.Host}.history.json"));
        }
    }

    public class RenewResult
    {
        public DateTime Date { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Thumbprint { get; set; }

        private RenewResult()
        {
            Date = DateTime.Now;
        }

        public RenewResult(X509Certificate2 certificate) : this()
        {
            Success = true;
            Thumbprint = certificate.Thumbprint;
        }

        public RenewResult(Exception ex) : this()
        {
            Success = false;
            ErrorMessage = ex.Message;
        }

        public override string ToString() => $"{Date.ToString(Properties.Settings.Default.FileDateFormat)} " +
            $"- {(Success ? "Success" : "Error")}" +
            $"{(string.IsNullOrEmpty(Thumbprint) ? "" : $" - Thumbprint {Thumbprint}")}" +
            $"{(string.IsNullOrEmpty(ErrorMessage) ? "" : $" - {ErrorMessage.ReplaceNewLines()}")}";

    }
}
