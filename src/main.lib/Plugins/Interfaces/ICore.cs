using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IPluginOptionsFactory
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Check if name matches
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Match(string name);

        /// <summary>
        /// Human-understandable description
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Which type is used as instance
        /// </summary>
        Type InstanceType { get; }

        /// <summary>
        /// Which type is used as options
        /// </summary>
        Type OptionsType { get; }

        /// <summary>
        /// How its sorted in the menu
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Indicates whether the plugin is currently disabled and why
        /// </summary>
        /// <returns></returns>
        (bool, string?) Disabled => (false, null);
    }

    public interface IPluginOptionsFactory<T>: IPluginOptionsFactory where T: PluginOptions
    {
        /// <summary>
        /// Check or get configuration information needed (interactive)
        /// </summary>
        /// <param name="target"></param>
        Task<T?> Aquire(IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed (unattended)
        /// </summary>
        /// <param name="target"></param>
        Task<T?> Default();
    }

    public interface INull { }

    /// <summary>
    /// Base class for the attribute is used to find it easily
    /// </summary>
    public interface IPluginMeta
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Hidden { get; }
        public Type Options { get; }
        public Type OptionsFactory { get; }
        public Type OptionsJson { get; }
    }

    public interface IPlugin
    {
        /// <summary>
        /// Indicates whether the plugin is currently disabled and why
        /// </summary>
        /// <returns></returns>
        (bool, string?) Disabled => (false, null);

        /// <summary>
        /// Mark a class as a plugin
        /// Only possible on types that implement IPlugin, as per 
        /// https://blog.marcgravell.com/2009/06/restricting-attribute-usage.html
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <typeparam name="TOptionsFactory"></typeparam>
        /// <typeparam name="TJson"></typeparam>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        protected sealed class PluginAttribute<TOptions, TOptionsFactory, TJson> : 
            Attribute, IPluginMeta
            where TOptions : PluginOptions, new()
            where TOptionsFactory : IPluginOptionsFactory
            where TJson : JsonSerializerContext
        {
            public Guid Id { get; }
            public bool Hidden { get; set; } = false;
            public string Name { get; set; }
            public string Description { get; set; }
            public Type Options { get; }
            public Type OptionsFactory { get; }
            public Type OptionsJson { get; }

            public PluginAttribute(string id, string name, string description)
            {
                Id = Guid.Parse(id);
                Options = typeof(TOptions);
                OptionsFactory = typeof(TOptionsFactory);
                OptionsJson = typeof(TJson);
                Name = name; 
                Description = description;
            }
        }
    }

}
