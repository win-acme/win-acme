---
sidebar: reference
---

# IIS sites
Create target based on all bindings of multiple IIS sites. 
- Automatically updates webroot paths (useful for [FileSystem validation](/win-acme/reference/plugins/validation/http/filesystem))
- Automatically adds/removes host names based on bindings

## Unattended 
`--target iissites --siteid 1,2,3 [--commonname common.example.com] [--excludebindings exclude.example.com]`