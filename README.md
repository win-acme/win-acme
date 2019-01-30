# Windows ACME Simple (WACS)
A simple ACME client for Windows - for use with Let's Encrypt. (Formerly known as letsencrypt-win-simple (LEWS))

# Overview
This is a ACMEv2 client for Windows that aims to be very simple to start with, but powerful enough to grow with you into every scenario. It's basic features are:

- A simple CLI interface to request, install and update certificates for IIS
- Advanced CLI options for other applications
- Runs as scheduled task to automatically renew certificates and update applications
- Supports wildcard certificates, OCSP Must Staple and ECDSHA keys
- Supports completely unattended operation from the command line
- Supports other forms of automation through manipulation of .json files
- Build your own plugins with .NET and make the program do exactly what you want

![screenshot](https://i.imgur.com/vRXYw9V.png)

# Running
Download the [latest release](https://github.com/PKISharp/win-acme/releases), unpack and run `wacs.exe`, and follow the messages in the input prompt. Head to the [Wiki](https://github.com/PKISharp/win-acme/wiki) to learn more about advanced scenarios.

# Support
If you run into trouble please open an issue [here](https://github.com/PKISharp/win-acme/issues). Please check to see if your issue is covered in the [Wiki](https://github.com/PKISharp/win-acme/wiki) before you create a new issue. Describe the exact steps you took and try to reproduce it while running with the `--verbose` command line option set. Post your command line and the console output to help us debug.

[![Build status](https://ci.appveyor.com/api/projects/status/c4b3t6g82yyjl4v1?svg=true)](https://ci.appveyor.com/project/WouterTinus/win-acme-s8t9q)
