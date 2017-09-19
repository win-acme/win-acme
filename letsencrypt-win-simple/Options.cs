using CommandLine;
using System.ComponentModel;

namespace LetsEncrypt.ACME.Simple
{
    public class Options
    {
        #region Basic 

        [Option(HelpText = "Warm up websites before attempting HTTP authorization")]
        public bool Warmup { get; set; }

        [Option(HelpText = "The address of the ACME server to use.", Default = "https://acme-v01.api.letsencrypt.org/")]
        public string BaseUri { get; set; }

        [Option(HelpText = "Overrides --baseuri setting to https://acme-staging.api.letsencrypt.org/ and enables other testing behaviours in the program which may help with troubleshooting.")]
        public bool Test { get; set; }

        [Option(HelpText = "Print additional log messages to console for troubleshooting.")]
        public bool Verbose { get; set; }

        #region Renew 

        [Option(HelpText = "Check for scheduled renewals.")]
        public bool Renew { get; set; }

        [Option(HelpText = "Force renewal on all scheduled certificates.")]
        public bool ForceRenewal { get; set; }

        [Option(HelpText = "Keep existing bindings and certificates.")]
        public bool KeepExisting { get; set; }

        #endregion

        #endregion

        #region Plugins

        [Option(HelpText = "[--plugin manual] Path to script to run after retrieving the certificate.")]
        public string Script { get; set; }

        [Option(HelpText = "[--plugin manual] Parameters for the script to run after retrieving the certificate.")]
        public string ScriptParameters { get; set; }

        #region IIS

        [Option(HelpText = "Hide sites that have existing HTTPS bindings")]
        public bool HideHttps { get; set; }

        [Option(HelpText = "Path to a centralized certificate store, which may be on a network drive. When using this setting, certificate files are stored there instead of in the --configpath.")]
        public string CentralSslStore { get; set; }
        internal bool CentralSsl
        {
            get
            {
                return !string.IsNullOrWhiteSpace(CentralSslStore);
            }
        }

        #endregion

        #region DNS

        //[Option(HelpText = "Tenant ID to login into Microsoft Azure.")]
        //public string AzureTenantId { get; set; }

        //[Option(HelpText = "Client ID to login into Microsoft Azure.")]
        //public string AzureClientId { get; set; }

        //[Option(HelpText = "Secret to login into Microsoft Azure.")]
        //public string AzureSecret { get; set; }

        //[Option(HelpText = "Subscription ID to login into Microsoft Azure DNS.")]
        //public string AzureSubscriptionId { get; set; }

        //[Option(HelpText = "The name of the resource group within Microsoft Azure DNS.")]
        //public string AzureResourceGroupName { get; set; }

        #endregion

        #endregion

        #region Unattended 

        [Option(HelpText = "Specify which plugin to run, bypassing the main menu and triggering unattended mode. Main plugins are 'iissite' and 'manual'.")]
        public string Plugin { get; set; }

        [Option(HelpText = "[--plugin manual] A host name to manually get a certificate for. This may be a comma separated list.")]
        public string ManualHost { get; set; }

        [Option(Default = WebRootDefault, HelpText = "[--plugin manual] A web root for the manual host name for authentication.")]
        public string WebRoot { get; set; }
        public const string WebRootDefault = "%SystemDrive%\\inetpub\\wwwroot";

        [Option(HelpText = "[--plugin iissite|iissites] Specify identifier for which site a plugin should run.For the iissites plugin this may be a comma separated list.")]
        public string SiteId { get; set; }

        [Option(HelpText = "[--plugin iissite|iissites] Exclude some bindings from being included in the certificate (comma separated).")]
        public string ExcludeBindings { get; set; }

        [Option(HelpText = "Close the application when complete, avoiding the `Press any key to continue` and `Would you like to start again` messages.")]
        public bool CloseOnFinish { get; set; }

        [Option(HelpText = "Do not create (or offer to update) the scheduled task.")]
        public bool NoTaskScheduler { get; set; }

        [Option(HelpText = "Avoid the question about specifying the task scheduler user, as such defaulting to the current principal.")]
        public bool UseDefaultTaskUser { get; set; }

        #region AcmeRegistration 

        [Option(HelpText = "Accept the ACME terms of service.")]
        public bool AcceptTos { get; set; }

        [Option(HelpText = "Email address to use by ACME for renewal fail notices.")]
        public string EmailAddress { get; set; }

        #endregion

        #endregion

    }
}
