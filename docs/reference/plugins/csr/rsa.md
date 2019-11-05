---
sidebar: reference
---

# RSA
Default plugin, generates 3072 bits RSA key pairs. The number of bits can be configured in 
[settings.json](/win-acme/reference/settings) but may not be less than 2048. For 
improved compatiblitity with Microsoft Exchange, RSA keys are automatically converted to the
`Microsoft RSA SChannel Cryptographic Provider`.

{% include csr-common.md %}

## Unattended
`[--csr rsa]`