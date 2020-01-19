---
sidebar: reference
---

# Installation plugins
Installation plugins are responsible for making the necessary changes to your 
application(s) after successfully creating or renewing a certificate. Currently 
there are three of these plugins.

## Multiple
More than one plugin can run by choosing them in order of execution. In interactive 
mode you will be asked, for unattended mode you can provide a comma seperated list, 
e.g. `--installation certificatestore,pemfiles`

## Default (simple mode)
In simple mode the default installation plugin is [IIS Web](/win-acme/reference/plugins/installation/iisweb). 

## Default (full options / unattended)
In full options and unattended modes there are **no** default installation steps,
which is equivalent to `--installation none`. You can to explicitly choose them 
from the interface or using the `--installation` switch. 