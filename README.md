# letsencrypt-win-simple
A Simple ACME Client for the Windows Platform

# Overview

This is a windows CLI client that's built in native .net and aims to be as simple as possible to use.

It's built on top of the [.net ACME protocol library](https://github.com/ebekker/letsencrypt-win/).

# Usage

The current build can be downloaded and run from https://mob0.com/Lets%20Encrypt%20Windows%20CLI%200.5.zip

It requires administrator privileges so be sure to run it from an elevated command prompt.

# Build Notes

To get the project to build correctly you may need to copy the "packages" folder that nuget creates into the letsencrypt-win folder.
