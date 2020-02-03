---
sidebar: reference
---

# Settings.json
Some of the applications' settings can be modified in a file called `settings.json`. 
If this file is not present when the program starts it will be automatically 
created on first run, copied from `settings_default.json`. This allows you to
xcopy new releases without worrying about overwriting your previously customized 
settings.

## Client

### `ClientNames`
Default: `[ "win-acme" ]`

The name of the client, which comes back in the scheduled task and the 
`ConfigurationPath`. If more than one value is provided the first one will
be used.

### `ConfigurationPath`
Default: `null`

Change the location where the program stores its (temporary) files. If not specified 
this resolves to `%programdata%\{ClientName}\{BaseUri}`. Values should be JSON-encoded, 
e.g. `"C:\\"` (note the double backslash).

### `LogPath`
Default: `null`

The path where log files for the past 31 days are stored. If not 
specified or invalid, this defaults to `{ConfigurationPath}\Log`.

## UI

### `DateFormat` 
Default: `"yyyy/M/d H:mm:ss"`

A string that is used to format the date of the pfx file friendly 
name. [Documentation](https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx) 
for possibilities is available from Microsoft.

### `PageSize`
Default: `50`

The number of items to display per page in list views.

### `TextEncoding`
Default: `"utf8"`

Encoding to use for the console output. A list of possible values can be
found [here](https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding?view=netcore-3.0).
For certain languages `"unicode"` might give better results displaying the characters,
but note that this reduces compatibility with other programs processing the output.

## ACME

### `DefaultBaseUri`
Default: `"https://acme-v02.api.letsencrypt.org/"`

Default ACMEv2 endpoint to use when none is specified with 
the command line.

### `DefaultBaseUriTest`
Default: `"https://acme-staging-v02.api.letsencrypt.org/"`

Default ACMEv2 endpoint to use when none is specified with
the command line and the `--test` switch is activated.

### `DefaultBaseUriImport`
Default: `"https://acme-v01.api.letsencrypt.org/"`

Default ACMEv1 endpoint to import renewal settings from.

### `PostAsGet`
Default: `true`

