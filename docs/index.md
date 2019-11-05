# win-acme
This is a ACMEv2 client for Windows that aims to be very simple to start with, 
but powerful enough to grow into almost every scenario.

- A very simple text driven interface to create and install certificates on a local IIS server
- A more advanced text driven interface for many other use cases, including Apache, Exchange, etc.
- Automatically creates a scheduled task to renew certificates when needed
- Get advanced certificates with wildcards (`*.example.com`), 
	international domain names (`证书.example.com`), 
	[OCSP Must Staple](/win-acme/reference/plugins/csr/rsa) extension, optional
	[private key re-use](/win-acme/reference/plugins/csr/rsa),
	[Elliptic Curve](/win-acme/reference/plugins/csr/ec) crypto or 
	even full [custom CSR](/win-acme/reference/plugins/target/csr)
- Advanced toolkit for DNS, HTTP and TLS validation:
	[SFTP](/win-acme/reference/plugins/validation/http/sftp), 
	[FTPS](/win-acme/reference/plugins/validation/http/ftps),
	[WebDav](/win-acme/reference/plugins/validation/http/webdav),
	[acme-dns](/win-acme/reference/plugins/validation/dns/acme-dns),
	[Azure](/win-acme/reference/plugins/validation/dns/azure),
	[Route53](/win-acme/reference/plugins/validation/dns/route53) 
	and more...
- Completely unattended operation from the command line
- Other forms of automation through manipulation of `.json` files
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
