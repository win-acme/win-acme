---
sidebar: reference
---

# Target plugins

A target plugin is responsible for providing information about a (potential) certificate to the rest of the program. 
Its primary purpose is to determine which host names should be included in the SAN list, but can also provide extra 
information such as the preferred common name or bindings to exclude.

## Default

There is no default target plugin, it always has to be chosen by the user.