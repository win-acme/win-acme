---
sidebar: manual
---

# Getting started

## Installation
- Download the latest version of the program from this website. For most users the file 
called `win-acme.v2.x.x.xx.x64.trimmed.zip` is recommended, but if you want to run on a 
32 bit system you should get the `x86` version instead of the `x64` one, or if you want to 
download or develop extra plugins, you should get the `pluggable` version instead of the 
`trimmed` one.
- Unzip files to a non-temporary folder, so that the scheduled task will be able to run. 
We recommend using `%programfiles%\win-acme`.
- Run `wacs.exe` (this requires administrator privileges).
- Follow the instructions on the screen to configure your first renewal.

## Creating your first certificate
**Note:** simple mode is for users looking to install a non-wildcard certificate on their local IIS instance. 
For any other scenario you should skip straight to the section on [advanced use](/win-acme/manual/advanced-use/).

- Choose `N` in the main menu to create a new certificate in simple mode.
- Choose how you want to determine the domain name(s) that you want to include in the certificate. 
This can be derived from the bindings of an IIS site, or you can input them manually.
- A registration is created with the ACME server, if no existing one can be found. You will be asked 
to agree to its terms of service and to provide an email address that the administrators can use to contact you.
- The program negotiates with ACME server to try and prove your ownership of the domain(s) that you want to 
create the certificate for. By default the [http validation](/win-acme/reference/plugins/validation/http/) 
mode is picked, handled by our [self-hosting](/win-acme/reference/plugins/validation/http/selfhosting) plugin. 
Getting validation right is often the most tricky part of getting an ACME certificate. If there are 
problems please check out some [common issues](/win-acme/manual/validation-problems).
- After the proof has been provided, the program gets the new certificate from the ACME server and updates 
or creates IIS bindings as required, according to the logic documented [here](/win-acme/reference/plugins/installation/iisweb).
- The program remembers all choices that you made while creating the certificate and applies them 
for each subsequent [renewal](/win-acme/manual/automatic-renewal).
