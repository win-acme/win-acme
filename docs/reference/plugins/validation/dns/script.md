---
sidebar: reference
---

# Script
Run an external script or program to create or update the validation records.

## Create
A script to create the DNS record must be provided. The arguments passed to the 
script will be `create {Identifier} {RecordName} {Token}` by default, where the
following replacements are made by win-acme:

| Value          |  Replaced with |
|----------------|----------------|
| `{Identifier}` | host name that's being validated, e.g. `sub.example.com`                                    |
| `{RecordName}` | full name of the TXT record that is being expected, e.g. `_acme-challenge.sub.example.com`  |
| `{Token}`      | content of the TXT record, e.g. `DGyRejmCefe7v4NfDGDKfA`                                    |

The order and format of arguments may be customized by providing a diffent argument string. 
For example if your script needs arguments like:

`--host _acme-challenge.example.com --token DGyRejmCefe7v4NfDGDKfA`

...your argument string should like like this: 

`--host {RecordName} --token {Token}`

## Delete
Optionally, another script may be provided to delete the record after validation. The arguments passed to the 
script will be `delete {Identifier} {RecordName} {Token}` by default. The order and format of arguments may be 
customized by providing a diffent argument string, just like for the create script. You can also choose to use 
the same script for create and delete, with each their own argument string.

## Resources
A lot of good example scripts are available from the 
[POSH-ACME](https://github.com/rmbolger/Posh-ACME/tree/master/Posh-ACME/DnsPlugins)
project.

## Unattended
- ##### Create script only
`-validationmode dns-01 --validation script --dnscreatescript c:\create.ps1 [--dnscreatescriptarguments {args}]`
- ##### Create and delete scripts seperate
`-validationmode dns-01 --validation script --dnscreatescript c:\create.ps1 --dnsdeletescript c:\delete.ps1 [--dnscreatescriptarguments {args}] [--dnsdeletescriptarguments {args}]`
- ##### Create-delete script (integrated)
`-validationmode dns-01 --validation script --dnsscript c:\create-and-delete.ps1 [--dnscreatescriptarguments {args}] [--dnsdeletescriptarguments {args}]`