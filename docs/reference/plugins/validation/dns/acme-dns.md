---
sidebar: reference
---

# acme-dns
Use an [acme-dns](https://github.com/joohoi/acme-dns) server to handle the validation records. 
The plugin will ask you to choose an endpoint to use. For testing the `https://auth.acme-dns.io/` 
endpoint is useful, but it is a security concern. As the readme of that project clearly states: 

> "You are encouraged to run your own acme-dns instance."

It's possible to use basic authentication for your acme-dns service by specifying a url with 
the format `https://user:password@acme-dns.example.com/`

## Unattended
Not supported, unless there is a pre-existing acme-dns registration for all the domains. 
The reason for this is that acme-dns requires you to create CNAME records. In the future this 
might be scripted the same way we can script DNS validation itself, but so far there hasn't been
enough demand for that feature to make it worth developing.