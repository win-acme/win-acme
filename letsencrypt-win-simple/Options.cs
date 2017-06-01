using CommandLine;

namespace LetsEncrypt.ACME.Simple
{
    class Options
    {
        [Option(Default = "https://acme-v01.api.letsencrypt.org/", HelpText = "The address of the ACME server to use.")]
        public string BaseUri { get; set; }

        [Option(HelpText = "Use the default user for the renew task.")]
        public bool UseDefaultTaskUser { get; set; }

        [Option(HelpText = "Provide email contact address.")]
        public string EmailAddress { get; set; }

        [Option(HelpText = "Accept the terms of service.")]
        public bool AcceptTos { get; set; }

        [Option(HelpText = "Email address (not public) to use for renewal fail notices - only gets set on first run for each Let's Encrypt server")]
        public string SignerEmail { get; set; }

        [Option(HelpText = "Check for renewals.")]
        public bool Renew { get; set; }

        [Option(HelpText = "Overrides BaseUri setting to https://acme-staging.api.letsencrypt.org/")]
        public bool Test { get; set; }

        [Option(HelpText = "A host name to manually get a certificate for. --webroot must also be set.")]
        public string ManualHost { get; set; }

        [Option(Default = "%SystemDrive%\\inetpub\\wwwroot",
            HelpText = "A web root for the manual host name for authentication.")]
        public string WebRoot { get; set; }

        [Option(HelpText = "A script for installation of non IIS Plugin.")]
        public string Script { get; set; }

        [Option(HelpText = "Parameters for the script for installation of non IIS Plugin.")]
        public string ScriptParameters { get; set; }

        [Option(HelpText = "Path for Centralized Certificate Store (This enables Centralized SSL). Ex. \\\\storage\\central_ssl\\")]
        public string CentralSslStore { get; set; }

        [Option(HelpText = "Path for certificate files to be output. Ex. C:\\Sites\\MyWeb.com\\certs")]
        public string CertOutPath { get; set; }

        [Option(HelpText = "Hide sites that have existing HTTPS bindings")]
        public bool HideHttps { get; set; }

        [Option(HelpText = "Certificates per site instead of per host")]
        public bool San { get; set; }

        [Option(HelpText = "Keep existing HTTPS bindings, and certificates")]
        public bool KeepExisting { get; set; }

        [Option(HelpText = "Warmup sites before authorization")]
        public bool Warmup { get; set; }

        [Option(HelpText = "Which plugin to use")]
        public string Plugin { get; set; }

        [Option(HelpText = "A web proxy address to use.")]
        public string Proxy { get; set; }

        [Option(HelpText = "Path for the config folder.")]
        public string ConfigPath { get; set; }        

        [Option(HelpText = "Tenant ID to login into Microsoft Azure.")]
        public string AzureTenantId { get; set; }

        [Option(HelpText = "Client ID to login into Microsoft Azure.")]
        public string AzureClientId { get; set; }

        [Option(HelpText = "Secret to login into Microsoft Azure.")]
        public string AzureSecret { get; set; }

        [Option(HelpText = "Subscription ID to login into Microsoft Azure DNS.")]
        public string AzureSubscriptionId { get; set; }

        [Option(HelpText = "The name of the resource group within Microsoft Azure DNS.")]
        public string AzureResourceGroupName { get; set; }
    }
}
