using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services.Serialization
{
    public abstract class PluginOptions
    {
        public PluginOptions()
        {
            Plugin = GetType().PluginId();
        }

        public string Plugin { get; set; }

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
        private readonly IDictionary<string, Type> _pluginsOptions;

        public PluginOptionsConverter(IEnumerable<Type> plugins, ILogService _log)
        {
            _pluginsOptions = new Dictionary<string, Type>();
            foreach (var p in plugins)
            {
                var key = p.PluginId();
                if (!_pluginsOptions.ContainsKey(key))
                {
                    _pluginsOptions.Add(key, p);
                }
                else
                {
                    var existing = _pluginsOptions[key];
                    _log.Warning(
                        "Duplicate plugin with key {key}. " +
                        "{p.FullName} from {p.Assembly.Location} and " +
                        "{existing.FullName} from {existing.Assembly.Location}",
                        key,
                        p.FullName, p.Assembly.Location,
                        existing.FullName, existing.Assembly.Location);
                }
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TOptions);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var data = JObject.Load(reader);
            var key = data.Property("Plugin").Value.Value<string>();
            var plugin = _pluginsOptions.ContainsKey(key) ? _pluginsOptions[key] : null;
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
