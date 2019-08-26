---
sidebar: manual
---

# Migration from v1.9.5+ to v1.9.9
This version introduced the ability to store information about renewals in a file instead of 
the registry. This has several advantages including easier replication, backups, etc.

By default this is only enable for new clean installs, but you can migrate manually if 
you want to. This assumes you already followed the [v1.9.5](/win-acme/manual/upgrading/to-v1.9.5) 
steps, or your initial install was on that version or higher. If not, 
replace `HKEY_LOCAL_MACHINE` with `HKEY_CURRENT_USER` and `%programdata%` with `%appdata%`.

- Start `regedit.exe` 
- Go to `HKEY_LOCAL_MACHINE\SOFTWARE\letsencrypt-win-simple\`
- For each key there take the following steps:
     - Find the matching folder in your `ConfigurationPath`, which defaults to `%programdata%\letsencrypt-win-simple`  
        - E.g. the key `https://acme-v01.api.letsencrypt.org/` matches with the folder `httpsacme-v01.api.letsencrypt.org`
     - Create a new file named `Renewals` in the matching folder
     - Copy the contents of the registry subkey `Renewals` to that file
- Delete or rename `HKEY_LOCAL_MACHINE\SOFTWARE\letsencrypt-win-simple\` to make sure the program doesn't find it anymore.