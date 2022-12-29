using System.Text.Json;

namespace PKISharp.WACS.Services.Serialization
{
    internal class WacsJsonPluginsOptionsFactory
    {
        public WacsJsonPluginsOptionsFactory(ILogService log, ISettingsService settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new ProtectedStringConverter(log, settings));
            Options = options;
        }
        public JsonSerializerOptions Options { get; }
    }
}
