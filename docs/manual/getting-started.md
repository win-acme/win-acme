---
sidebar: manual
---

# Getting started

## Installation
- Download the latest version of `win-acme-v2.x.x.x.zip` from this website.
- Unzip files to a permanent location (so that it can run for renewals). We recommend `%programfiles%\win-acme`.
- Run `wacs.exe` (requires administrator privileges).
- Follow the instructions on the screen to configure your first renewal.

## Create your first certificate
Note: simple mode is for users looking to install a non-wildcard certificate on their local IIS instance. 
For any other scenario you should skip straight to [advanced use](/win-acme/advanced-use/).

- Choose `N` in the main menu to create a new certificate in simple mode.
- Choose how you want to determine the domain name(s) for which the certificate should be issued. 
This can for example be based on the bindings of an IIS site, or manual input.
- An account is created at the ACME server, if it doesn't already exist. You will be asked 
to agree to the terms of service and to provide an email address that the server administrators can 
use to contact you.
- The program talks the ACME server to validate your ownership of the domain(s) that you which to 
create a certificate for. By default that the ACME server does that by sending a couple of 
requests like `http://www.example.com/.well-known/acme-challenge/[random]` and we will be 
expected to respond with another random string. We run our own listener on port 80 - side by 
side with IIS - to answer those challenges. Getting validation right is often the most tricky 
part of getting an ACME certificate. If there are problems please check out some 
[common issues](/win-acme/manual/validation-problems).
- After the proof has been provided, the program gets the new certificate and updates or creates 
binding in IIS as required.
- The program will remember all the choices that you made while creating the certificate and apply them 
for each subsequent renewal.