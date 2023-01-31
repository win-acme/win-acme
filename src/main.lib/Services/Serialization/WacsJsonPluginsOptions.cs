using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    public class WacsJsonPluginsOptionsFactory
    {
        private readonly ILogService _log;
        private readonly ISettingsService _settings;

        public WacsJsonPluginsOptionsFactory(ILogService log, ISettingsService settings) 
        {
            _log = log;
            _settings = settings;
        }

        /// <summary>
        /// Return new instance every time because we can create
        /// several unique contexts using the same options (e.g.
        /// for plugins)
        /// </summary>
        public JsonSerializerOptions Options
        {
            get
            {
                var opt = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                opt.Converters.Add(new ProtectedStringConverter(_log, _settings));
                return opt;
            }
        }
    }
}
