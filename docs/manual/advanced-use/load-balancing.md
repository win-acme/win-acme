---
sidebar: manual
---

# Load balancing
Some pointers on win-acme and load balancing.

## Sharing certificates between servers
It really depends if you're using a separate appliance to offload HTTPS or if it's 
handled by the servers in the pool themselves. In the latter case you should probably 
use the [Central Certificate Store](https://blogs.msdn.microsoft.com/kaushal/2012/10/11/central-certificate-store-ccs-with-iis-8-windows-server-2012/) 
feature of IIS. Instructions on how to configure win-acme to use it can be found 
[here](/win-acme/reference/plugins/store/centralssl).

## Scheduled task
- You can have a single server act as a renewal server running win-acme. That means it's a single 
  point of failure, but only a minor one, because certificates only need to be renewed once every
  three months.
- To distribute the task of renewing, you should point the `ConfigurationPath` in the `settings.json` 
  of win-acme to somewhere on your SAN, so that any member of the pool can potentially renew the 
  certificates. 
- You can configure the Scheduled Task on different machines at different times, e.g. one at 4:00 am, 
  the next at 5:00 am, etc. Then you can be sure that they will not run at the same time and the first 
  one that succeeds handles everything.
- If you're building an actual cluster, you can use a Clustered Task instead of a regular Scheduled Task.

## Encryption
The encryption for the config files will have to be disabled via `settings.json` so that all machines 
in the cluster can read the passwords.

## Appliance
If you are using an appliance then you have to use their API and call into that from a `.bat`/`.ps1`/`.exe` 
using an [installation script](/win-acme/reference/plugins/installation). 