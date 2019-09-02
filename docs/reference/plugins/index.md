---
sidebar: reference
---

# Plugins

Conceptually win-acme works by chaining together five components also known as plugins, which can be mixed and matched to support many use cases.

- A [target plugin](/win-acme/reference/plugins/target/) provides information about (potential) certificates to create.
- A [validation plugin](/win-acme/reference/plugins/validation/) provides the ACME server with proof that you own the domain(s).
- A [CSR plugin](/win-acme/reference/plugins/csr/) determines the (type of) private key and extensions to use for the certificate.
- One or more [store plugins](/win-acme/reference/plugins/store/) place the certificate in a specific location and format.
- One or more [installation plugins](/win-acme/reference/plugins/installation/) make changes to your application(s) configuration.