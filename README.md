# letsencrypt-win-simple
A Simple ACME Client for the Windows Platform

# Overview

This is a windows CLI client that's built in native .net and aims to be as simple as possible to use.

It's built on top of the [.net ACME protocol library](https://github.com/ebekker/letsencrypt-win/).

# Usage

The current build can be downloaded and run from https://github.com/Lone-Coder/letsencrypt-win-simple/releases

It requires administrator privileges so be sure to run it from an elevated command prompt.

Running the client will take you thru a menu system to get your certs and install them.

It will scan IIS for bindings with host names so you may need to add one for this client to work.

The client will write out an answer file that needs to be visible to the ACME server to verify domain ownership.

Certs .pfx files are written to disk currently as well as optionally imported into the windows cert store.

The client can also create a https binding in IIS for you.

There's no support for AWS or Azure sites yet. Pull requests for them are welcome.

Automatic renewals are not working yet, so you'll need to renew your certs before their 90 day expiration.

# Build Notes

To get the project to build correctly you may need to copy the "packages" folder that nuget creates into the letsencrypt-win folder.
