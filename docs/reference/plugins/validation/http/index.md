---
sidebar: reference
---

# HTTP validation
HTTP validation works as follows:
- For each domain, e.g. `sub.example.com`, the ACME server provides a 
challenge consisting of an `x` and `y` value (it's a little more complicated than that, 
but for the sake of this explanation it will suffice).
- The client has to make sure that when the ACME server makes a request 
to `http://sub.example.com/.well-known/acme-challenge/x`, the answer will be exactly `y`.
- The validation request is *always* made to port 80, that cannot be changed. 
- There may be more than one validation request for the same token, e.g. from 
different locations or different protocols (IPv4/IPv6).
- Let's Encrypt does *not* disclose the source locations of these requests, which 
effectively means that the domain has to be accessible for the public, 
at least for the duration of the validation.