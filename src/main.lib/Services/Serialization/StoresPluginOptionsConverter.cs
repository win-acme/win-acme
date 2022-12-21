using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Convert StorePluginOptions in legacy JSON to List<StorePluginOptions> for 2.0.7+
    /// </summary>
    internal class StoresPluginOptionsConverter : JsonConverter<List<StorePluginOptions>>
    {
        private readonly JsonConverter _childConverter;

        public StoresPluginOptionsConverter(JsonConverter childConverter) => _childConverter = childConverter;

        public override bool CanConvert(Type objectType) => objectType == typeof(List<StorePluginOptions>);

        public override List<StorePluginOptions>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var childOptions = new JsonSerializerOptions();
            childOptions.Converters.Add(_childConverter);
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                return JsonSerializer.Deserialize<List<StorePluginOptions>>(ref reader, childOptions);
            }
            else
            {
                var list = new List<StorePluginOptions>();
                var option = JsonSerializer.Deserialize<StorePluginOptions>(ref reader, childOptions);
                if (option != null)
                {
                    list.Add(option);
                }
                return list;
            }
        }

        public override void Write(Utf8JsonWriter writer, List<StorePluginOptions> value, JsonSerializerOptions options) => 
            throw new NotImplementedException();
    }
}
