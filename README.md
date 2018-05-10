[![Build status](https://ci.appveyor.com/api/projects/status/c4b3t6g82yyjl4v1?svg=true)](https://ci.appveyor.com/project/WouterTinus/win-acme-s8t9q)

# Windows ACME Simple (WACS)
A simple ACME client for Windows - for use with Let's Encrypt. (Formerly known as letsencrypt-win-simple (LEWS))

# Overview
This is a ACME CLI client for Windows built in native .NET and aims to be as simple as possible to use. It's built on top of the [ACMESharp project](https://github.com/ebekker/ACMESharp).

# Running
Download the [latest release](https://github.com/PKISharp/win-acme/releases), unpack and run `letsencrypt.exe`, and follow the messages in the input prompt. There are some useful [command line arguments](https://github.com/PKISharp/win-acme/wiki/Command-Line-Arguments) which can help with advanced or unattended usage scenarios.

# Settings
Some of the applications' settings can be updated in the app's settings or configuration file. The is located in the application root and is called `settings.config` (created during the first run, based on `settings_default.config`). The settings are documented on [this page](https://github.com/PKISharp/win-acme/wiki/Application-Settings).

# Wiki
Please head to the [Wiki](https://github.com/PKISharp/win-acme/wiki) to learn more.

# Support
If you run into trouble please open an issue [here](https://github.com/PKISharp/win-acme/issues). Please check to see if your issue is covered in the [Wiki](https://github.com/PKISharp/win-acme/wiki) before you create a new issue. Describe the exact steps you took and try to reproduce it while running with the `--verbose` command line option set. Post your command line and the console output to help us debug.
