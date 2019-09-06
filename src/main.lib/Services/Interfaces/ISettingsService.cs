using System;

namespace PKISharp.WACS.Services
{
    public interface ISettingsService
    {
        #region UI
        /// <summary>
        /// A string that is used to format the date of the 
        /// pfx file friendly name. Documentation for 
        /// possibilities is available from Microsoft.
        /// </summary>
        string FileDateFormat { get; }
        /// <summary>
        /// The number of hosts to display per page.
        /// </summary>
        int HostsPerPage { get; }
        #endregion

        #region ACME
        /// <summary>
        /// Default ACMEv2 endpoint to use when none 
        /// is specified with the command line.
        /// </summary>
        string DefaultBaseUri { get; }
        /// <summary>
        /// Default ACMEv2 endpoint to use when none is specified 
        /// with the command line and the --test switch is
        /// activated.
        /// </summary>
        string DefaultBaseUriTest { get; }
        /// <summary>
        /// Default ACMEv1 endpoint to import renewal settings from.
        /// </summary>
        string DefaultBaseUriImport { get; }
        /// <summary>
        /// When renewing or re-creating a previously
        /// requested certificate that has the exact 
        /// same set of domain names, the program will 
        /// used a cached version for this many days,
        /// to prevent users from running into rate 
        /// limits while experimenting. Set this to 
        /// a high value if you regularly re-request 
        /// the same certificates, e.g. for a Continuous 
        /// Deployment scenario.
        /// </summary>
        int CertificateCacheDays { get; }
        /// <summary>
        /// Automatically delete files older than 120 days 
        /// from the CertificatePath folder. Running with 
        /// default settings, these should only be long-
        /// expired certificates, generated for abandoned
        /// renewals. However we do advise caution.
        /// </summary>
        bool DeleteStaleCacheFiles { get; }
        /// <summary>
        /// Configures a proxy server to use for 
        /// communication with the ACME server. The 
        /// default setting uses the system proxy.
        /// Passing an empty string will bypass the 
        /// system proxy.
        /// </summary>
        string Proxy { get; }
        /// <summary>
        /// Username used to access the proxy server.
        /// </summary>
        string ProxyUsername { get; }
        /// <summary>
        /// Password used to access the proxy server.
        /// </summary>
        string ProxyPassword { get; }
        #endregion

        #region Scheduled task
        /// <summary>
        /// The number of days to renew a certificate 
        /// after. Let’s Encrypt certificates are 
        /// currently for a max of 90 days so it is 
        /// advised to not increase the days much.
        /// If you increase the days, please note
        /// that you will have less time to fix any
        /// issues if the certificate doesn’t renew 
        /// correctly.
        /// </summary>
        int RenewalDays { get; }
        /// <summary>
        /// Configures start time for the scheduled task.
        /// </summary>
        TimeSpan ScheduledTaskStartBoundary { get; }
        /// <summary>
        /// Configures time after which the scheduled 
        /// task will be terminated if it hangs for
        /// whatever reason.
        /// </summary>
        TimeSpan ScheduledTaskExecutionTimeLimit { get; }
        /// <summary>
        /// Configures random time to wait for starting 
        /// the scheduled task.
        /// </summary>
        TimeSpan ScheduledTaskRandomDelay { get; }

        #endregion

        #region Notifications
        /// <summary>
        /// SMTP server to use for sending email notifications. 
        /// Required to receive renewal failure notifications.
        /// </summary>
        string SmtpServer { get; }
        /// <summary>
        /// SMTP server port number.
        /// </summary>
        int SmtpPort { get; }
        /// <summary>
        /// User name for the SMTP server, in case 
        /// of authenticated SMTP.
        /// </summary>
        string SmtpUser { get; }
        /// <summary>
        /// Password for the SMTP server, in case 
        /// of authenticated SMTP.
        /// </summary>
        string SmtpPassword { get; }
        /// <summary>
        /// Change to True to enable SMTPS.
        /// </summary>
        bool SmtpSecure { get; }
        /// <summary>
        /// Display name to use as the sender of 
        /// notification emails. Defaults to the 
        /// ClientName setting when empty.
        /// </summary>
        string SmtpSenderName { get; }
        /// <summary>
        /// Email address to use as the sender 
        /// of notification emails. Required to 
        /// receive renewal failure notifications.
        /// </summary>
        string SmtpSenderAddress { get; }
        /// <summary>
        /// Email address to receive notification emails. 
        /// Required to receive renewal failure 
        /// notifications.
        /// </summary>
        string SmtpReceiverAddress { get; }
        /// <summary>
        /// Send an email notification when a certificate 
        /// has been successfully renewed, as opposed to 
        /// the default behavior that only send failure
        /// notifications. Only works if at least 
        /// SmtpServer, SmtpSenderAddressand 
        /// SmtpReceiverAddress have been configured.
        /// </summary>
        bool EmailOnSuccess { get; }
        #endregion

