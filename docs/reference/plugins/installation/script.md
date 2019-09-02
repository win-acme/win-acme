---
sidebar: reference
---

# Script
Runs an external script or executable after a succesful renewal. This may be a `.bat`, `.ps1` or even `.exe`.
You provide the program with the path to the script and it will run automatically.

## Parameters
The following variables can be provided from the program to the script as command line arguments.

| Value          |  Replaced with |
|----------------|----------------|
| `{0}` or `{CertCommonName}` | Common name (primary domain name)                                |
| `{1}` or `{CachePassword}` | The .pfx password (generated randomly for each renewal)  |
| `{2}` or `{CacheFile}`       | Full path of the cached.pfx file                                 |
| `{4}` or `{CertFriendlyName}`       |  Friendly name of the generated certificate                               |
| `{5}` or `{CertThumbprint}`      | Thumbprint of the generated certificate                             |
| `{7}` or `{RenewalId}`       | Id of the renewal                                    |
| `{3}` or `{6}` or `{StorePath}`      | Path or store name used by the (first) store plugin                                   |
| `{StoreType}`        |  Name of the plugin (CentralSsl, CertificateStore or PemFiles)                                    |

## Example
If you need your scripts parameters to look something like this:

`action=import file=C:\mydomain.pfx password=*****`

Then your argument string should look like this:

`action=import file={CacheFile} password={CachePassword}`

## Unattended 
`--installation script --script C:\script.bat [--scriptparameters x]`

### Parameter escaping
If you need to put double quotes around your parameters from the command line, you have to escape them with a slash, for example:

`--scriptparameters "action=import file=\"{CacheFile}\" password=\"{CachePassword}\""`

For **Powershell** scripts, string parameters can also be delimited with single quotes, for example:

`--scriptparameters "action=import file='{CacheFile}' password='{CachePassword}'"`