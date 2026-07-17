# Gandi LiveDNS validation plugin

Answers ACME `dns-01` challenges by creating (and cleaning up) TXT records in
[Gandi LiveDNS](https://api.gandi.net/docs/livedns/). It works the same way as
the Cloudflare plugin: it looks up the correct zone in your account, writes the
`_acme-challenge` record, and removes it again after validation.

## Requirements

- The domain must use **Gandi LiveDNS** as its nameservers.
- A **Personal Access Token** (or a legacy API key) with permission to manage
  the DNS records of that domain. Create one under
  *Account → Personal Access Tokens* in the Gandi dashboard.

## Installation

Plugins are loaded from the directory that contains `wacs.exe`. Build the
project and copy the output next to `wacs.exe`:

```
dotnet publish src/plugin.validation.dns.gandi/wacs.validation.dns.gandi.csproj -c Release
```

Copy these files into the win-acme folder:

- `PKISharp.WACS.Plugins.ValidationPlugins.Gandi.dll`
- `Newtonsoft.Json.dll`

On the next start, **Gandi** appears in the list of DNS validation plugins.

## Interactive use

Run `wacs.exe`, choose to create a certificate with full options (`M`), and pick
**Gandi** at the validation step. When prompted, paste your token:

```
Gandi API Token: ********
```

## Unattended / command line

```
wacs.exe --source manual --host example.com,*.example.com ^
         --validation gandi --gandiapitoken "pat_xxxxxxxx"
```

| Argument            | Description                                              |
| ------------------- | ------------------------------------------------------- |
| `--validation gandi`| Select the Gandi validation plugin.                     |
| `--gandiapitoken`   | Personal Access Token (or legacy API key). Stored encrypted in the renewal. |

The token is saved encrypted after the first run, so scheduled renewals keep
working without further input.

## How it works

1. `GET /v5/livedns/domains` lists the domains in your account.
2. The most specific matching zone is selected for the requested host name.
3. `PUT /v5/livedns/domains/{fqdn}/records/{name}/TXT` writes the validation
   value. Existing TXT values on the same name are preserved, so a wildcard and
   its base domain can validate at the same time.
4. After validation the plugin removes only its own value, deleting the record
   set entirely once nothing else remains.
