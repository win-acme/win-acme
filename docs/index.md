# win-acme
This is a ACMEv2 client for Windows that aims to be very simple to start with, 
but powerful enough to grow into almost every scenario.

- A very simple text driven interface to create and install certificates on a local IIS server
- A more advanced text driven interface for many other use cases, including Apache, Exchange, etc.
- Automatically creates a scheduled task to renew certificates when needed
- Supports creation of advanced certificates
  - Wildcards `*.example.com`
  - International domain names `证书.example.com`
  - [Custom CSR](/win-acme/reference/plugins/target/csr)
  - [Private key re-use](/win-acme/reference/plugins/csr/rsa)
  - [OCSP Must Staple](/win-acme/reference/plugins/csr/rsa)
  - [ECDSA keys](/win-acme/reference/plugins/csr/ec)
- Advanced tools for validation
  - [SFTP](/win-acme/reference/plugins/validation/http/sftp) / [FTPS](/win-acme/reference/plugins/validation/http/ftps)
  - [TLS-ALPN](/win-acme/reference/plugins/validation/tls-alpn/)
  - [WebDav](/win-acme/reference/plugins/validation/http/webdav)
  - [acme-dns](/win-acme/reference/plugins/validation/dns/acme-dns)
  - [Azure](/win-acme/reference/plugins/validation/dns/azure)
  - [Route53](/win-acme/reference/plugins/validation/dns/route53)
  - And more...
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
