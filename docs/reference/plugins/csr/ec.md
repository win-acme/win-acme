---
sidebar: reference
---

# Elliptic Curve
Generates ECDSA keys based on the `secp384r1` curve. The curve to use can be 
configured in [settings.json](/win-acme/reference/settings) but currently only 
SEC named curves are supported by this program. The ACME server provider may 
also have limitations.

{% include csr-common.md %}

## Unattended
`--csr ec`