namespace PKISharp.WACS.Configuration
{
    public class MainArguments
    {
        #region Basic 

        public string BaseUri { get; set; }
        public bool Test { get; set; }
        public bool Import { get; set; }
        public string ImportBaseUri { get; set; }
        public bool Verbose { get; set; }
        public bool Help { get; set; }
        public bool Version { get; set; }

        public bool Renew { get; set; }
        public bool Force { get; set; }
        public string FriendlyName { get; set; }
        public bool Cancel { get; set; }
        public bool List { get; set; }

        #endregion

        #region Target

        public string SiteId { get; set; }
        public string CommonName { get; set; }
        public string ExcludeBindings { get; set; }
        public bool HideHttps { get; set; }
        public string Host { get; set; }
        public bool ManualTargetIsIIS { get; set; }

        #endregion

        #region Validation

        public string WebRoot { get; set; }
        public int? ValidationPort { get; set; }
        public string ValidationSiteId { get; set; }
        public bool Warmup { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DnsCreateScript { get; set; }
        public string DnsDeleteScript { get; set; }
        
        #endregion

        #region Store

        public bool KeepExisting { get; set; }        
        public string CentralSslStore { get; set; }
        public string PfxPassword { get; set; }        
        public string CertificateStore { get; set; }

        #endregion

        public string Target { get; set; }
        public string Validation { get; set; }
        public string ValidationMode { get; set; }
        public string Store { get; set; }
        public string Installation { get; set; }
        public bool CloseOnFinish { get; set; }
        public bool NoTaskScheduler { get; set; }
        public bool UseDefaultTaskUser { get; set; }
        public bool AcceptTos { get; set; }
        public string EmailAddress { get; set; }
    }
}
