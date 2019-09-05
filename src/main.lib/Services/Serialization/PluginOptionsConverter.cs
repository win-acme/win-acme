using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Non-generic base class needed for serialization
    /// </summary>
    public abstract class PluginOptions
    {
        public PluginOptions()
        {
            Plugin = GetType().PluginId();
        }

        /// <summary>
        /// Contains the unique GUID of the plugin
        /// </summary>
        public string Plugin { get; set; }

        /// <summary>
        /// Describe the plugin to the user
        /// </summary>
        /// <param name="input"></param>
        public virtual void Show(IInputService input) { }

        [JsonIgnore]
        public virtual Type Instance { get; }

        /// <summary>
        /// Short name for the plugin to be shown in the menu and e-mails
        /// </summary>
        [JsonIgnore]
        public virtual string Name { get => null; }

        /// <summary>
        /// One-line description for the plugin to be shown in the menu
        /// </summary>
        [JsonIgnore]
        public virtual string Description { get => null; }
    }

    /// <summary>
    /// Read flat PluginOptions objects from JSON and convert them into 
    /// the propery strongly typed object required by the plugin
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    class PluginOptionsConverter<TOptions> : JsonConverter where TOptions: PluginOptions
    {

        /// <summary>
        /// Possible plugins to match with, indexed by GUID
        /// </summary>
        private readonly IDictionary<string, Type> _pluginsOptions;

        public PluginOptionsConverter(IEnumerable<Type> plugins, ILogService _log)
        {
            // Index plugins by key
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
                        "{Name1} from {Location1} and " +
                        "{Name2} from {Location2}",
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

        /// <summary>
        /// Override reading to allow strongly typed object return, based on Plugin
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Standard write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
