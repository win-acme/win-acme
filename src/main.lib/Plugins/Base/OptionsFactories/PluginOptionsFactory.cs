using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public abstract class PluginOptionsFactory<TPlugin, TOptions> :
        IPluginOptionsFactory
        where TOptions : PluginOptions, new()
    {
        public virtual int Order => 0;
        Type IPluginOptionsFactory.OptionsType => typeof(TOptions);
        Type IPluginOptionsFactory.InstanceType => typeof(TPlugin);
        public virtual (bool, string?) Disabled { get; protected set; } = (false, null);
    }
}
