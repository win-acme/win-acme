---
sidebar: reference
---

# Validation plugins

A validation plugin is responsible for providing the ACME server with proof that you own the identifiers 
(host names) that you want to create a certificate for. The 
[ACMEv2 protocol](https://tools.ietf.org/html/draft-ietf-acme-acme-18) defines different 
challenge types, three of which are supported by win-acme, namely 
[HTTP-01](/win-acme/reference/plugins/validation/http/), 
[DNS-01](/win-acme/reference/plugins/validation/dns/) and 
[TLS-ALPN-01](/win-acme/reference/plugins/validation/tls-alpn/).

For wildcard identifiers, only DNS-01 validation is accepted by Let's Encrypt.

Several other challenge types are not supported for various reasons:
- `TLS-SNI-01/-02` - deprecated and removed
- `PROOFOFPOSSESSION-01` - unknown

## Default

By default, the [self-hosting plugin](/win-acme/reference/plugins/validation/http/selfhosting) is used.