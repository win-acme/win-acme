using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IPluginOptionsFactory
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Check if name matches
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Match(string name);

        /// <summary>
        /// Human-understandable description
        /// </summary>
        string Description { get; }

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
        /// Indicates whether the plugin is currently disabled 
        /// because of insufficient access rights
        /// </summary>
        /// <returns></returns>
        bool Disabled { get; }
    }

    public interface INull { }

    public interface IIgnore { }

    public interface IPlugin
    {
        /// <summary>
        /// Indicates whether the plugin is currently disabled 
        /// because of insufficient access rights
        /// </summary>
        /// <returns></returns>
        bool Disabled { get; }
    }

}
