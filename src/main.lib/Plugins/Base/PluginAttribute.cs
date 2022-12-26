using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.Base
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PluginAttribute : Attribute
    {
        public Guid Id { get; set; }
        public PluginAttribute(string guid) => Id = new Guid(guid);
    }

    public abstract class Plugin2Attribute : Attribute
    {
        public abstract Guid Id { get; }
        public abstract Type Options { get; }
        public abstract Type OptionsFactory { get; }
        public abstract Type JsonContext { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class Plugin2Attribute<TOptions, TOptionsFactory, TJson> : Plugin2Attribute
        where TOptions: PluginOptions
        where TOptionsFactory: IPluginOptionsFactory<TOptions>
        where TJson : JsonSerializerContext
    {
        public override Guid Id { get; }
        public override Type Options { get; }
        public override Type OptionsFactory { get; }
        public override Type JsonContext { get; }

        public Plugin2Attribute(string id)
        {
            Id = Guid.Parse(id);
            Options = typeof(TOptions);
            OptionsFactory = typeof(TOptionsFactory);
            JsonContext = typeof(TJson);
        }       
    }
}