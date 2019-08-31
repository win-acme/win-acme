---
sidebar: reference
---

# Settings.config
Some of the applications' settings can be modified in a file called `settings.config`. 
If this file is not present when the program starts it will be automatically 
created on first run, copied from `settings_default.config`. This allows you to
xcopy new releases without worrying about overwriting your previously customized 
settings.

### `FileDateFormat` 
Default: `'yyyy/M/d H:mm:ss'`

A string that is used to format the date of the pfx file friendly 
name. [Documentation](https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx) 
for possibilities is available from Microsoft.

### `RSAKeyBits`
Default: `3072`

The key size to sign the certificate with. Minimum is 1024.

### `HostsPerPage`
Default: `50`

The number of hosts to display per page.

### `ClientName`
Default: `win-acme`

The name of the client, which comes back in the scheduled task and the `ConfigurationPath`.

### `ConfigurationPath`
Default: `''` (empty)

Change the location where the program stores its (temporary) files. If not specified 
this resolves to `%programdata%\[ClientName]\[BaseUri]`

### `CertificatePath`
Default: `''` (empty)

The path where certificates and request files are stored. If not specified or invalid,
this defaults to `(ConfigurationPath)\Certificates`. All directories and subdirectories 
in the specified path are created unless they already exist. If you are using a 
[[Central SSL Store|Store-Plugins#centralssl]], this can **not** be set to the same path.

### `RenewalDays`
Default: `55`

The number of days to renew a certificate after. Let's Encrypt certificates are 
currently for a max of 90 days so it is advised to not increase the days much. 
If you increase the days, please note that you will have less time to fix any 
issues if the certificate doesn't renew correctly.

### `DefaultCertificateStore`
Default: `''` (empty)

The certificate store to save the certificates in. If left empty, certificates will
be installed either in the `WebHosting` store, or if that is not available, 
the `My` store (better known as `Personal`).

### `DefaultCentralSslStore`
Default: `''` (empty)

When using `--store centralssl` this path is used by default, saving you the 
effort from providing it manually. Filling this out makes the `--centralsslstore`
parameter unnecessary in most cases. Renewals created with the default path will 
automatically change to any future default value, meaning this is also a good 
practice for maintainability.

### `DefaultCentralSslPfxPassword`
Default: `''` (empty)

When using `--store centralssl` this password is used by default for the pfx 
files, saving you the effort from providing it manually. Filling this out makes
the `--pfxpassword` parameter unnecessary in most cases. Renewals created with
the default password will automatically change to any future default value, 
meaning this is also a good practice for maintainability.

### `DefaultPemFilesPath`
Default: `''` (empty)

When using `--store pemfiles` this path is used by default, saving you the effort 
from providing it manually. Filling this out makes the `--pemfilespath` parameter
unnecessary in most cases. Renewals created with the default path will automatically
change to any future default value, meaning this is also a good practice for
maintainability.

### `CleanupFolders`
Default: `True`

If set to `True`, it will cleanup the folder structure and files it creates 
under the site for authorization.

### `PrivateKeyExportable`
Default: `False`

If set to `True`, it will be possible to export the generated certificates from
the certificate store, for example to move them to another server.

### `Proxy`
Default: `[System]`

Configures a proxy server to use for communication with the ACME server. The 
default setting uses the system proxy. Passing an empty string will bypass
the system proxy.

### `ProxyUsername`
Default: `''` (empty)

Username used to access the proxy server.

### `ProxyPassword`
Default: `''` (empty)

Password used to access the proxy server.

### `ScheduledTaskStartBoundary`
Default: `09:00:00` (9:00 am)

Configures start time for the scheduled task.

### `ScheduledTaskExecutionTimeLimit`
Default: `02:00:00` (2 hours)

Configures time after which the scheduled task will be 
terminated if it hangs for whatever reason.

### `ScheduledTaskRandomDelay`
Default: `00:00:00`

Configures random time to wait for starting the scheduled task.

### `EncryptConfig`
Default: `True`

Uses Microsoft Data Protection API to encrypt sensitive parts of 
the configuration, e.g. passwords. This may be disabled to share 
the configuration across a cluster of machines.

### `DefaultBaseUri`
Default: `https://acme-v02.api.letsencrypt.org/`

Default ACMEv2 endpoint to use when none is specified with 
the command line.

### `DefaultBaseUriTest`
Default: `https://acme-staging-v02.api.letsencrypt.org/`

Default ACMEv2 endpoint to use when none is specified with
the command line and the `--test` switch is activated.

### `DefaultBaseUriImport`
Default: `https://acme-v01.api.letsencrypt.org/`

Default ACMEv1 endpoint to import renewal settings from.

### `SmtpServer`
Default: `''` (empty)

SMTP server to use for sending email notifications. 
Required to receive renewal failure notifications.

### `SmtpPort`
Default: `25`

SMTP server port number.

### `SmtpUser`
Default: `''` (empty)

User name for the SMTP server, in case of authenticated SMTP.

### `SmtpPassword`
Default: `''` (empty)

Password for the SMTP server, in case of authenticated SMTP.

### `SmtpSecure`
Default: `False` (empty)

Change to `True` to enable SMTPS.

### `SmtpSenderName`
Default: ``

Display name to use as the sender of notification emails.
Defaults to the `ClientName` setting when empty.

### `SmtpSenderAddress`
Default: `''` (empty)

Email address to use as the sender of notification emails. 
Required to receive renewal failure notifications.

### `SmtpReceiverAddress`
Default: `''` (empty)

Email address to receive notification emails. 
Required to receive renewal failure notifications.

### `EmailOnSuccess`
Default: `False`

Send an email notification when a certificate has been successfully renewed,
as opposed to the default behavior that only send failure notifications. 
Only works if at least `SmtpServer`, `SmtpSenderAddress`and `SmtpReceiverAddress` 
have been configured.

### `DeleteStaleCacheFiles`
Default: `False`

Automatically delete files older than 120 days from the `CertificatePath` 
folder. Running with default settings, these should only be long-expired 
certificates, generated for abandoned renewals. However we do advise caution.

### `DnsServer`
Default: `'8.8.8.8,1.1.1.1,8.8.4.4'`

A comma seperated list of servers to query during DNS prevalidation
checks to verify whether or not the validation record has been properly 
created and is visible for the world. These servers will be used to located
the actual authoritative name servers for the domain. You can use the 
string `[System]` to have the program query your servers default, but note that
this can lead to prevalidation failures when your Active Directory is hosting 
a private version of the DNS zone for internal use. 

### `CertificateCacheDays`
Default: `1` (empty)

When renewing or re-creating a previously requested certificate that 
has the exact same set of domain names, the program will used a cached 
version for this many days, to prevent users from running into 
[rate limits](https://letsencrypt.org/docs/rate-limits/) while experimenting. 
Set this to a high value if you regularly re-request the same certificates, 
e.g. for a Continuous Deployment scenario.

### `LogPath`
Default: `` (empty)

The path where log files for the past 31 days are stored. If not 
specified or invalid, this defaults to `(ConfigurationPath)\Log`.