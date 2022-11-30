using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Non-generic base class needed for serialization
    /// </summary>
    public abstract class PluginOptions
    {
        public PluginOptions() => Plugin = GetType().PluginId();

        /// <summary>
        /// Contains the unique GUID of the plugin
        /// </summary>
        public string? Plugin { get; set; }

        /// <summary>
        /// Describe the plugin to the user
        /// </summary>
        /// <param name="input"></param>
        public virtual void Show(IInputService input) { }

        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public abstract Type Instance { get; }

        /// <summary>
        /// Short name for the plugin to be shown in the menu and e-mails
        /// </summary>
        [JsonIgnore]
        public abstract string Name { get; }

        /// <summary>
        /// One-line description for the plugin to be shown in the menu
        /// </summary>
        [JsonIgnore]
        public abstract string Description { get; }
    }

    /// <summary>
    /// Read flat PluginOptions objects from JSON and convert them into 
    /// the propery strongly typed object required by the plugin
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    internal class PluginOptionsConverter<TOptions> : 
        JsonConverter<TOptions> 
        where TOptions : PluginOptions
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
                if (key == null)
                {
                    _log.Warning("No PluginId found on plugin {p}", p.FullName);
                }
                else if (!_pluginsOptions.ContainsKey(key))
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

        /// <summary>
        /// Is conversion possible?
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) => typeof(TOptions) == objectType;

        /// <summary>
        /// Override reading to allow strongly typed object return, based on Plugin
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override TOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var readerClone = reader;
            var neutral = JsonSerializer.Deserialize(ref readerClone, typeof(TOptions)) as TOptions;
            var key = neutral?.Plugin;
            if (key == null)
            {
                return null;
            }
            var plugin = _pluginsOptions.TryGetValue(key, out var value) ? value : null;
            if (plugin != null)
            {
                return JsonSerializer.Deserialize(ref reader, plugin, options) as TOptions;
            }
            else
            {
                return JsonSerializer.Deserialize(ref reader, typeof(TOptions)) as TOptions;
            }
        }

        public override void Write(Utf8JsonWriter writer, TOptions value, JsonSerializerOptions options) => 
            JsonSerializer.Serialize(writer, value, options);
    }
}
