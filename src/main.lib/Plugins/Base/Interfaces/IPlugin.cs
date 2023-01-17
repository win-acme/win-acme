using ACMESharp.Authorizations;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IPlugin
    {
        /// <summary>
        /// Mark a class as a plugin
        /// Only possible on types that implement IPlugin, as per 
        /// https://blog.marcgravell.com/2009/06/restricting-attribute-usage.html
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <typeparam name="TOptionsFactory"></typeparam>
        /// <typeparam name="TJson"></typeparam>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        protected sealed class PluginAttribute<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptions,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptionsFactory,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCapability,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TJson> : 
            Attribute, IPluginMeta
            where TOptions : PluginOptions, new()
            where TOptionsFactory : IPluginOptionsFactory<TOptions>
            where TJson : JsonSerializerContext
        {
            public Guid Id { get; }
            public bool Hidden { get; set; } = false;
            public string Name { get; set; }
            public string Description { get; set; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            public Type Options { get; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            public Type OptionsFactory { get; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            public Type OptionsJson { get; }
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            public Type Capability { get; }

            public PluginAttribute(string id, string name, string description)
            {
                Id = Guid.Parse(id);
                Options = typeof(TOptions);
                OptionsFactory = typeof(TOptionsFactory);
                OptionsJson = typeof(TJson);
                Capability = typeof(TCapability);
                Name = name; 
                Description = description;
            }
        }
    }

}
