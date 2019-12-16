---
sidebar: manual
---

# Microsoft Exchange
Choose the domains that you want to generate the certificate for. Note that Let's Encrypt only 
issues certificates to public domains, that means no Active Directory server names or domain suffixes
that are only known inside of your intranet can be used. You can specify a maximum of 100 domains 
in a certificate.

Assumptions made in this example:

- We want to generate the certificate for three domains
   - mail.example.com
   - webmail.example.com
   - autodiscover.example.com
- mail.example.com will be the common name, hence we put it first
- OWA is running in the Default Web Site of IIS with Site Id `1`.
- We want to enable the certificate for SMTP and IMAP

## Interactive
- Create certificate with full options
- Manually input host names
- [http-01] Self-host verification files
- Create or update https bindings in IIS
- Would you like to add another installer step? (y/n): Y
- Run a custom script
- Would you like to add another installer step? (y/n): N
- Choose site to create new bindings: Default Web Site (or where ever OWA is at)
- Enter the path to the script that you want to run after renewal: `./Scripts/ImportExchange.ps1`
- Enter the parameter format string for the script: `'{CertThumbprint}' 'IIS,SMTP,IMAP' 1 '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'`

## Unattended
- Without Central Certificate Store 
  `wacs.exe --target manual --host mail.example.com,webmail.example.com,autodiscover.example.com --certificatestore My --acl-fullcontrol "network service,administrators" --installation iis,script --installationsiteid 1 --script "./Scripts/ImportExchange.ps1" --scriptparameters "'{CertThumbprint}' 'IIS,SMTP,IMAP' 1 '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'"`

- With Central Certificate Store
`wacs.exe --target manual --host mail.example.com,webmail.example.com,autodiscover.example.com --store centralssl --centralsslstore "C:\Central SSL" --installation iis,script --installationsiteid 1 --script "./Scripts/ImportExchange.ps1" --scriptparameters "'{CertThumbprint}' 'IIS,SMTP,IMAP' 1 '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'"`

## Verification
To make sure all is working properly, I'd encourage you to use the 
[Microsoft's Remote Connectivity Analyzer](https://testconnectivity.microsoft.com/). 
The Autodiscover and ActiveSync Autodiscover tests are really useful for testing this out. 
With Outlook 2016 requiring the use of [Autodiscover to connect to Exchange](http://blogs.technet.com/b/exchange/archive/2015/11/19/outlook-2016-what-exchange-admins-need-to-know.aspx), 
verifying that this works properly is an important step is making sure your environment is setup correctly.

## References
- [Assign certificates to Exchange services](https://technet.microsoft.com/en-us/library/dd351257%28v=exchg.160%29.aspx)
- [Import certificates into Exchange](https://technet.microsoft.com/en-us/library/bb124424(v=exchg.160).aspx)
- [Add MIME Type](https://support.microsoft.com/en-us/kb/326965)
- [Namespace planning in Exchange 2016](http://blogs.technet.com/b/exchange/archive/2015/10/06/namespace-planning-in-exchange-2016.aspx) 
- [Exchange Server 2016 Client Access Namespace configuration](http://exchangeserverpro.com/exchange-server-2016-client-access-namespace-configuration/)
- [Install Exchange 2016 in your lab](https://supertekboy.com/2015/09/22/install-exchange-2016-in-your-lab-part-5/)