---
sidebar: manual
---

# Advanced use
The simple mode works well for the most common use case, but there are many 
reasons to go for full options mode. For example:
- You don't have or use IIS
- You are requesting a wildcard certificate (and therefor need to use DNS validation)
- Port 80 is blocked on your network
- You are not running the program from your web server
- You are load balancing
- You need to run a script to install the certificate to your application, e.g. Exchange
- etc.

## Interactive
This describes the basic steps of an advanced mode request. It touches on concepts 
described [here](/win-acme/reference/plugins/), because it exposes more of the internal 
logic of the program to use to your advantage.

- Choose `M` in the main menu to create a new certificate in full options mode
- Choose a [target plugin](/win-acme/reference/plugins/target/) that will be used 
  to determine for which domain(s) the certificate should be issued.
- Choose a [validation plugin](/win-acme/reference/plugins/validation/) that will 
  be used to prove ownership of the domain(s) to the ACME server.
- Pick between RSA and EC private keys, which are both [plugins](/win-acme/reference/plugins/csr/) 
  used to generate a certificate signing request (CSR).
- One or more [store plugins](/win-acme/reference/plugins/store/) must be selected to save
  the certificate. For Apache, nginx and others web servers the `PemFiles` plugin is commonly chosen.
- One or more [installation plugins](/win-acme/reference/plugins/installation/) can be selected 
  to run after the certificate has been requested. The standard IIS option from simple mode 
  is of course available, but also the powerful [script installer](/win-acme/reference/plugins/installation/script) 
  installer.
- A registration with the ACME server is created, if it doesn't already exist. You will be 
  asked to agree to the terms of service and to provide an email address that the server 
  administrators can use to contact you.
- The program talks the ACME server to validate your ownership of the domain(s) that 
  you which to issue for.
- After validating the domains, a certificate signing request is prepared.
- The CSR is submitted to the ACME server and the signed response saved.
- The program runs the requested installation steps.

## Unattended
By providing the right [command line arguments](/win-acme/reference/cli) you can do 
everything that is possible in interactive mode, and more.

### Examples
The `--target` switch, used to select a [target plugin](/win-acme/reference/plugins/target/), 
triggers the unattended creation of new certificate.

- `--target manual` - selects the [manual plugin](/win-acme/reference/plugins/target/manual).
- `--target iissite` - selects the [iissite plugin](/win-acme/reference/plugins/target/iissite).

Each plugin has their own inputs which it needs to generate the certificate, for example:

```wacs.exe --target manual --host www.domain.com --webroot C:\sites\wwwroot```
```wacs.exe --target iissite --siteid 1 --excludebindings exclude.me```

There are some other parameters needed for first-time unattended use (e.g. on a clean server) 
to create the Let's Encrypt registration automatically (```--emailaddress myaddress@example.com --accepttos```).

One more parameters is needed for a first run to either prevent the creation of a scheduled 
task (`--notaskscheduler`), or to accept that it will be created under the default `SYSTEM` 
credential (`--usedefaulttaskuser`). So a full command line to create a certificate for IIS 
site 1 on a clean server (except for the 'exclude.me' binding) would look like this:

```wacs.exe --target iissite --siteid 1 --excludebindings exclude.me --emailaddress myaddress@example.com --accepttos --usedefaulttaskuser```

### More examples
Some application-specific examples are available [here](/win-acme/manual/advanced-use/examples).