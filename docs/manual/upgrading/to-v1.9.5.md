---
sidebar: manual
---

# Migration from <=1.9.4 to v1.9.5
Version 1.9.5 and later store information in system-wide folders and registry locations by 
default, but still support reading configuration data from user specific locations. As such 
it is backwards compatible with 1.9.4 and earlier.

## Registry (optional)
- Export the key `HKEY_CURRENT_USER\Software\letsencrypt-win-simple`
- Delete the key 
- Open the .reg file with your favorite text editor
- Replace `HKEY_CURRENT_USER` for `HKEY_LOCAL_MACHINE` and save it
- Import the .reg file

## File system (optional)
Move the folder `%appdata%\letsencrypt-win-simple` to `%programdata%\letsencrypt-win-simple`. 
Make sure the original is deleted, because the program prefers the old one over the new one.

## Scheduled task (optional)
It should be changed to run as `SYSTEM`. Running under a specific user does not seem to work
if the task is running without a password specified.