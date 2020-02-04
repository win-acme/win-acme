---
sidebar: manual
---

# Migration from v1.9.x to v2.0.11
The following does not apply to you. Please just follow the steps for the 
[v1.9.x to v2.0.x](/win-acme/manual/upgrading/to-v2.0.0) migration.

# Migration from v2.0.x to v2.0.11
You will have to delete the files `Registration_v2` and `Signer_v2` from the ConfigurationPath,
which is `%programdata%\win-acme` by default. Then you will have to manually renew once to 
create a new account with the ACME server. 

The reason for this is that v2.0.11 runs on .NET Framework 4.6.1 which required us to drop support
for the elliptic curve account signers.