Use [POST-as-GET] mode as defined in 
[RFC8555](https://tools.ietf.org/html/rfc8555#section-6.3), 
will be required by Let's Encrypt in production from November 2020, 
and in test from November 2019.

### `RetryCount`
Default: `5`

Maximum numbers of times to refresh validation and order status, while
waiting for the ACME server to complete its tasks.

### `RetryInterval`
Default: `5`

Amount of time in seconds to wait for each retry.

## Proxy

### `Url`
Default: `"[System]"`

Configures a proxy server to use for communication with the ACME server and
other HTTP requests done by the program. The default setting uses the 
system proxy. Passing an empty string will try to bypass the system proxy.

### `Username`
Default: `null`

Username used to access the proxy server.

### `Password`
Default: `null`

Password used to access the proxy server.

## Cache

### `Path`
Default: `null`

The path where certificates and request files are cached. If not specified or invalid,
this defaults to `{ConfigurationPath}\Certificates`. If you are using 
[Central SSL](//win-acme/reference/plugins/store/centralssl), this can **not** 
be set to the same path. Values should be JSON-encoded, e.g. `"C:\\"`
(note the double backslash).

### `ReuseDays`
Default: `1`

When renewing or re-creating a previously requested certificate that 
has the exact same set of domain names, the program will used a cached 
version for this many days, to prevent users from running into 
[rate limits](https://letsencrypt.org/docs/rate-limits/) while experimenting. 
Set this to a high value if you regularly re-request the same certificates, 
e.g. for a Continuous Deployment scenario.

### `DeleteStaleFiles`
Default: `false`

Automatically delete files older than 120 days from the `CertificatePath` 
folder. Running with default settings, these should only be long-expired 
certificates, generated for abandoned renewals. However we do advise caution.

## Scheduled task

### `RenewalDays`
Default: `55`

The number of days to renew a certificate after. Let's Encrypt certificates are 
currently for a max of 90 days so it is advised to not increase the days much. 
If you increase the days, please note that you will have less time to fix any 
issues if the certificate doesn't renew correctly.

### `StartBoundary`
Default: `"09:00:00"` (9:00 am)

Configures start time for the scheduled task.

### `ExecutionTimeLimit`
Default: `"02:00:00"` (2 hours)

Configures time after which the scheduled task will be 
terminated if it hangs for whatever reason.

### `RandomDelay`
Default: `"00:00:00"`

Configures random time to wait for starting the scheduled task.

## Notifications

### `SmtpServer`
Default: `null`

SMTP server to use for sending email notifications. 
Required to receive renewal failure notifications.

### `SmtpPort`
Default: `25`

SMTP server port number.

### `SmtpUser`
Default: `null`

User name for the SMTP server, in case of authenticated SMTP.

### `SmtpPassword`
Default: `null`

Password for the SMTP server, in case of authenticated SMTP.

### `SmtpSecure`
Default: `false`

Change to `true` to enable SMTPS.

### `SmtpSenderName`
Default: `null`

Display name to use as the sender of notification emails.
Defaults to the `ClientNames[0]` setting when empty.

### `SenderAddress`
Default: `null`

Email address to use as the sender of notification emails. 
Required to receive renewal failure notifications.

### `ReceiverAddresses`
Default: `[]`

Email address to receive notification emails. Required to 
receive renewal failure notifications. The correct format 
for the receiver is `["example@example.com"]` for a single 
address and `["example1@example.com", "example2@example.com"]` 
for multiple addresses.

### `EmailOnSuccess`
Default: `false`

Send an email notification when a certificate has been successfully renewed,
as opposed to the default behavior that only send failure notifications. 
Only works if at least `SmtpServer`, `SmtpSenderAddress`and `SmtpReceiverAddress` 
have been configured.

## Security

### `RSAKeyBits`
Default: `3072`

The key size to sign the certificate with. Minimum is 2048.

### `ECCurve`
Default: `"secp384r1"`

The curve to use for EC certificates.

### `PrivateKeyExportable`
Default: `false`

If set to `true`, it will be possible to export the generated certificates from
the certificate store, for example to move them to another server.

### `EncryptConfig`
Default: `true`

Uses Microsoft Data Protection API to encrypt sensitive parts of 
the configuration, e.g. passwords. This may be disabled to share 
the configuration across a cluster of machines.

## Script

### `Timeout`
Default: `600`

Time in seconds to allow installation and DNS scripts to run before
terminating them forcefully.

## Validation

### `CleanupFolders`
Default: `true`

If set to `true`, it will cleanup the folder structure and files it creates 
under the site for authorization.

### `PreValidateDns`
Default: `true`

If set to `true`, it will wait until it can verify that the validation record
has been created and is available before beginning DNS validation.

### `PreValidateDnsRetryCount`
Default: `5`

Maximum numbers of times to retry DNS pre-validation, while
waiting for the name servers to start providing the expected answer.

### `PreValidateDnsRetryInterval`
Default: `30`

Amount of time in seconds to wait between each retry.

### `DnsServers`
Default: `[ "8.8.8.8", "1.1.1.1", "8.8.4.4" ]`

A list of servers to query during DNS prevalidation checks to verify whether 
or not the validation record has been properly created and is visible for the 
world. These servers will be used to located the actual authoritative name
servers for the domain. You can use the string `[System]` to have the 
program query your servers default, but note that this can lead to 
prevalidation failures when your Active Directory is hosting a private 
version of the DNS zone for internal use. 

## Store

### `DefaultCertificateStore`
Default: `null`

The certificate store to save the certificates in. If left empty, certificates will
be installed either in the `WebHosting` store, or if that is not available, 
the `My` store (better known as `Personal`).

### `DefaultCentralSslStore`
Default: `null`

When using `--store centralssl` this path is used by default, saving you the 
effort from providing it manually. Filling this out makes the `--centralsslstore`
parameter unnecessary in most cases. Renewals created with the default path will 
automatically change to any future default value, meaning this is also a good 
practice for maintainability. Values should be JSON-encoded, e.g. `"C:\\"`
(note the double backslash).

### `DefaultCentralSslPfxPassword`
Default: `null`

When using `--store centralssl` this password is used by default for the pfx 
files, saving you the effort from providing it manually. Filling this out makes
the `--pfxpassword` parameter unnecessary in most cases. Renewals created with
the default password will automatically change to any future default value, 
meaning this is also a good practice for maintainability.

### `DefaultPemFilesPath`
Default: `null`

When using `--store pemfiles` this path is used by default, saving you the effort 
from providing it manually. Filling this out makes the `--pemfilespath` parameter
unnecessary in most cases. Renewals created with the default path will automatically
change to any future default value, meaning this is also a good practice for
maintainability. Values should be JSON-encoded, e.g. `"C:\\"`
(note the double backslash).