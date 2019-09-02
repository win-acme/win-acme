---
sidebar: reference
---

# DNS validation
DNS validation works as follows:
- For each domain, e.g. `sub.example.com`, the ACME server provides a 
challenge consisting of an `x` and `y` value. The truth is actually a little 
more complicated than that, but for the sake of this explanation it will suffice.
- The client has to make sure that when the ACME server requests the TXT 
records for `_acme-challenge.sub.example.com`,
there should be at least one record called `x` with content `"y"`.
- There may be more than one validation lookup for the same token, e.g. from 
different locations or different protocols (IPv4/IPv6).
- Let's Encrypt validates the DNSSEC chain.
- Let's Encrypt follows CNAME records and respects delegated autority.
- Let's Encrypt does *not* disclose the source locations of these lookups, which 
effectively means that the DNS records have to be public, at least for the duration of 
the validation.