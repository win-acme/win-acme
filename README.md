# Windows ACME Simple (WACS)
A simple ACME client for Windows - for use with Let's Encrypt. (Formerly known as letsencrypt-win-simple (LEWS))

[![Build status](https://ci.appveyor.com/api/projects/status/c4b3t6g82yyjl4v1?svg=true)](https://ci.appveyor.com/project/WouterTinus/win-acme-s8t9q)

# Overview
This is a ACMEv2 client for Windows that aims to be very simple to start with, but powerful enough to grow into almost every scenario.

- A simple CLI interface to request, install and update certificates for IIS
- Advanced CLI options for other applications
- Runs as scheduled task to automatically renew certificates and update applications
- Supports wildcard certificates, OCSP Must Staple and ECDSA keys
- Advanced validation via SFTP/FTPS, WebDav, [acme-dns](https://github.com/joohoi/acme-dns), Azure, Route53 and more
- Supports completely unattended operation from the command line
- Supports other forms of automation through manipulation of .json files
- Build your own plugins with .NET and make the program do exactly what you want

![screenshot](https://i.imgur.com/vRXYw9V.png)

# Running
Download the latest `win-acme.v2.x.x.xxx.zip` from the [releases](https://github.com/PKISharp/win-acme/releases) page (under "Assets"), unpack it to a location on your hard disk and run `wacs.exe`. Head to the [Wiki](https://github.com/PKISharp/win-acme/wiki) to learn more.

# Community support
If you run into trouble please open an issue [here](https://github.com/PKISharp/win-acme/issues). Please check to see if your issue is covered in the [Wiki](https://github.com/PKISharp/win-acme/wiki) before you create a new issue. Describe the exact steps you took and try to reproduce it while running with the `--verbose` command line option set. Post your command line and the console output to help us debug.

# Professional support / sponsorship
Is your business relying on this program to secure customer websites and perhaps even critical infrastructure? Then maybe it would be good for your peace of mind then to sponsor one of its core developers, to gain guaranteed future support and good karma at the same time. I offer my help quickly, discreetly and professionally via [Patreon](https://www.patreon.com/woutertinus).

# Donations
Do you like the program and want to buy me a beer and discuss the future of the program in private? My [Patreon](https://www.patreon.com/woutertinus) also has some simple "Thank you" tiers.
