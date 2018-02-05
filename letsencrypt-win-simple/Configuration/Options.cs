using CommandLine;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple
{
    public class Options
    {
        #region Basic 

        [Option(HelpText = "The address of the ACME server to use.", Default = "https://acme-v01.api.letsencrypt.org/")]
        public string BaseUri { get; set; }

        [Option(HelpText = "Overrides --baseuri setting to https://acme-staging.api.letsencrypt.org/ and enables other testing behaviours in the program which may help with troubleshooting.")]
        public bool Test { get; set; }

        [Option(HelpText = "Print additional log messages to console for troubleshooting.")]
        public bool Verbose { get; set; }

        #region Renew 

        [Option(HelpText = "Check for scheduled renewals.")]
        public bool Renew { get; set; }

        [Option(HelpText = "Force renewal on all scheduled certificates  when used together with --renew. Otherwise just bypasses the certificate cache on new certificate requests.")]
        public bool ForceRenewal { get; set; }

        #endregion

        #endregion

        #region Unattended

        #region Target

        [Option(HelpText = "Specify which target plugin to run, bypassing the main menu and triggering unattended mode.")]
        public string Plugin { get; set; }

        [Option(HelpText = "[--plugin iissite|iissites|iisbinding] Specify identifier of the site that the plugin should create the target from. For the iissites plugin this may be a comma separated list.")]
        public string SiteId { get; set; }

        [Option(HelpText = "[--plugin iissite|iissites] Exclude bindings from being included in the certificate. This may be a comma separated list.")]
        public string ExcludeBindings { get; set; }

        [Option(HelpText = "Hide sites that have existing https bindings.")]
        public bool HideHttps { get; set; }

        [Option(HelpText = "The password to set for the generated PFX file.")]
        public string PfxPassword { get; set; }

        [Option(HelpText = "[--plugin manual|iisbinding] A host name to manually get a certificate for. For the manual plugin this may be a comma separated list.")]
        public string ManualHost { get; set; }

        [Option(HelpText = "[--plugin manual] Is the target of the manual host an IIS website?")]
        public bool ManualTargetIsIIS { get; set; }

        #endregion

        #region Validation

        [Option(HelpText = "Specify which validation plugin to run. If none is specified, FileSystem validation will be chosen as the default.")]
        public string Validation { get; set; }

        [Option(Default = "http-01", HelpText = "Specify which validation mode to use.")]
        public string ValidationMode { get; set; }

        [Option(HelpText = "[--validationmode http-01 --validation filesystem] A web root for the manual host name for validation.")]
        public string WebRoot { get; set; }

        [Option(HelpText = "[--validationmode http-01 --validation filesystem|iis] Specify site to use for handling validation requests. Defaults to --siteid.")]
        public string ValidationSiteId { get; set; }

        [Option(HelpText = "[--validationmode http-01] Warm up websites before attempting HTTP authorization.")]
        public bool Warmup { get; set; }

        [Option(HelpText = "[--validationmode http-01 --validation ftp|webdav] Username for ftp(s)/WebDav server.")]
        public string UserName { get; set; }

        [Option(HelpText = "[--validationmode http-01 --validation ftp|webdav] Password for ftp(s)/WebDav server.")]
        public string Password { get; set; }

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

        [Option(HelpText = "[--validationmode dns-01 --validation dnsscript] Path to script to create TXT record. Parameters passed are the host name, record name and desired content.")]
        public string DnsCreateScript { get; set; }

        [Option(HelpText = "[--validationmode dns-01 --validation dnsscript] Path to script to remove TXT record. Parameters passed are the host name and record name.")]
        public string DnsDeleteScript { get; set; }

        #endregion

        #region Store

        [Option(HelpText = "While renewing, do not remove the previous certificate.")]
        public bool KeepExisting { get; set; }

        [Option(HelpText = "When using this setting, certificate files are stored to the CCS and IIS bindings are configured to reflect that.", SetName = "store")]
        public string CentralSslStore { get; set; }

        [Option(HelpText = "This setting can be used to target a specific Certificate Store for a renewal.", SetName = "store")]
        public string CertificateStore { get; set; }

        #endregion

        #region Installation

        [Option(HelpText = "Specify which installation plugins to use. This may be a comma separated list.", Separator = ',')]
        public IEnumerable<string> Installation { get; set; }

        [Option(HelpText = "[--installation iis] Specify site to install new bindings to. Defaults to --siteid.")]
        public string InstallationSiteId { get; set; }

        [Option(Default = 443, HelpText = "[--installation iis] Port to use for creating new HTTPS bindings.")]
        public int SSLPort { get; set; }

        [Option(HelpText = "[--installation manual] Path to script to run after retrieving the certificate.")]
        public string Script { get; set; }

        [Option(HelpText = "[--installation manual] Parameters for the script to run after retrieving the certificate.")]
        public string ScriptParameters { get; set; }

        #endregion

        #region Other 

        [Option(HelpText = "Close the application when complete. Overrules behaviour of --test, otherwise not needed.")]
        public bool CloseOnFinish { get; set; }
     
        [Option(HelpText = "Do not create (or offer to update) the scheduled task.")]
        public bool NoTaskScheduler { get; set; }

        [Option(HelpText = "Avoid the question about specifying the task scheduler user, as such defaulting to the SYSTEM account.")]
        public bool UseDefaultTaskUser { get; set; }

        [Option(HelpText = "Cancels existing scheduled renewal as specified by the target parameters.")]
        public bool Cancel { get; set; }

        #region AcmeRegistration 

        [Option(HelpText = "Accept the ACME terms of service.")]
        public bool AcceptTos { get; set; }

        [Option(HelpText = "Email address to use by ACME for renewal fail notices.")]
        public string EmailAddress { get; set; }

        #endregion

        #endregion

        #endregion
    }
}
