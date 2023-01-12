using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Write options writes modern style
    /// </summary>
    internal class WacsJsonOptionsFactory
    {
        public WacsJsonOptionsFactory(
            PluginOptionsConverter pluginConverter,
            PluginOptionsListConverter pluginOptionsListConverter,
            ILogService log,
            ISettingsService settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new ProtectedStringConverter(log, settings));
            options.Converters.Add(pluginOptionsListConverter);
            options.Converters.Add(pluginConverter);
            Options = options;
        }
        public JsonSerializerOptions Options { get; }
    }
}
