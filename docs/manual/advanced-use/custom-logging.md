---
sidebar: manual
---

# Custom logging

The program users [Serilog](https://serilog.net/) for logging which is a powerful extensible library. By default the 
program logs to the console window and selected events are persisted into the Windows Event Viewer and rolling log files
located in `%programdata%\win-acme\log`, but you may want to extend this based on your infrastructure.

# Seq example

- Download `Serilog.Sinks.PeriodicBatching.dll` and `Serilog.Sinks.Seq.dll` from NuGet. These files can be found 
[here](https://www.nuget.org/packages/Serilog.Sinks.PeriodicBatching) and 
[here](https://www.nuget.org/packages/Serilog.Sinks.Seq), respectively.
- Add the following lines to `wacs.exe.config`

```XML
<add key="serilog:using:Seq" value="Serilog.Sinks.Seq" />
<add key="serilog:write-to:Seq.serverUrl" value="http://localhost:5341" />
````

# Changing the Log Level

You can change the log level by adding the following setting:

```XML
<add key="serilog:minimum-level" value="Verbose" />
```

Additional information on leg levels can be found [here](https://github.com/serilog/serilog/wiki/Configuration-Basics).

# Other sinks
There are many types of output channels called [sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks) for all
kinds of different databases, file formats and services.