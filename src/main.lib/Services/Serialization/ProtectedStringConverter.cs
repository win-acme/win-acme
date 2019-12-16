using Newtonsoft.Json;
using System;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// forces a re-calculation of the protected data according to current machine setting in EncryptConfig when
    /// writing the json for renewals and options for plugins
    /// </summary>
    public class ProtectedStringConverter : JsonConverter<ProtectedString>
    {
        private readonly ILogService _log;
        private readonly ISettingsService _settings;

        public ProtectedStringConverter(ILogService log, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
        }

        public override void WriteJson(JsonWriter writer, ProtectedString value, JsonSerializer serializer) => writer.WriteValue(value.DiskValue(_settings.Security.EncryptConfig));
       
        public override ProtectedString ReadJson(JsonReader reader, Type objectType, ProtectedString existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            //allows a user to manually edit the renewal file and enter a password in clear text
            var s = (string)reader.Value;
            return new ProtectedString(s, _log);
        }
    }
}