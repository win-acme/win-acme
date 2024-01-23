using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Configuration.Settings
{
    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsJson : JsonSerializerContext 
    {
        public static SettingsJson Insensitive => new(new JsonSerializerOptions() { 
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        });
    }

    /// <summary>
    /// All settings
    /// </summary>
    public class Settings
    {
        public ClientSettings Client { get; set; } = new ClientSettings();
        public UiSettings UI { get; set; } = new UiSettings();
        public AcmeSettings Acme { get; set; } = new AcmeSettings();
        public ExecutionSettings Execution { get; set; } = new ExecutionSettings();
        public ProxySettings Proxy { get; set; } = new ProxySettings();
        public CacheSettings Cache { get; set; } = new CacheSettings();
        public ScheduledTaskSettings ScheduledTask { get; set; } = new ScheduledTaskSettings();
        public NotificationSettings Notification { get; set; } = new NotificationSettings();
        public SecuritySettings Security { get; set; } = new SecuritySettings();
        public ScriptSettings Script { get; set; } = new ScriptSettings();
        [Obsolete("Use Source instead")]
        public SourceSettings Target { get; set; } = new SourceSettings();
        public SourceSettings Source { get; set; } = new SourceSettings();
        public ValidationSettings Validation { get; set; } = new ValidationSettings();
        public OrderSettings Order { get; set; } = new OrderSettings();
        public CsrSettings Csr { get; set; } = new CsrSettings();
        public StoreSettings Store { get; set; } = new StoreSettings();
        public InstallationSettings Installation { get; set; } = new InstallationSettings();
        public SecretsSettings Secrets { get; set; } = new SecretsSettings();
    }

    public class ClientSettings
    {
        public string ClientName { get; set; } = "win-acme";
        public string ConfigurationPath { get; set; } = "";
        public string? LogPath { get; set; }
        public bool VersionCheck { get; set; }
    }

    public class UiSettings
    {
        /// <summary>
        /// The number of hosts to display per page.
        /// </summary>
        public int PageSize { get; set; } = 50;
        /// <summary>
        /// A string that is used to format the date of the 
        /// pfx file friendly name. Documentation for 
        /// possibilities is available from Microsoft.
        /// </summary>
        public string? DateFormat { get; set; }
        /// <summary>
        /// How console tekst should be encoded
        /// </summary>
        public string? TextEncoding { get; set; }
    }

    public class AcmeSettings
    {
        /// <summary>
        /// Default ACMEv2 endpoint to use when none 
        /// is specified with the command line.
        /// </summary>
        public Uri? DefaultBaseUri { get; set; }
        /// <summary>
        /// Default ACMEv2 endpoint to use when none is specified 
        /// with the command line and the --test switch is
        /// activated.
        /// </summary>
        public Uri? DefaultBaseUriTest { get; set; }
        /// <summary>
        /// Default ACMEv1 endpoint to import renewal settings from.
        /// </summary>
        public Uri? DefaultBaseUriImport { get; set; }
        /// <summary>
        /// Use POST-as-GET request mode
        /// </summary>
        public bool PostAsGet { get; set; }
        /// <summary>
        /// Validate the server certificate
        /// </summary>
        public bool? ValidateServerCertificate { get; set; }
        /// <summary>
        /// Number of times wait for the ACME server to 
        /// handle validation and order processing
        /// </summary>
        public int RetryCount { get; set; } = 4;
        /// <summary>
        /// Amount of time (in seconds) to wait each 
        /// retry for the validation handling and order
        /// processing
        /// </summary>
        public int RetryInterval { get; set; } = 2;
        /// <summary>
        /// If there are alternate certificate, select 
        /// which issuer is preferred
        /// </summary>
        public string? PreferredIssuer { get; set; }        
    }

    /// <summary>
    /// Settings regarding the execution of the renewal
    /// </summary>
    public class ExecutionSettings
    {
        /// <summary>
        /// Default script to run before executing a renewal
        /// </summary>
        public string? DefaultPreExecutionScript { get; set; }
        /// <summary>
        /// Default script to run after execution a renewal
        /// </summary>
        public string? DefaultPostExecutionScript { get; set; }
    }

    public class ProxySettings
    {
        /// <summary>
        /// Configures a proxy server to use for 
        /// communication with the ACME server. The 
        /// default setting uses the system proxy.
        /// Passing an empty string will bypass the 
        /// system proxy.
        /// </summary>
        public string? Url { get; set; }
        /// <summary>
        /// Username used to access the proxy server.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Password used to access the proxy server.
        /// </summary>
        public string? Password { get; set; }
    }

    /// <summary>
    /// Settings for secret management
    /// </summary>
    public class SecretsSettings
    {
        public JsonSettings? Json { get; set; }
    }

    /// <summary>
    /// Settings for json secret store
    /// </summary>
    public class JsonSettings
    {
        public string? FilePath { get; set; }
    }

    public class CacheSettings
    {
        /// <summary>
        /// The path where certificates and request files are 
        /// stored. If not specified or invalid, this defaults 
        /// to (ConfigurationPath)\Certificates. All directories
        /// and subdirectories in the specified path are created 
        /// unless they already exist. If you are using a 
        /// [[Central SSL Store|Store-Plugins#centralssl]], this
        /// can not be set to the same path.
        /// </summary>
        public string? Path { get; set; }
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
        public int ReuseDays { get; set; }
        /// <summary>
        /// Automatically delete files older than
        /// (DeleteStaleFilesDays) days from the (Path). 
        /// Running with default settings, these should 
        /// only be long expired certificates, generated for 
        /// abandoned renewals. However we do advise caution.
        /// </summary>
        public bool DeleteStaleFiles { get; set; }        
        /// <summary>
        /// Automatically delete files older than 120 days 
        /// from the CertificatePath folder. Running with 
        /// default settings, these should only be long-
        /// expired certificates, generated for abandoned
        /// renewals. However we do advise caution.
        /// </summary>
        public int? DeleteStaleFilesDays { get; set; }

    }

    public class ScheduledTaskSettings
    {
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
        public int RenewalDays { get; set; }

        /// <summary>
        /// If a certificate is valid for less time than
        /// specified in RenewalDays it is at risk of expiring.
        /// E.g. a certificate valid for 30 days, would be invalid
        /// for 15 days already before it would be renewed at 
        /// 55 days. This is of course undesirable, so this setting
        /// defines the minimum number of valid days that the 
        /// certificate should have left. E.g. when the setting is 7,
        /// any certificate due to expire in less than 7 days will be
        /// renewed, regardless of when they were created.
        /// </summary>
        public int? RenewalMinimumValidDays { get; set; }

        /// <summary>
        /// To spread service load, program run time and/or to minimize 
        /// downtime, those managing a large amount of renewals may want
        /// to spread them out of the course of multiple days/weeks. 
        /// The number of days specified here will be substracted from
        /// RenewalDays to create a range in which the renewal will
        /// be processed. E.g. if RenewalDays is 66 and RenewalDaysRange
        /// is 10, the renewal will be processed between 45 and 55 days
        /// after issuing. 
        /// 
        /// If you use an order plugin to split your renewal into 
        /// multiple orders, orders may run on different days.
        /// </summary>
        public int? RenewalDaysRange { get; set; }

        /// <summary>
        /// By default we use ARI to manage renewal period (if available
        /// on the endpoint). This switch allows users to disable it.
        /// https://datatracker.ietf.org/doc/draft-ietf-acme-ari/
        /// </summary>
        public bool? RenewalDisableServerSchedule { get; set; }

        /// <summary>
        /// Configures random time to wait for starting 
        /// the scheduled task.
        /// </summary>
        public TimeSpan RandomDelay { get; set; }

        /// <summary>
        /// Configures start time for the scheduled task.
        /// </summary>
        public TimeSpan StartBoundary { get; set; }

        /// <summary>
        /// Configures time after which the scheduled 
        /// task will be terminated if it hangs for
        /// whatever reason.
        /// </summary>
        public TimeSpan ExecutionTimeLimit { get; set; }
    }

    public class NotificationSettings
    {
        /// <summary>
        /// SMTP server to use for sending email notifications. 
        /// Required to receive renewal failure notifications.
        /// </summary>
        public string? SmtpServer { get; set; }
        /// <summary>
        /// SMTP server port number.
        /// </summary>
        public int SmtpPort { get; set; }
        /// <summary>
        /// User name for the SMTP server, in case 
        /// of authenticated SMTP.
        /// </summary>
        public string? SmtpUser { get; set; }
        /// <summary>
        /// Password for the SMTP server, in case 
        /// of authenticated SMTP.
        /// </summary>
        public string? SmtpPassword { get; set; }
        /// <summary>
        /// Change to True to enable SMTPS.
        /// </summary>
        public bool SmtpSecure { get; set; }
        /// <summary>
        /// 1: Auto (default)
        /// 2: SslOnConnect
        /// 3: StartTls
        /// 4: StartTlsWhenAvailable
        /// </summary>
        public int? SmtpSecureMode { get; set; }
        /// <summary>
        /// Display name to use as the sender of 
        /// notification emails. Defaults to the 
        /// ClientName setting when empty.
        /// </summary>
        public string? SenderName { get; set; }
        /// <summary>
        /// Email address to use as the sender 
        /// of notification emails. Required to 
        /// receive renewal failure notifications.
        /// </summary>
        public string? SenderAddress { get; set; }
        /// <summary>
        /// Email addresses to receive notification emails. 
        /// Required to receive renewal failure 
        /// notifications.
        /// </summary>
        public List<string>? ReceiverAddresses { get; set; }
        /// <summary>
        /// Send an email notification when a certificate 
        /// has been successfully renewed, as opposed to 
        /// the default behavior that only send failure
        /// notifications. Only works if at least 
        /// SmtpServer, SmtpSenderAddressand 
        /// SmtpReceiverAddress have been configured.
        /// </summary>
        public bool EmailOnSuccess { get; set; }
        /// <summary>
        /// Override the computer name that 
        /// is included in the body of the email
        /// </summary>
        public string? ComputerName { get; set; }
    }

    public class SecuritySettings
    {
        [Obsolete("Use Csr.Rsa.KeySize")]
        public int? RSAKeyBits { get; set; }

        [Obsolete("Use Csr.Ec.CurveName")]
        public string? ECCurve { get; set; }

        [Obsolete("Use Store.CertificateStore.PrivateKeyExportable")]
        public bool? PrivateKeyExportable { get; set; }

        /// <summary>
        /// Uses Microsoft Data Protection API to encrypt 
        /// sensitive parts of the configuration, e.g. 
        /// passwords. This may be disabled to share 
        /// the configuration across a cluster of machines.
        /// </summary>
        public bool EncryptConfig { get; set; }
        /// <summary>
        /// Apply a datetimestamp to the friendly name 
        /// of the generated certificates
        /// </summary>
        public bool? FriendlyNameDateTimeStamp { get; set; }
    }

    /// <summary>
    /// Options for installation and DNS scripts
    /// </summary>
    public class ScriptSettings
    {
        public int Timeout { get; set; } = 600;
        public string? PowershellExecutablePath { get; set; }
    }

    public class SourceSettings
    {
        /// <summary>
        /// Default plugin to select in the Advanced menu
        /// in the menu.
        [Obsolete("Use DefaultSource instead")]
        public string? DefaultTarget { get; set; }
        /// <summary>
        /// Default plugin to select in the Advanced menu
        /// in the menu.
        public string? DefaultSource { get; set; }
    }

    public class ValidationSettings
    {
        /// <summary>
        /// Default plugin to select in the Advanced menu (if
        /// supported for the target), or when nothing is 
        /// specified on the command line.
        /// </summary>
        public string? DefaultValidation { get; set; }

        /// <summary>
        /// Default plugin type, e.g. HTTP-01 (default), DNS-01, etc.
        /// </summary>
        public string? DefaultValidationMode { get; set; }

        /// <summary>
        /// Disable multithreading for validation
        /// </summary>
        public bool? DisableMultiThreading { get; set; }

        /// <summary>
        /// Max number of validations to run in parallel
        /// </summary>
        public int? ParallelBatchSize { get; set; }

        /// <summary>
        /// If set to True, it will cleanup the folder structure
        /// and files it creates under the site for authorization.
        /// </summary>
        public bool CleanupFolders { get; set; }
        /// <summary>
        /// If set to `true`, it will wait until it can verify that the 
        /// validation record has been created and is available before 
        /// beginning DNS validation.
        /// </summary>
        public bool PreValidateDns { get; set; } = true;
        /// <summary>
        /// Maximum numbers of times to retry DNS pre-validation, while
        /// waiting for the name servers to start providing the expected
        /// answer.
        /// </summary>
        public int PreValidateDnsRetryCount { get; set; } = 5;
        /// <summary>
        /// Amount of time in seconds to wait between each retry.
        /// </summary>
        public int PreValidateDnsRetryInterval { get; set; } = 30;
        /// <summary>
        /// If set to `true`, the program will attempt to recurively 
        /// follow CNAME records present on _acme-challenge subdomains to 
        /// find the final domain the DNS-01 challenge should be handled by.
        /// This allows you to delegate validation of your certificates
        /// to another domain or provider, which can have benefits for 
        /// security or save you the effort of having to move everything 
        /// to a party that supports automation.
        /// </summary>
        public bool AllowDnsSubstitution { get; set; } = true;
        /// <summary>
        /// A comma-separated list of servers to query during DNS 
        /// prevalidation checks to verify whether or not the validation 
        /// record has been properly created and is visible for the world.
        /// These servers will be used to located the actual authoritative 
        /// name servers for the domain. You can use the string [System] to
        /// have the program query your servers default, but note that this 
        /// can lead to prevalidation failures when your Active Directory is 
        /// hosting a private version of the DNS zone for internal use.
        /// </summary>
        public List<string>? DnsServers { get; set; }
        /// <summary>
        /// Settings for FTP validation
        /// </summary>
        public FtpSettings? Ftp { get; set; }
    }

    /// <summary>
    /// Settings for FTP validation
    /// </summary>
    public class FtpSettings
    {
        // Use GnuTls library for SSL, tradeoff: https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection-using-GnuTLS
        public bool? UseGnuTls { get; set; }
    }

    public class OrderSettings
    {
        /// <summary>
        /// Default plugin to select when none is provided through the 
        /// command line
        /// </summary>
        public string? DefaultPlugin { get; set; }
        /// <summary>
        /// Amount of time (in days) that ordered 
        /// certificates should remain valid
        /// </summary>
        public int? DefaultValidDays { get; set; } = null;
    }

    public class CsrSettings
    {
        /// <summary>
        /// Default plugin to select 
        /// </summary>
        public string? DefaultCsr { get; set; }
        /// <summary>
        /// RSA key settings
        /// </summary>
        public RsaSettings? Rsa { get; set; }
        /// <summary>
        /// EC key settings
        /// </summary>
        public EcSettings? Ec { get; set; }
    }

    public class RsaSettings
    {
        /// <summary>
        /// The key size to sign the certificate with. 
        /// Minimum is 2048.
        /// </summary>
        public int? KeyBits { get; set; }
        /// <summary>
        /// CSR signature algorithm, to be picked from 
        /// https://github.com/bcgit/bc-csharp/blob/master/crypto/src/cms/CMSSignedGenerator.cs
        /// </summary>
        public string? SignatureAlgorithm { get; set; }
    }

    public class EcSettings
    {
        /// <summary>
        /// The curve to use for EC certificates.
        /// </summary>
        public string? CurveName { get; set; }
        /// <summary>
        /// CSR signature algorithm, to be picked from 
        /// https://github.com/bcgit/bc-csharp/blob/master/crypto/src/cms/CMSSignedGenerator.cs
        /// </summary>
        public string? SignatureAlgorithm { get; set; }
    }

    public class StoreSettings
    {
        /// <summary>
        /// Default plugin(s) to select 
        /// </summary>
        public string? DefaultStore { get; set; }

        [Obsolete("Use CertificateStore.DefaultStore instead")]
        public string? DefaultCertificateStore { get; set; }
        [Obsolete("Use CentralSsl.DefaultStore instead")]
        public string? DefaultCentralSslStore { get; set; }
        [Obsolete("Use CentralSsl.DefaultPassword instead")]
        public string? DefaultCentralSslPfxPassword { get; set; }
        [Obsolete("Use PemFiles.DefaultPath instead")]
        public string? DefaultPemFilesPath { get; set; }

        /// <summary>
        /// Settings for the CentralSsl plugin
        /// </summary>
        public CertificateStoreSettings CertificateStore { get; set; } = new CertificateStoreSettings();

        /// <summary>
        /// Settings for the CentralSsl plugin
        /// </summary>
        public CentralSslSettings CentralSsl { get; set; } = new CentralSslSettings();

        /// <summary>
        /// Settings for the PemFiles plugin
        /// </summary>
        public PemFilesSettings PemFiles { get; set; } = new PemFilesSettings();

        /// <summary>
        /// Settings for the PfxFile plugin
        /// </summary>
        public PfxFileSettings PfxFile { get; set; } = new PfxFileSettings();

    }

    public class CertificateStoreSettings
    {
        /// <summary>
        /// The certificate store to save the certificates in. If left empty, 
        /// certificates will be installed either in the WebHosting store, 
        /// or if that is not available, the My store (better known as Personal).
        /// </summary>
        public string? DefaultStore { get; set; }
        /// <summary>
        /// If set to True, it will be possible to export 
        /// the generated certificates from the certificate 
        /// store, for example to move them to another 
        /// server.
        /// </summary>
        public bool? PrivateKeyExportable { get; set; }
        /// <summary>
        /// If set to True, the program will use the "Next-Generation Crypto API" (CNG)
        /// to store private keys, instead of thhe legacy API. Note that this will
        /// make the certificates unusable or behave differently for software that 
        /// only supports the legacy API. For example it will not work in older
        /// versions of Microsoft Exchange and they won't be exportable from IIS,
        /// even if the PrivateKeyExportable setting is true.
        /// </summary>
        public bool? UseNextGenerationCryptoApi { get; set; }
    }

    public class PemFilesSettings
    {
        /// <summary>
        /// When using --store pemfiles this path is used by default, saving 
        /// you the effort from providing it manually. Filling this out makes 
        /// the --pemfilespath parameter unnecessary in most cases. Renewals 
        /// created with the default path will automatically change to any 
        /// future default value, meaning this is also a good practice for 
        /// maintainability.
        /// </summary>
        public string? DefaultPath { get; set; }
        /// <summary>
        /// When using --store pemfiles this password is used by default for 
        /// the private key file, saving you the effort from providing it manually. 
        /// Filling this out makes the --pemfilespassword parameter unnecessary in 
        /// most cases. Renewals created with the default password will 
        /// automatically change to any future default value, meaning this
        /// is also a good practice for maintainability.
        /// </summary>
        public string? DefaultPassword { get; set; }
    }
    public class CentralSslSettings
    {
        /// <summary>
        /// When using --store centralssl this path is used by default, saving you
        /// the effort from providing it manually. Filling this out makes the 
        /// --centralsslstore parameter unnecessary in most cases. Renewals 
        /// created with the default path will automatically change to any 
        /// future default value, meaning this is also a good practice for 
        /// maintainability.
        /// </summary>
        public string? DefaultPath { get; set; }
        /// <summary>
        /// When using --store centralssl this password is used by default for 
        /// the pfx files, saving you the effort from providing it manually. 
        /// Filling this out makes the --pfxpassword parameter unnecessary in 
        /// most cases. Renewals created with the default password will 
        /// automatically change to any future default value, meaning this
        /// is also a good practice for maintainability.
        /// </summary>
        public string? DefaultPassword { get; set; }
    }

    public class PfxFileSettings
    {
        /// <summary>
        /// When using --store pfxfile this path is used by default, saving 
        /// you the effort from providing it manually. Filling this out makes 
        /// the --pfxfilepath parameter unnecessary in most cases. Renewals 
        /// created with the default path will automatically change to any 
        /// future default value, meaning this is also a good practice for 
        /// maintainability.
        /// </summary>
        public string? DefaultPath { get; set; }
        /// <summary>
        /// When using --store pfxfile this password is used by default for 
        /// the pfx files, saving you the effort from providing it manually. 
        /// Filling this out makes the --pfxpassword parameter unnecessary in 
        /// most cases. Renewals created with the default password will 
        /// automatically change to any future default value, meaning this
        /// is also a good practice for maintainability.
        /// </summary>
        public string? DefaultPassword { get; set; }
    }

    public class InstallationSettings
    {
        /// <summary>
        /// Default plugin(s) to select 
        /// </summary>
        public string? DefaultInstallation { get; set; }
    }
}