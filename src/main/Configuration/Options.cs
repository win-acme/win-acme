using CommandLine;
using PKISharp.WACS.Clients.IIS;
using System.Collections.Generic;

namespace PKISharp.WACS
{
    public class Options
    {
        #region Basic 

        public string BaseUri { get; set; }
        public bool Test { get; set; }
        public bool Import { get; set; }
        public string ImportBaseUri { get; set; }
        public bool Verbose { get; set; }

        public bool Renew { get; set; }
        public bool Force { get; set; }
        public string FriendlyName { get; set; }

        #endregion

        #region Target

        public string Target { get; set; }
        public string SiteId { get; set; }
        public string CommonName { get; set; }
        public string ExcludeBindings { get; set; }
        public bool HideHttps { get; set; }
        public string Host { get; set; }
        public bool ManualTargetIsIIS { get; set; }

        #endregion

        #region Validation

        public string Validation { get; set; }
        public string ValidationMode { get; set; }
        public string WebRoot { get; set; }
        public int? ValidationPort { get; set; }
        public string ValidationSiteId { get; set; }
        public bool Warmup { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DnsCreateScript { get; set; }
        public string DnsDeleteScript { get; set; }

        [Option(HelpText = "[--validationmode dns-01 --validation azure] Tenant ID to login into Microsoft Azure.")]
        public string AzureTenantId { get; set; }

        [Option(HelpText = "[--validationmode dns-01 --validation azure] Client ID to login into Microsoft Azure.")]
        public string AzureClientId { get; set; }

        [Option(HelpText = "[--validationmode dns-01 --validation azure] Secret to login into Microsoft Azure.")]
        public string AzureSecret { get; set; }

        [Option(HelpText = "[--validationmode dns-01 --validation azure] Subscription ID to login into Microsoft Azure DNS.")]
        public string AzureSubscriptionId { get; set; }

        [Option(HelpText = "[--validationmode dns-01 --validation azure] The name of the resource group within Microsoft Azure DNS.")]
        public string AzureResourceGroupName { get; set; }
        
        #endregion

        #region Store
        
        public string Store { get; set; }
        public bool KeepExisting { get; set; }        
        public string CentralSslStore { get; set; }
        public string PfxPassword { get; set; }        
        public string CertificateStore { get; set; }

        #endregion

        #region Installation
        
        public string Installation { get; set; }
        public string InstallationSiteId { get; set; }
        public string FtpSiteId { get; set; }
        public int SSLPort { get; set; }
        public string SSLIPAddress { get; set; }
        public string Script { get; set; }
        public string ScriptParameters { get; set; }

        #endregion

        #region Other 
        
        public bool CloseOnFinish { get; set; }
        public bool NoTaskScheduler { get; set; }
        public bool UseDefaultTaskUser { get; set; }
        public bool Cancel { get; set; }
        public bool AcceptTos { get; set; }
        public string EmailAddress { get; set; }

        #endregion
    }
}
