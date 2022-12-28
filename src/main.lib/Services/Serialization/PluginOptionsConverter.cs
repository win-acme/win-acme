using Autofac;
using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Read flat PluginOptions objects from JSON and convert them into 
    /// the propery strongly typed object required by the plugin
    /// </summary>
    internal class PluginOptionsConverter : JsonConverter<PluginOptionsBase>
    {
        private readonly IPluginService _pluginService;

        public PluginOptionsConverter(IPluginService plugin) => _pluginService = plugin;

        public override bool CanConvert(Type typeToConvert) => typeof(PluginOptionsBase).IsAssignableFrom(typeToConvert);

        /// <summary>
        /// Override reading to allow strongly typed object return, based on Plugin
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override PluginOptionsBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var readerClone = reader;
            var neutral = JsonSerializer.Deserialize(ref readerClone, WacsJson.Default.PluginOptionsBase);
            var plugin = neutral?.FindPlugin(_pluginService);
            if (plugin == null)
            {
                reader.Skip();
                return null;
            }
#if !PLUGGABLE
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#endif
            return JsonSerializer.Deserialize(ref reader, plugin.Meta.Options) as PluginOptionsBase;
#if !PLUGGABLE
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#endif

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
            JsonSerializer.Serialize(writer, value, plugin.Meta.Options);
        }
    }
}
