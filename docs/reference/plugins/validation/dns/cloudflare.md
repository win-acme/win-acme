---
sidebar: reference
---

# Cloudflare 
Create the record in Cloudflare DNS.

{% include plugin-seperate.md %}

## Setup
This assumes you already have your DNS managed in Cloudflare; if not, you'll need to set that up first. If you are 
using the Cloudflare DNS option for validation, you'll need to obtain a Cloudflare API Token (not Key) that is allowed
to read and write the DNS records of the zone your domain belongs to.

### Create an appropriate API Token
1. Navigate here: https://dash.cloudflare.com/profile/api-tokens
2. Click *Create Token*
3. Choose a name
4. Under *Permissions*, select "Zone", "DNS", "Edit"
5. Under *Zone Resources*, select "Include", "Specific Zone" and the dns zone you want to create certificates for.
6. Finish creating the token, store it in a safe place or, better, paste it directly into win-acme.

## Unattended 
`--validationmode dns-01 --validation cloudflare --cloudflareapitoken ***`