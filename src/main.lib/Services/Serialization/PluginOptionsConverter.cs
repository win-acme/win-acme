using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
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
    /// <typeparam name="TOptions"></typeparam>
    [Obsolete]
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

    /// <summary>
    /// Read flat PluginOptions objects from JSON and convert them into 
    /// the propery strongly typed object required by the plugin
    /// </summary>
    internal class Plugin2OptionsConverter : JsonConverter<PluginOptionsBase>
    {
        private readonly IPluginService _pluginService;

        public Plugin2OptionsConverter(IPluginService plugin) => _pluginService = plugin;

        public override bool CanConvert(Type typeToConvert) => typeof(TargetPluginOptions).IsAssignableFrom(typeToConvert);

        /// <summary>
        /// Override reading to allow strongly typed object return, based on Plugin
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override PluginOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var readerClone = reader;
            var neutral = JsonSerializer.Deserialize(ref readerClone, WacsJson.Default.PluginOptionsBase);
            var plugin = neutral?.FindPlugin(_pluginService);
            if (plugin == null)
            {
                reader.Skip();
                return null;
            }
            return JsonSerializer.Deserialize(ref reader, plugin.Meta.Options, plugin.Meta.JsonContext) as PluginOptions;
        }

        /// <summary>
        /// Write plugin to string
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, PluginOptionsBase value, JsonSerializerOptions options)
        {
            var plugin = value.FindPlugin(_pluginService);
            if (plugin == null)
            {
                throw new Exception("Can't figure out for which plugin these options are");
            }
            if (string.IsNullOrWhiteSpace(value.Plugin))
            {
                // Add plugin identifier for future reference
                value.Plugin = plugin.Id.ToString();
            }
            else if (!string.Equals(value.Plugin, plugin.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Mismatch between detected plugin and pre-existing identifier");
            }
            JsonSerializer.Serialize(writer, value, plugin.Meta.Options, plugin.Meta.JsonContext);
        }
    }
}
