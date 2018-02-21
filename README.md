# Windows ACME Simple (WACS)
A simple ACME client for Windows - for use with Let's Encrypt.
(Formerly known as letsencrypt-win-simple (LEWS))

# Overview
This is a ACME CLI client for Windows built in native .NET and aims to be as simple as possible to use. It's built on top of the [ACMESharp project](https://github.com/ebekker/ACMESharp).

# Running
Download the [latest release](https://github.com/Lone-Coder/letsencrypt-win-simple/releases), unpack and run `letsencrypt.exe`, and follow the messages in the input prompt. There are some useful [command line arguments](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/Command-Line-Arguments) which can help with advanced or unattended usage scenarios.

# Settings
Some of the applications' settings can be updated in the app's settings or configuration file. the file is in the application root and is called `letsencrypt.exe.config`. The settings are documented on [this page](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/Application-Settings).

# Wiki
Please head to the [Wiki](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki) to learn more.

# Support
If you run into trouble please open an issue [here](https://github.com/Lone-Coder/letsencrypt-win-simple/issues). Please check to see if your issue is covered in the [Wiki](https://github.com/Lone-Coder/letsencrypt-win-simple/wiki) before you create a new issue. Describe the exact steps you took and try to reproduce it while running with the `--verbose` command line option set. Post your command line and the console output to help us debug.
