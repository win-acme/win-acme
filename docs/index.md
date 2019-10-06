# win-acme
This is a ACMEv2 client for Windows that aims to be very simple to start with, 
but powerful enough to grow into almost every scenario.

- A very simple text driven interface to create and install certificates for a local IIS server
- A more advanced text driven interface for many other use cases
- Automatically creates a scheduled task renew certificates when needed
- Supports wildcards, OCSP Must Staple and ECDSA keys
- Advanced validation via SFTP/FTPS, TLS-ALPN, WebDav, [acme-dns](https://github.com/joohoi/acme-dns), Azure, Route53 and more
- Supports completely unattended operation from the command line
- Supports other forms of automation through manipulation of `.json` files
- Write your own Powershell `.ps1` scripts to handle custom installation and validation
- Build your own plugins with C# and make the program do exactly what you want

![screenshot](/win-acme/assets/screenshot.png)

# Sponsors
- [e-shop LTD](https://www.e-shop.co.il/)
- The Proof Group @proofgroup

# Getting started
Download the `.zip` file from the download menu, unpack it to a location on your hard disk
and run `wacs.exe`. If you require assistance please check the [manual](/win-acme/manual/getting-started)
first before looking for [support](/win-acme/support/).
