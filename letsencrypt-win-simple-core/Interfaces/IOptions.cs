using System.Collections.Generic;
using ACMESharp;
using LetsEncrypt.ACME.Simple.Core.Configuration;

namespace LetsEncrypt.ACME.Simple.Core.Interfaces
{
    public interface IOptions
    {
        string BaseUri { get; set; }
        bool UseDefaultTaskUser { get; set; }
        string EmailAddress { get; set; }
        bool AcceptTos { get; set; }
        string SignerEmail { get; set; }
        bool Renew { get; set; }
        bool Test { get; set; }
        string ManualHost { get; set; }
        string WebRoot { get; set; }
        string Script { get; set; }
        string ScriptParameters { get; set; }
        string CentralSslStore { get; set; }
        string CertOutPath { get; set; }
        bool HideHttps { get; set; }
        bool San { get; set; }
        bool KeepExisting { get; set; }
        bool Warmup { get; set; }
        string Plugin { get; set; }
        string Proxy { get; set; }
        float RenewalPeriodDays { get; set; }
        string CertificateStore { get; set; }
        bool CentralSsl { get; set; }
        Settings Settings { get; set; }
        string ClientName { get; set; }
        string ConfigPath { get; set; }
        AcmeClient AcmeClient { get; set; }
        int HostsPerPage { get; set; }
        Dictionary<string, IPlugin> Plugins { get; set; }
    }
}