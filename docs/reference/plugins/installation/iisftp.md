---
sidebar: reference
---

# IIS FTP
Create or update FTP site bindings in IIS, according to the following logic:

- Any existing FTP sites linked to the previous certificate are updated to use the new certificate.
- The target FTP site will be updated to use the new certificate.

## Unattended 
`--installation iisftp [--ftpsiteid x]`