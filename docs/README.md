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
Download the `.zip` from the download menu, unpack it to a location on your hard disk and run `wacs.exe`.