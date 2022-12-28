using Autofac;
using Serilog;
using System.Text.Json;

namespace PKISharp.WACS.Services.Serialization
{
    internal class WacsJsonOptionsFactory
    {
        public WacsJsonOptionsFactory(ILifetimeScope context) 
        {
            var log = context.Resolve<ILogService>();
            var settings = context.Resolve<ISettingsService>();
            var pluginConverter = new PluginOptionsConverter(context);
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
