using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKISharp.WACS.Extensions;
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

        public PluginOptionsConverter(ILogService _log, IEnumerable<Type> plugins)
        {
            _pluginsOptions = new Dictionary<string, Type>();
            foreach (var p in plugins)
            {
                string id = p.PluginId();
                if (_pluginsOptions.ContainsKey(id))
                {
                    _log.Warning("Duplicate Plugin GUID ({0}). Using {1} instead of {2}", id, p.FullName, _pluginsOptions[id].FullName);
                }
                _pluginsOptions[id] = p;
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
