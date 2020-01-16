---
sidebar: manual
---

# Custom logging
The program users [Serilog](https://serilog.net/) for logging which is a powerful extensible library.

## Levels
win-acme uses the following five log levels:

- `Error` - Logs fatal or dangerous conditions
- `Warning` - Logs minor errors and suspicious conditions
- `Information` - General information about operations
- `Debug` - Additional information that can be useful for troubleshooting
- `Verbose` - Full logging for submitting bug reports

You can change the log level by adding the following setting:

`<add key="serilog:minimum-level" value="Verbose" />`

## Included sinks
- The default sink logs to the console window to provide real time insights.
- The `event` sink writes to the Windows Event Viewer includes `Error`, `Warning` and selected `Information` messages.
- The `disk` sink writes rolling log files to `%programdata%\win-acme\log` 
  (that path can be changed in [settings.json](/win-acme/reference/settings))

## Custom sinks
There are many types of output channels called [sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks) for all
kinds of different databases, file formats and services.

### Example (Seq)

- Download `Serilog.Sinks.PeriodicBatching.dll` and `Serilog.Sinks.Seq.dll` from NuGet. These files can be found 
[here](https://www.nuget.org/packages/Serilog.Sinks.PeriodicBatching) and 
[here](https://www.nuget.org/packages/Serilog.Sinks.Seq), respectively.
- Configure the sink in a file called `serilog.json` according to the specification [here](https://github.com/serilog/serilog-settings-configuration).

### Example

The follow piece of code in `serilog.json` adds the process ID to the output of the file log.

```json
{
	"disk": {
		"WriteTo": [
			{ 
				"Name": "File",
				"Args": { 
					"outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [PID:{ProcessId}] {Message:lj}{NewLine}{Exception}"
				} 
			}
		]
	}
}
```