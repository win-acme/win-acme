# letsencrypt-win-simple
A Simple ACME Client for the Windows Platform

# Overview

This is a ACME windows CLI client built in native .net and aims to be as simple as possible to use.

It's built on top of the [.net ACME protocol library](https://github.com/ebekker/letsencrypt-win/).

# Usage

1. Download latest build from https://github.com/Lone-Coder/letsencrypt-win-simple/releases
2. Unzip files to a permanent location (so that it can run for renewals)
3. Run letsencrypt.exe with administrator privileges.

Running the client will take you thru a menu system to get your certs and install them.

It will scan IIS for bindings with host names so you may need to add one for this client to work.

The client will write out an answer file to the web server directory that needs to be visible to the ACME server to verify domain ownership.

Certificate .pfx files are written to disk currently as well as imported into the windows certificate store.

The client can also create or update an https binding in IIS for you.

Automatic renewals should be fully working. It will create a task in Windows Task Schedule that will run each morning and update the certs automatically every 60 days. For renewals your web site must still be able to pass authorization via the answer file.

There's no support for AWS or Azure sites yet. Pull requests for them are welcome. For more complicated scenarios try the [powershell windows client](https://github.com/ebekker/letsencrypt-win/wiki/Example-Usage).

# Command Line Arguments

	LetsEncrypt.ACME 1.0.5795.26498
	 Let's Encrypt

	  --baseuri      (Default: https://acme-v01.api.letsencrypt.org/) The address
			 of the ACME server to use.

	  --accepttos    Accept the terms of service.

	  --renew        Check for renewals.

	  --test         Overrides BaseURI setting to
			 https://acme-staging.api.letsencrypt.org/

	  --help         Display this help screen.

	  --version      Display version information.

# Example Output

	Let's Encrypt (Simple Windows ACME Client)

	ACME Server: https://acme-staging.api.letsencrypt.org/
	Config Folder: C:\Users\Bryan\AppData\Roaming\letsencrypt-win-simple\httpsacme-s
	taging.api.letsencrypt.org
	Loading Signer from C:\Users\Bryan\AppData\Roaming\letsencrypt-win-simple\httpsa
	cme-staging.api.letsencrypt.org\Signer

	Getting AcmeServerDirectory
	Loading Registration from C:\Users\Bryan\AppData\Roaming\letsencrypt-win-simple\
	httpsacme-staging.api.letsencrypt.org\Registration

	Scanning IIS 7 Site Bindings for Hosts (Elevated Permissions Required)
	IIS Bindings
	 1: cooltext.com (%SystemDrive%\inetpub\wwwroot)
	 2: office.cooltext.com (%SystemDrive%\inetpub\wwwroot)

	 A: Get Certificates for All Bindings
	 Q: Quit
	Which binding do you want to get a cert for: 2

	Authorizing Identifier office.cooltext.com Using Challenge Type http-01
	 Writing challenge answer to C:\inetpub\wwwroot\.well-known/acme-challenge/ky_uL
	AH0x2O2452Vos5dMpQ1hiRj6cV7SJAnUoT8qHg
	 Writing web.config to add extensionless mime type to C:\inetpub\wwwroot\.well-k
	nown\acme-challenge\web.config
	 Answer should now be browsable at http://office.cooltext.com/.well-known/acme-c
	hallenge/ky_uLAH0x2O2452Vos5dMpQ1hiRj6cV7SJAnUoT8qHg
	 Submitting answer
	 Refreshing authorization
	 Authorization RESULT: valid
	 Deleting answer

	Requesting Certificate
	 Request Status: Created
	 Saving Certificate to C:\Users\Bryan\AppData\Roaming\letsencrypt-win-simple\htt
	psacme-staging.api.letsencrypt.org\office.cooltext.com-crt.der
	 Saving Issuer Certificate to C:\Users\Bryan\AppData\Roaming\letsencrypt-win-sim
	ple\httpsacme-staging.api.letsencrypt.org\ca-009CF1912EA8D50908-crt.pem
	 Saving Certificate to C:\Users\Bryan\AppData\Roaming\letsencrypt-win-simple\htt
	psacme-staging.api.letsencrypt.org\office.cooltext.com-all.pfx (with no password
	 set)

	Do you want to install the .pfx into the Certificate Store? (Y/N)
	 Opening Certificate Store
	 Loading .pfx
	 Adding Certificate to Store
	 Closing Certificate Store

	Do you want to add/update an https IIS binding? (Y/N)
	 Updating Existing https Binding
	 Commiting binding changes to IIS

	Do you want to automatically renew this certificate in 60 days? This will add a
	task scheduler task. (Y/N)
	 Deleting existing Task letsencrypt-win-simple httpsacme-staging.api.letsencrypt
	.org from Windows Task Scheduler.
	 Creating Task letsencrypt-win-simple httpsacme-staging.api.letsencrypt.org with
	 Windows Task Scheduler at 9am every day.
	 Removing existing scheduled renewal office.cooltext.com (%SystemDrive%\inetpub\
	wwwroot) Renew After 1/12/2016
	 Renewal Scheduled office.cooltext.com (%SystemDrive%\inetpub\wwwroot) Renew Aft
	er 1/12/2016

# Build Notes

To get the project to build correctly you may need to copy the "packages" folder that nuget creates into the letsencrypt-win folder.
