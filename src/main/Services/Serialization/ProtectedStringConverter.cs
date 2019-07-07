using Newtonsoft.Json;
using System;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// forces a re-calculation of the protected data according to current machine setting in EncryptConfig when
    /// writing the json for renewals and options for plugins
    /// </summary>
    public class ProtectedStringConverter : JsonConverter
    {
        private ILogService _log;

        public ProtectedStringConverter(ILogService log)
        {
            _log = log;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ProtectedString);
        }

        public override void WriteJson(JsonWriter writer, object protectedStr, JsonSerializer serializer)
        {
            writer.WriteValue((protectedStr as ProtectedString).DiskValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //allows a user to manually edit the renewal file and enter a password in clear text
            string s = (string)reader.Value;
            return new ProtectedString(s, _log);
        }
    }
}