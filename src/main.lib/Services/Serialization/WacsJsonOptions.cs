using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    internal class WacsJsonLegacyOptionsFactory
    {
        /// <summary>
        /// Read options contains some backwards compatiblity shims
        /// </summary>
        /// <param name="pluginConverter"></param>
        /// <param name="log"></param>
        /// <param name="settings"></param>
        public WacsJsonLegacyOptionsFactory(PluginOptionsConverter pluginConverter, ILogService log, ISettingsService settings) 
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new ProtectedStringConverter(log, settings));
            options.Converters.Add(new StoresPluginOptionsConverter(pluginConverter));
            options.Converters.Add(pluginConverter);
            Options = options;
        }
        public JsonSerializerOptions Options { get; }
    }

    /// <summary>
    /// Write options writes modern style
    /// </summary>
    internal class WacsJsonOptionsFactory
    {
        public WacsJsonOptionsFactory(PluginOptionsConverter pluginConverter, ILogService log, ISettingsService settings)
        {    
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new ProtectedStringConverter(log, settings));
            options.Converters.Add(pluginConverter);
            Options = options;
        }
        public JsonSerializerOptions Options { get; }
    }
}
