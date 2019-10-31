---
sidebar: reference
---

# TLS-ALPN validation
TLS-ALPN validation works as follows:
- For each domain (e.g. `sub.example.com`), the ACME server sends a 
challenge consisting of an `x` and `y` value. The truth is actually a little 
more complicated than that, but for the sake of this explanation it will suffice.
- The client has to make sure that when the ACME server sets up a TLS connection 
to `sub.example.com`, a specifically crafted negotiation response with a 
self-signed certificate containing the `y` value as extension is presented.
- The validation request is *always* made to port 443, that cannot be changed. 
- There may be more than one validation connection for the same token, e.g. 
for different IP addresses (in case of multiple A/AAAA records).
- Let's Encrypt does **not** disclose the source locations of these requests, which 
effectively means that the domain has to be accessible for the public, 
at least for the duration of the validation.