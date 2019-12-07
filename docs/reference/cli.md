---
sidebar: reference
---

# Command line arguments
Here are all the command line arguments the program accepts.

#### Notes
- Make sure that you are familiar with the basics of [renewal management](/win-acme/manual/renewal-management) 
  before proceeding with unattended use.
- Arguments documented as such: `--foo [--bar baz|qux]` mean that `--foo` is only 
applicable when `--bar` is set to `baz` or `qux`.

## Main
```
   --baseuri
     Address of the ACMEv2 server to use. The default endpoint
     can be modified in settings.json.

   --import
     Import scheduled renewals from version 1.9.x in unattended
     mode.

   --importbaseuri
     [--import] When importing scheduled renewals from version
     1.9.x, this argument can change the address of the ACMEv1
     server to import from. The default endpoint to import from
     can be modified in settings.json.

   --test
     Enables testing behaviours in the program which may help
     with troubleshooting. By default this also switches the
     --baseuri to the ACME test endpoint. The default endpoint
     for test mode can be modified in settings.json.

   --verbose
     Print additional log messages to console for
     troubleshooting and bug reports.

   --help
     Show information about all available command line options.

   --version
     Show version information.

   --renew
     Renew any certificates that are due. This argument is used
     by the scheduled task. Note that it's not possible to
     change certificate properties and renew at the same time.

   --force
     Force renewal on all scheduled certificates when used
     together with --renew. Otherwise just bypasses the
     certificate cache on new certificate requests.

   --cancel
     Cancel scheduled renewal specified by the friendlyname
     argument.

   --list
     List all created renewals in unattended mode.

   --id
     [--target|--cancel|--renew] Id of a new or existing 
	 renewal, can be used to override the default when 
	 creating a new renewal or to specify a specific 
	 renewal for other commands.

   --friendlyname
     [--target|--cancel|--renew] Friendly name of a new 
	 or existing renewal, can be used to override the 
	 default when creating a new renewal or to specify 
	 a specific renewal for other commands.

   --target
     Specify which target plugin to run, bypassing the main
     menu and triggering unattended mode.

   --validation
     Specify which validation plugin to run. If none is
     specified, SelfHosting validation will be chosen as the
     default.

   --validationmode
     Specify which validation mode to use. HTTP-01 is the
     default.

   --csr
     Specify which csr plugin to use. RSA is the default.

   --store
     Specify which store plugin to use. CertificateStore is the
     default. This may be a comma separated list.

   --installation
     Specify which installation plugins to use. IIS is the
     default. This may be a comma separated list.

   --closeonfinish
     [--test] Close the application when complete, which
     usually does not happen when test mode is active. Useful
     to test unattended operation.

   --hidehttps
     Hide sites that have existing https bindings from
     interactive mode.

   --notaskscheduler
     Do not create (or offer to update) the scheduled task.

   --usedefaulttaskuser
     Avoid the question about specifying the task scheduler
     user, as such defaulting to the SYSTEM account.

   --accepttos
     Accept the ACME terms of service.

   --emailaddress
     Email address to use by ACME for renewal fail notices.

   --encrypt
     Rewrites all renewal information using current
     EncryptConfig setting

```
# CSR

## Common
```
   --ocsp-must-staple
     Enable OCSP Must Staple extension on certificate.

   --reuse-privatekey
     Reuse the same private key for each renewal.

```
# Installation

## IIS FTP plugin
``` [--installation iisftp] ```
```
   --ftpsiteid
     Site id to install certificate to.

```
## IIS Web plugin
``` [--installation iis] ```
```
   --installationsiteid
     Specify site to install new bindings to. Defaults to the
     target if that is an IIS site.

   --sslport
     Port number to use for newly created HTTPS bindings.
     Defaults to 443.

   --sslipaddress
     IP address to use for newly created HTTPS bindings.
     Defaults to *.

```
## Script plugin
``` [--installation script] ```
```
   --script
     Path to script file to run after retrieving the
     certificate. This may be a .exe or .bat. Refer to the Wiki
     for instructions on how to run .ps1 files.

   --scriptparameters
     Parameters for the script to run after retrieving the
     certificate. Refer to the Wiki for further instructions.

```
# Store

