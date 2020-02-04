---
sidebar: manual
---

# Renewal management
This program is primarily used to create certificates, but the nature of ACME encourages certificates to be 
replaced regularly. We call a sequence of certificates, created with specific settings, a **renewal**. It's the 
basic unit of work that you manage with the program.

## Creation
- Creating a renewal can be done interactively from the main menu. The option `N` uses the easiest defaults for 
IIS users and the option `M` offers full options, for example for Apache, Exchange, wildcard certificates, etc. 
- Any certificate that can be created from the main menu, can also be created from the 
[command line](/win-acme/reference/cli). 
The command line even offers some options that the menu does not, check out the documentation 
about [plugins](/win-acme/reference/plugins/) to read all about it.
- It's also possible to add `.json` files to the folder yourself, either manually or using some clever tooling or 
scripting, to create a lighty coupled integration between your own management tools and win-acme.

## Modification
Many users mistakenly try to modify their renewal by issuing commands like `--renew --webroot C:\NewRoot` 
hoping that the configured webroot for their renewal will be changed. The reason this doesn't work is 
because a renew cycle checks **all** renewals, each of which can use any of the hundreds of possible 
combinations of [plugins](/win-acme/reference/plugins/), so it's complex to figure out what the 
true intention of such a command should be. Therefore, modification and renewal are completely separate
functions.

Modifying a renewal is essential the same as re-creating it, either from the command line or the main menu. 
If it turns out that a newly configured certificate has the same friendly name as a previously created one, 
then the older settings will be overwritten. In interactive mode the user is asked to confirm this. In 
unattended mode the script or program calling win-acme is assumed to know the consequences of its actions.

## Deleting/cancelling
To cancel a renewal means that the certificate will not be renewed anymore. The certificate, bindings 
and other configuration that is already in place will **not** be touched, so it's completely safe to do
this without disturbing your production applications. Only you will have to set up a new renewal or 
alternative certificate solution before the certificate reaches its natural expiration date. 
- You can cancel a renewal from the main menu. The program will then delete the `.json` file from 
disk and forget about it.
- You can cancel from the command line using the arguments `--cancel [--friendlyname xxx|-id xxx]`. 
The effects are the same as above.
- You can delete the `.json` file yourself. The effects are the same as above.

## Revocation
Revoking a certificate should only be done when the private key is believed to have been compromised, 
not when simply replacing or cancelling it. Revocation can be done from the main menu with
(`Manage renewals` > `Revoke certificate`)
- You can revoke from the command line using the arguments `--revoke [--friendlyname xxx|-id xxx]`. 
The effects are the same as above.

## Internals
Renewals are stored in the `ConfigPath` which typically means `%ProgramData%\win-acme\acme-v02.api.letsencrypt.org`, 
though that can be changed in [settings.json](/win-acme/reference/settings). Each file that fits the pattern 
`*.renewal.json` is considered to be a renewal. 

### File names
The files are randomly named by the program, but you are free to rename them if that suits you. The only requirement 
is that they must be unique, which is enforced by checking that the `"Id"` field in the JSON must match with the 
name of file. You can specify your own identifier at creation time with the `--id` switch.

### File content
The renewal files consist of three parts:
- Metadata, e.g. it's identifier, friendly name and the encrypted version of the password that is used for 
the cached `.pfx` archive.
- Plugin configuration, e.g. everything that the [plugins](/win-acme/reference/plugins/) need to know 
to do their jobs, according to the command line arguments and menu choices that were given at creation time.
- History, i.e. a record of each successful and failed attempt to get a certificate based on the file.