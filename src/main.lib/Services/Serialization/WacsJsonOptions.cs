using Autofac;
using Serilog;
using System.Text.Json;

namespace PKISharp.WACS.Services.Serialization
{
    internal class WacsJsonOptionsFactory
    {
        public WacsJsonOptionsFactory(ILogService log, ISettingsService settings, IPluginService plugin) 
        {
            var pluginConverter = new PluginOptionsConverter(plugin);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new ProtectedStringConverter(log, settings));
            options.Converters.Add(new StoresPluginOptionsConverter(pluginConverter));
            options.Converters.Add(pluginConverter);
            Options = options;
        }
        public JsonSerializerOptions Options { get; }
    }
}
