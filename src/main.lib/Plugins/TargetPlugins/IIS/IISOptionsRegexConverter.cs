using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISOptionsRegexConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var ret = default(string?);
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        if (propertyName == "Pattern")
                        {
                            reader.Read();
                            ret = reader.GetString();
                        }
                    }
                    reader.Read();
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                ret = reader.GetString();
            }
            return ret;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => 
            writer.WriteStringValue(value);
    }
}
