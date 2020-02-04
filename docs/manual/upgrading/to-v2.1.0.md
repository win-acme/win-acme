---
sidebar: manual
---

# Migration from v1.9.x to v2.1.x
You can follow the same instructions as listed for for 
[v1.9.x to v2.0.x](/win-acme/manual/upgrading/to-v2.0.0) 
with some notable exceptions. 

Releases `2.1.4` and above will also ensure that there is an account for the 
ACMEv2 server, so that an initial manual renewal is no longer required. 
For fully unattended upgrades, you will therefor have to specify 
`--import --emailaddress you@example.com --accepttos` on the command line so 
that the account can be created without additional user input.

# Migration from v2.0.x to v2.1.0
Version 2.1.0 is an xcopy update for "standard" users, but those who customized the program to fit their
needs with `settings.config`, custom plugins or custom Serilog configuration will have to re-apply some of 
these modifications. 

- Custom plugins will have to be modified to conform to the new async interfaces of this version of win-acme. 
Also they will have to be targeted to build for .NET Core 3.1. Note that this does not affect installation or
DNS scripts, only additional `.dll`s.
- settings.config has been replaced with [settings.json](/win-acme/reference/settings). The format is more
readable and nicely structured, but if you have some custom settings, you will have to re-apply them.
- If you are using custom settings for Serilog, you will have to migrate them to a new file called 
`serilog.json`. More details are available [here](/win-acme/manual/advanced-use/custom-logging).