---
sidebar: reference
---

# IIS site
Create target based on all bindings of an IIS site. 
- Automatically updates webroot path (useful for [FileSystem validation](/win-acme/reference/plugins/validation/http/filesystem))
- Automatically adds/removes host names based on bindings

## Unattended 
`--target iissite --siteid 1 [--commonname common.example.com] [--excludebindings exclude.example.com]`