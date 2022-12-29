using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    internal class WacsJsonPluginsOptionsFactory
    {
        public WacsJsonPluginsOptionsFactory(ILogService log, ISettingsService settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new ProtectedStringConverter(log, settings));
            Options = options;
        }
        public JsonSerializerOptions Options { get; }
    }
}
