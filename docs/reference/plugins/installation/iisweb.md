---
sidebar: reference
---

# IIS Web
Create or update website bindings in IIS, according to the following logic:

- Existing https bindings in *any* site linked to the previous certificate are updated to use the new certificate.
- Hosts names which are determined to not yet have been covered by any existing binding, will be processed further.
  - All existing https bindings in *target* site whose hostnames match with the new certificate are updated 
    to use the new certificate. This happens even if they are using certificates issued by other authorities. 
	(Note that if you want to prevent this from happening, you can use the `--excludebindings` switch).
  - If no existing https binding can be found, a new binding is created.
    - It will create bindings on the specified installation site and fall back to the target site if there is none.
	- It will use port `443` on IP `*` unless different values are specified with the `--sslport` and/or 
	  `--sslipaddress` switches.
  - New bindings will be created or updated for matching host headers with the most specific match. E.g. if you 
    generate a certificate for `a.b.c.com`, the order of preference for the binding creation/change will be:
      1. `a.b.c.com`
      2. `*.b.c.com`
      3. `*.c.com`
      4. `*.com`
      5. `*` (Default/empty binding)
  - If the certificate contains a wildcard domain, the order of preference will be:
      1. `*.a.b.c.com`
      2. `x.a.b.c.com`
  - In both cases, the first preferred option will be created from scratch if none of the later options 
    are available.
  - In some cases the plugin will not be able to (safely) add a new binding on older versions of IIS, e.g. due to
    lack of support for SNI and/or wildcard bindings. In that case the user will have to create them manually. 
	Renewals will still be automatic after this initial manual setup.

## Unattended 
`--installation iis [--installationsiteid x] [-sslport x] [--sslipaddress x]`