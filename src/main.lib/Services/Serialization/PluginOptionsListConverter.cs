using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Read flat PluginOptions objects from JSON and convert them into 
    /// the propery strongly typed object required by the plugin
    /// </summary>
    internal class PluginOptionsListConverter : JsonConverter<List<StorePluginOptions>>
    {
        private readonly PluginOptionsConverter _child;

        public PluginOptionsListConverter(PluginOptionsConverter child) => _child = child;

        public override List<StorePluginOptions>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
        {
            var ret = new List<StorePluginOptions>();
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                if (_child.Read(ref reader, typeof(StorePluginOptions), options) is StorePluginOptions read)
                {
                    ret.Add(read);
                }
            } 
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }
                    if (_child.Read(ref reader, typeof(StorePluginOptions), options) is StorePluginOptions read)
                    {
                        ret.Add(read);
                    }
                }
            }
            return ret;
        }

        public override void Write(Utf8JsonWriter writer, List<StorePluginOptions> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                _child.Write(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}
