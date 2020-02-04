---
sidebar: manual
---

# Migration from v1.9.9+ to v2.0.x
Version 2.0.0 is **not** an xcopy update. Many small but potentially **breaking** changes have been made. 
See the [release notes](https://github.com/PKISharp/win-acme/releases/tag/v2.0.0.177) for details. This 
guide explains how to import the renewals, but you might need to take other steps depending on how you
use the tool.

## Pre-upgrade
When using an old version of LEWS or win-acme (1.9.7 or older), it's recommended to upgrade to the latest 
1.9.x version and forcing a renewal of all certificates before attempting to upgrade to 2.0.0. The reason
for this is that new versions of the 1.9.x series automatically apply some compatibility steps, which may 
not have been fully tested for the built-in converter to 2.0.0.

## Importing renewals
Importing from 1.9.x is pretty easy. In the main menu select the option `More options...` and then 
`Import renewals from LEWS/WACS 1.9.x`. The program will locate, convert and import the renewals 
from the previous version. It will also disable the scheduled task for the old version and create 
a new one for the new version.

The same thing can be accomplished unattended with the `--import` switch.

Note that the import process in versions before 2.1.4 will *not* create an account at the ACMEv2 server 
for you, so you will have to set one up (by manually renewing once) before you will be able to renew with
the scheduled task. In any case it's a good idea to review the imported renewals and monitor the 
progress of the first execution before declaring the conversion a success.