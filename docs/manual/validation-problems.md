---
sidebar: manual
---

# Validation problems
Validation is an important aspect of the ACME and Let's Encrypt, but there are many subtle ways 
that it can fail. This page is meant for people who run into problems to help figure out what 
the issue might be.

## Testing 
Run `wacs.exe` with the `--test` and `--verbose` parameters to watch your validation unfold in 
'slow motion'. This will run against the Let's Encrypt staging server so you don't risk 
running into any rate limits. If you want to test against the production endpoint, include the
parameter `--baseuri https://acme-v02.api.letsencrypt.org/` as well.

## General validation issues

### DNSSEC
ACME providers will typically validate your DNSSEC configuration. If there is anything suspicious 
about it, your browser might not complain, but you will not be able to get a certificate. A useful 
tool to check your (provider's) DNSSEC configuration from the perspective of a strict external
observer is the [Unbound DNS checker](https://unboundtest.com/).

### CAA records
ACME providers will check for the existence and validity of a 
[CAA record](https://support.dnsimple.com/articles/caa-record/) for your domain. You may have to add
a record like `example.com. CAA 0 issue "letsencrypt.org" to your DNS server in order to allow the
provider to issue certificates for your domain.

### Protocols and cipher suites
Tools like [IISCrypto](https://www.nartac.com/Products/IISCrypto) are often used configure the 
[cipher suites](http://letsencrypt.readthedocs.io/en/latest/ciphers.html) of Windows systems 
according to the latest best practices. Changing these settings always brings some risk of 
breaking compatibility between two parties though. Too restrictive cipher suites have been known 
to hamper the ability to communicate with the ACME API endpoint and its validation servers. If 
that happens try more conservative settings. Test if the [API endpoint](https://acme-v02.api.letsencrypt.org)
is accessible from a web browser on your server.

## Let's Encrypt limitations
The following limitations apply to Let's Encrypt and may not be true for every ACME 
service provider.

### Domain count limit
Let's Encrypt does not support more than 100 domain names per certificate.

### Non-public domains
Let's Encrypt can only be used to issue certificates for domains living on the
public internet. Interal domains or Active Directory host names are therefor not
possible to use.

## HTTP validation issues

### Firewall
HTTP validation happens on port 80, so it will have to open on your firewall(s). Let's Encrypt 
doesn't disclose IP address range(s) for their validation servers, meaning port 80 will have 
to be accessible from *any* origin, at least for the duration of the validation.

### IPv6 configuration 
Let's Encrypt will check IPv6 access to your site if `AAAA` records are configured. Many browsers
and networks don't use IPv6 yet or automatically fallback to IPv4 when an error occurs, so 
it might not be immediately obvious that your site is unreachable on IPv6. You can test 
it [here](http://ipv6-test.com/validate.php).

### FileSystem plugin IIS issues
Note that it's recommended to use the default `SelfHosting` validation plugin in combination 
with IIS. The `FileSystem` validation is great of other web servers such as 
[Apache](/win-acme/manual/advanced-use/examples/apache), but using it in combination with IIS 
leads to many potentials issues, described in the following sections.

#### CMS modules
Your CMS might intercept the request and redirect the user to an (error) page. The solution 
is to configure your CMS to allow unlimited access to the `/.well-known/acme-challenge/` 
path.

#### Problems with httpHanders
IIS might not be configured to serve static extensionless files. 

1. In IIS manager go to the `/.well-known/acme-challenge/` folder of the site (you may have to 
create it). **Don't** do this at the root of the server or the website, because it might 
break your application(s).
2. Choose Handler Mappings -> View Ordered List.
3. Move the StaticFile mapping above the ExtensionlessUrlHandler mappings. 

![Move StaticFile mapping](http://i.stack.imgur.com/nkvrL.png)

#### Anonymous authentication
Your website might require Windows authentication, client certificates or other 
authentication methods. Enable anonymous authentication to the `/.well-known/acme-challenge/` 
path to allow access from the ACME server.

#### Require SSL
Your website might be configured to exclusively accept SSL traffic, while the validation 
request comes in on port 80. Disable the "Require SSL" setting for the 
`/.well-known/acme-challenge/` path to fix that.

#### IP Address and Domain Restrictions
Your website might use IP Address and Domain Restrictions to provide extra security. 
The ACME server will have to bypass though. Let's Encrypt does not publicize a list of 
IP addresses that they can use for validation, so this features needs to be disabled 
for the `/.well-known/acme-challenge/` path.

#### URL Rewrite 
If you are using [URL Rewrite](https://www.iis.net/downloads/microsoft/url-rewrite) the 
validation request might get caught up in that, so you have to make exceptions for 
the `/.well-known/acme-challenge/` path. For example like so:

```XML
<rule name="LetsEncrypt Rule" stopProcessing="true">
    <match url="^\.well-known\acme-challenge\.*$" />
    <action type="None" />
</rule>
```

## DNS validation issues

### Name server synchronisation
Let's Encrypt may query all of your name servers, so you they will have to be 
in sync before submitting the challenge. The program will perform a pre-validation
'dry run' for a maximum of 5 times with 30 second intervals to allow the DNS
changed to be processed.