## PEM files plugin
``` [--store pemfiles] ```
```
   --pemfilespath
     .pem files are exported to this folder

```
## Central Certificate Store plugin
``` [--store centralssl] ```
```
   --centralsslstore
     When using this setting, certificate files are stored to
     the CCS and IIS bindings are configured to reflect that.

   --pfxpassword
     Password to set for .pfx files exported to the IIS CSS.

```
## Certificate Store plugin
``` [--store certificatestore] ``` (default)
```
   --certificatestore
     This setting can be used to save the certificate in a
     specific store. By default it will go to 'WebHosting'
     store on modern versions of Windows.

   --keepexisting
     While renewing, do not remove the previous certificate.

   --acl-fullcontrol
     List of additional principals (besides the owners of the 
	 store) that should get full control permissions on the 
	 private key of the certificate.
```
# Target

## CSR plugin
``` [--target csr] ```
```
   --csrfile
     Specify the location of a CSR file to make a certificate
     for

   --pkfile
     Specify the location of the private key corresponding to
     the CSR

```
## IIS Binding plugin
``` [--target iisbinding] ```
```
   --siteid
     Id of the site where the binding should be found
     (optional).

   --host
     Host of the binding to get a certificate for.

```
## IIS Site(s) plugin
``` [--target iissite|iissites] ```
```
   --siteid
     Identifier of the site that the plugin should create the
     target from. For iissites this may be a comma separated
     list.

   --commonname
     Specify the common name of the certificate that should be
     requested for the target. By default this will be the
     first binding that is enumerated.

   --excludebindings
     Exclude host names from the certificate. This may be a
     comma separated list.

```
## Manual plugin
``` [--target manual] ```
```
   --commonname
     Specify the common name of the certificate. If not
     provided the first host name will be used.

   --host
     A host name to get a certificate for. This may be a comma
     separated list.

```
# Validation

## FileSystem plugin
``` [--validation filesystem] ```
```
   --validationsiteid
     Specify IIS site to use for handling validation requests.
     This will be used to choose the web root path.

```
## Common HTTP validation options
``` [--validation filesystem|ftp|sftp|webdav] ```
```
   --webroot
     Root path of the site that will serve the HTTP validation
     requests.

   --warmup
     Warm up website(s) before attempting HTTP validation.

   --manualtargetisiis
     Copy default web.config to the .well-known directory.

```
## SelfHosting plugin
``` [--validation selfhosting] ``` (default)
```
   --validationport
     Port to use for listening to validation requests. Note
     that the ACME server will always send requests to port 80.
     This option is only useful in combination with a port
     forwarding.

```
## AcmeDns
``` [--validationmode dns-01 --validation acme-dns] ```
```
   --acmednsserver
     Root URI of the acme-dns service

```
## Script
``` [--validationmode dns-01 --validation script] ```
```
   --dnsscript
     Path to script that creates and deletes validation
     records, depending on its parameters. If this parameter is
     provided then --dnscreatescript and --dnsdeletescript are
     ignored.

   --dnscreatescript
     Path to script that creates the validation TXT record.

   --dnscreatescriptarguments
     Default parameters passed to the script are create
     {Identifier} {RecordName} {Token}, but that can be
     customized using this argument.

   --dnsdeletescript
     Path to script to remove TXT record.

   --dnsdeletescriptarguments
     Default parameters passed to the script are delete
     {Identifier} {RecordName} {Token}, but that can be
     customized using this argument.

```
## Credentials
``` [--validation ftp|sftp|webdav] ```
```
   --username
     User name for WebDav/(s)ftp server

   --password
     Password for WebDav/(s)ftp server

```