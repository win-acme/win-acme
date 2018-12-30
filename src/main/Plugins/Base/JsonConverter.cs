using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.Base
{
    public abstract class PluginOptions
    {
        public PluginOptions()
        {
            Plugin = GetType().FullName;
        }

        public virtual string Plugin { get; set; }

        public virtual void Show(IInputService input) { }

        [JsonIgnore]
        public virtual Type Instance { get; }
        [JsonIgnore]
        public virtual string Name { get => null; }
        [JsonIgnore]
        public virtual string Description { get => null; }
    }

    class PluginOptionsConverter<TOptions> : JsonConverter where TOptions: PluginOptions
    {
        private readonly IEnumerable<Type> _pluginsOptions;

        public PluginOptionsConverter(IEnumerable<Type> plugins)
        {
            _pluginsOptions = plugins;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TOptions);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var data = JObject.Load(reader);
            var key = data.Property("Plugin").Value.Value<string>();
            var plugin = _pluginsOptions.FirstOrDefault(type => type.FullName == key);
            if (plugin != null)
            {
                return data.ToObject(plugin, serializer);
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
