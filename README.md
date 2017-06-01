# letsencrypt-win-simple
A Simple ACME Client for Windows

[![Appveyor Build](https://ci.appveyor.com/api/projects/status/8eoftpjpyyja2j7p?svg=true)](https://ci.appveyor.com/project/brondavies/letsencrypt-win-simple/build/artifacts)
[![Code Climate](https://codeclimate.com/github/brondavies/letsencrypt-win-simple.png)](https://codeclimate.com/github/brondavies/letsencrypt-win-simple)

# Overview

This is a ACME windows CLI client built in native .net and aims to be as simple as possible to use.

The goal of this particular fork is focused on code quality improvement, Azure web app support, unit test coverage and the ability to fully run the tool without the need for interaction from the command line.

It's built on top of the [.net ACME protocol library](https://github.com/ebekker/ACMESharp).

# Running

Download the latest release from [https://ci.appveyor.com/project/brondavies/letsencrypt-win-simple/build/artifacts](https://ci.appveyor.com/project/brondavies/letsencrypt-win-simple/build/artifacts). Unzip and run `letsencryptcli.exe`, and follow the messages in the input prompt.  You can also modify the behavior and options using the command line or settings in `letsencryptcli.exe.config`

# Command-line Options

Option                 | Description
---------------------- | -----------
--accepttos            | Accept the terms of service.
--baseuri              | The address of the ACME server to use. (Default: https://acme-v01.api.letsencrypt.org/)
--centralssl           | Whether to use the Centralized Certificate Store
--centralsslstore      | Path for Centralized Certificate Store (This enables Centralized SSL). Ex. \\storage\central_ssl\
--certificatestore     | The name of the Windows certificate store where certificates will be installed (Default is WebHosting.
--certoutpath          | Path for certificate files to be output. Ex. C:\Sites\MyWeb.com\certs
--cleanupfolders       | Whether to delete empty folders created for /.well-known/acme-challenge
--configpath           | Path to a folder where configuration files will be saved.
--emailaddress         | Provide email contact address.
--filedateformat       | The date format used for the certificate friendly name (Default is `yyyy/M/d h:m:s tt`)
--help                 | Display the help screen.
--hidehttps            | Hide sites that have existing HTTPS bindings
--keepexisting         | Keep existing HTTPS bindings, and certificates
--manualhost           | A host name to manually get a certificate for. The webroot option must also be set.
--pfxpassword          | The password to use on the certificate PFX file
--plugin               | Which plugin to use
--pluginconfig         | Path to the plugin configuration file.
--privatekeyexportable | Whether the private key is exportable (Default is true)
--proxy                | A web proxy address to use.
--renew                | Check for renewals.
--renewalperiod        | The number of days after the certificate issuance to renew it. (Default is 60)
--rsakeybits           | Either 2048 (default) or 1024
--san                  | Certificates per site instead of per host
--script               | A script for installation of non IIS Plugin.
--scriptparameters     | Parameters for the script for installation of non IIS Plugin.
--signeremail          | Email address (not public) to use for renewal fail notices - only gets set on first run for each Let's Encrypt server
--silent               | Execute silently - no prompts.  If any information is needed, you must pass it in.
--test                 | Overrides BaseUri setting to https://acme-staging.api.letsencrypt.org/
--usedefaulttaskuser   | Use the default user for the renew task.
--version              | Display version information.
--warmup               | Warmup sites before authorization
--webroot              | (Default: `%SystemDrive%\inetpub\wwwroot`) The web root to use for manual host name authentication.

# Settings

Some of the applications' settings can be updated in the app's settings or configuration file. the file is in the application root and is called letsencryptcli.exe.config.

### FileDateFormat

The FileDateFormat is a string that is used to format the date of the pfx file's friendly name.
Default: ```yyyy/M/d/ h:m:s tt``` ex. 2016/1/21 2:58:12 PM
See https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx to create your own custom date format.

### PFXPassword

The password to sign all PFX files with. Default is empty.

### RSAKeyBits

The key size to sign the certificate with. Default is 2048, Minimum is 1024.

### HostsPerPage

The number of hosts to display per page. Default is 50.

### RenewalDays

The number of days to renew a certificate after.
The default is 60. Let's Encrypt certificates are currently valid for a max of 90 days so it is advised to not increase the days much.
If you increase the days, please note that you will have less than 30 days to fix any issues if the certificate doesn't renew correctly.

### CertificateStore

The certificate store to save the certificates in.

### CleanupFolders

If set to True, it will cleanup the folder structure and files it creates under the site for authorization.

# Support

If you run into trouble please open an issue at https://github.com/brondavies/letsencrypt-win-simple/issues

If you ran the app and you got an error when it tried to authorize your site take a look [here](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/web.config).

# Web.Config Pull Requests

If you submit a pull request that changes the included web_config.xml file and it does not work on stock IIS 7.5 +, it will not be merged in. Instead add a section to the [WIki page](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/web.config) with your changes.