        #region Security 
        /// <summary>
        /// The key size to sign the certificate with. 
        /// Minimum is 2048.
        /// </summary>
        int RSAKeyBits { get; }
        /// <summary>
        /// The curve to use for EC certificates.
        /// </summary>
        string ECCurve { get; }
        /// <summary>
        /// If set to True, it will be possible to export 
        /// the generated certificates from the certificate 
        /// store, for example to move them to another 
        /// server.
        /// </summary>
        bool PrivateKeyExportable { get; }
        /// <summary>
        /// Uses Microsoft Data Protection API to encrypt 
        /// sensitive parts of the configuration, e.g. 
        /// passwords. This may be disabled to share 
        /// the configuration across a cluster of machines.
        /// </summary>
        bool EncryptConfig { get; }
        #endregion

        #region Disk paths
        /// <summary>
        /// The name of the client, which comes back in the 
        /// scheduled task and the ConfigurationPath.
        /// </summary>
        string[] ClientNames { get; }
        /// <summary>
        /// Change the location where the program stores 
        /// its (temporary) files. If not specified this
        /// resolves to %programdata%\[ClientName]\[BaseUri]
        /// </summary>
        string ConfigPath { get; }
        /// <summary>
        /// The path where certificates and request files 
        /// are stored. If not specified or invalid, this 
        /// defaults to (ConfigurationPath)\Certificates. 
        /// All directories and subdirectories in the 
        /// specified path are created unless they already 
        /// exist. If you are using a Central SSL Store, 
        /// this can not be set to the same path.
        /// </summary>
        string CertificatePath { get; }
        /// <summary>
        /// The path where log files for the past 31 days 
        /// are stored. If not specified or invalid, this 
        /// defaults to (ConfigurationPath)\Log.
        /// </summary>
        string LogPath { get; }
        #endregion

        #region Validation
        /// <summary>
        /// If set to True, it will cleanup the folder 
        /// structure and files it creates under the 
        /// site for authorization.
        /// </summary>
        bool CleanupFolders { get; }
        /// <summary>
        /// A comma seperated list of servers to query 
        /// during DNS prevalidation checks to verify 
        /// whether or not the validation record has 
        /// been properly created and is visible for 
        /// the world. These servers will be used to 
        /// located the actual authoritative name 
        /// servers for the domain. You can use the 
        /// string [System] to have the program query 
        /// your servers default, but note that this 
        /// can lead to prevalidation failures when 
        /// your Active Directory is hosting a private 
        /// version of the DNS zone for internal use.
        /// </summary>
        string DnsServer { get; }
        #endregion

        #region Store
        /// <summary>
        /// The certificate store to save the certificates 
        /// in. If left empty, certificates will be installed 
        /// either in the WebHosting store, or if that is 
        /// not available, the My store (better known as 
        /// Personal).
        /// </summary>
        string DefaultCertificateStore { get; }
        /// <summary>
        /// When using --store centralssl this path is used 
        /// by default, saving you the effort from providing 
        /// it manually. Filling this out makes the 
        /// --centralsslstore parameter unnecessary 
        /// in most cases. Renewals created with the 
        /// default path will automatically change to 
        /// any future default value, meaning this is
        /// also a good practice for maintainability.
        /// </summary>
        string DefaultCentralSslStore { get; }
        /// <summary>
        /// When using --store centralssl this password 
        /// is used by default for the pfx files, saving 
        /// you the effort from providing it manually.
        /// Filling this out makes the --pfxpassword 
        /// parameter unnecessary in most cases. 
        /// Renewals created with the default password
        /// will automatically change to any future 
        /// default value, meaning this is also a 
        /// good practice for maintainability.
        /// </summary>
        string DefaultCentralSslPfxPassword { get; }
        /// <summary>
        /// When using --store pemfiles this path is used
        /// by default, saving you the effort from 
        /// providing it manually. Filling this out 
        /// makes the --pemfilespath parameter 
        /// unnecessary in most cases. Renewals 
        /// created with the default path will automatically 
        /// change to any future default value, meaning 
        /// this is also a good practice for maintainability.
        /// </summary>
        string DefaultPemFilesPath { get; }
        #endregion
    }
}
