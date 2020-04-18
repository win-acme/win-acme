using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullTargetFactory : ITargetPluginOptionsFactory, INull
    {
        Type IPluginOptionsFactory.InstanceType => typeof(object);
        Type IPluginOptionsFactory.OptionsType => typeof(object);
        (bool, string?) IPluginOptionsFactory.Disabled => (false, null);
        bool IPluginOptionsFactory.Match(string name) => false;
        Task<TargetPluginOptions?> IPluginOptionsFactory<TargetPluginOptions>.Aquire(IInputService inputService, RunLevel runLevel) => Task.FromResult<TargetPluginOptions?>(default);
        Task<TargetPluginOptions?> IPluginOptionsFactory<TargetPluginOptions>.Default() => Task.FromResult<TargetPluginOptions?>(default);
        string IPluginOptionsFactory.Name => "None";
        string? IPluginOptionsFactory.Description => null;
        int IPluginOptionsFactory.Order => int.MaxValue;
    }
}
