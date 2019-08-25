---
sidebar: manual
---

# Automatic renewal

## Scheduled task
A single scheduled task is responsible for renewing *all* certificates created by the program. 
The task is created by the program itself after successfully issuing the first certificate. 
The task runs every day and check two conditions to determine if it should request and install a new 
certificate for each renewal:
- If the last *successful* renewal happened too long ago. This is based on the known history stored in the file.
- If the target has changed since the last successful renewal, e.g. an extra binding has been added to an IIS site.

### Customization
The default renewal period of 55 days can be changed in [settings.config](/win-acme/reference/settings).
Other properties of the scheduled task can also be changed that way, or from the Task Scheduler itself,
as long as the name is not modified. By default it runs at 9:00am using the `SYSTEM` account.

### Health checks
The health of the scheduled task is checked each time the program is run manually. It can also 
be (re)created from the menu (`More options...` > `(Re)create scheduled task`).

## Monitoring
The renewal process can be monitored from the Windows Event Viewer and log files 
written to `%programdata%\win-acme\logs`. You can also set up email notifications 
by configuring a mail server in [settings.config](/win-acme/reference/settings). 
You can test these notifications from the menu (`More options...` > `Test email notification`).

## Testing and troubleshooting
To test or troubleshoot the renewal process, renewals can be triggered manually from the menu or the 
command line with the `--renew --force` switches. We recommend doing so while running with the 
`--verbose` parameter to get maximum log visibility. When listing the details for a renewal, the 
program will show any errors that have been recorded during previous runs.