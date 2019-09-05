using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Convert StorePluginOptions in legacy JSON to List<StorePluginOptions> for 2.0.7+
    /// </summary>
    internal class StorePluginOptionsConverter : JsonConverter
    {
        private JsonConverter _childConverter;

        public StorePluginOptionsConverter(JsonConverter childConverter)
        {
            _childConverter = childConverter;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<StorePluginOptions>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var data = JArray.Load(reader);
                return data.
                    Children().
                    Select(x => Read(x.CreateReader(), serializer)).
                    ToList();
            }
            else
            {
                return new List<StorePluginOptions>() { Read(reader, serializer) };
            }
        }

        private StorePluginOptions Read(JsonReader reader, JsonSerializer serializer)
        {
            return (StorePluginOptions)_childConverter.ReadJson(reader, typeof(StorePluginOptions), null, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
