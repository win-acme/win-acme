using CommandLine;

namespace LetsEncrypt.ACME.Simple
{
    class Options
    {
        [Option(Default = "https://acme-v01.api.letsencrypt.org/", HelpText = "The address of the ACME server to use.")]
        public string BaseUri { get; set; }

        [Option(HelpText = "Accept the terms of service.")]
        public bool AcceptTos { get; set; }

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

        [Option(
            HelpText =
                "Path for Centralized Certificate Store (This enables Centralized SSL). Ex. \\\\storage\\central_ssl\\")
        ]
        public string CentralSslStore { get; set; }

        [Option(HelpText = "Hide sites that have existing HTTPS bindings")]
        public bool HideHttps { get; set; }

        [Option(HelpText = "Certificates per site instead of per host")]
        public bool San { get; set; }

        [Option(HelpText = "Keep existing HTTPS bindings, and certificates")]
        public bool KeepExisting { get; set; }

        [Option(HelpText = "Warmup sites before authorization")]
        public bool Warmup { get; set; }

        [Option(HelpText = "Force Certificate Renewal")]
        public bool ForceRenewal { get; set; }

        [Option(HelpText = "No Task Scheduler")]
        public bool NoTaskScheduler { get; set; }

        [Option(HelpText = "Close the application when complete")]
        public bool CloseOnFinish { get; set; }

    }
}