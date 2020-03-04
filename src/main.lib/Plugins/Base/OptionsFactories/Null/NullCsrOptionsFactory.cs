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
    internal class NullCsrFactory : ICsrPluginOptionsFactory, INull
    {
        Type IPluginOptionsFactory.InstanceType => typeof(object);
        Type IPluginOptionsFactory.OptionsType => typeof(object);
        string IPluginOptionsFactory.Name => "None";
        string? IPluginOptionsFactory.Description => null;
        int IPluginOptionsFactory.Order => int.MaxValue;
        (bool, string?) IPluginOptionsFactory.Disabled => (false, null);
        bool IPluginOptionsFactory.Match(string name) => false;
        Task<CsrPluginOptions?> ICsrPluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => Task.FromResult<CsrPluginOptions?>(null);
        Task<CsrPluginOptions?> ICsrPluginOptionsFactory.Default() => Task.FromResult<CsrPluginOptions?>(null);
    }
}
