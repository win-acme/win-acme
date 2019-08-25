---
sidebar: reference
---

A validation plugin is responsible for providing the ACME server with proof that you own the identifiers 
(host names) that you want to create a certificate for. The 
[ACMEv2 protocol](https://tools.ietf.org/html/draft-ietf-acme-acme-18) defines different challenge types, 
two whom are supported by Let's Encrypt and win-acme, namely [HTTP-0](/win-acme/reference/plugins/validation/http/) and 
[DNS-01](/win-acme/reference/plugins/validation/dns/). 

For wildcard identifiers, only DNS validation is accepted by Let's Encrypt.

Other challenge types are not supported for various reasons:
- `TLS-ALPN-01` - under investigation (see [#990](https://github.com/PKISharp/win-acme/issues/990))
- `TLS-SNI-01/-02` - deprecated and all but removed
- `PROOFOFPOSSESSION-01` - unknown