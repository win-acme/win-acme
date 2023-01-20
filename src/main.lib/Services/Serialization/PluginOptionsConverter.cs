using Autofac;
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
        private readonly ILogService _log;
        private readonly ILifetimeScope _scope;

        public PluginOptionsConverter(ILifetimeScope context) 
        {
            _pluginService = context.Resolve<IPluginService>();
            _log = context.Resolve<ILogService>();
            _scope = context;
        }

        public override bool CanConvert(Type typeToConvert) => typeof(PluginOptionsBase).IsAssignableFrom(typeToConvert);

        /// <summary>
        /// Override reading to allow strongly typed object return, based on PluginBackend
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override PluginOptionsBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var readerClone = reader;
            var neutral = JsonSerializer.Deserialize(ref readerClone, WacsJson.Insensitive.PluginOptionsBase);
            if (!_pluginService.TryGetPlugin(neutral, out var plugin))
            {
                _log.Error("Unable to find {typeToConvert} plugin {id}", 
                    typeToConvert.Name.Replace("PluginOptions", "").Replace("Target", "Source").ToLower(), 
                    neutral?.Plugin);
                reader.Skip();
                return null;
            }
            if (_scope.Resolve(plugin.OptionsJson) is not JsonSerializerContext context)
            {
                throw new Exception("Unable to create JsonSerializerContext");
            }
            return JsonSerializer.Deserialize(ref reader, plugin.Options, context) as PluginOptionsBase;
        }

        /// <summary>
        /// Write plugin to string
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, PluginOptionsBase value, JsonSerializerOptions options)
        {
            if (!_pluginService.TryGetPlugin(value, out var plugin))
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
            if (_scope.Resolve(plugin.OptionsJson) is not JsonSerializerContext context)
            {
                throw new Exception("Unable to create JsonSerializerContext");
            }
            JsonSerializer.Serialize(writer, value, plugin.Options, context);
        }
    }
}
