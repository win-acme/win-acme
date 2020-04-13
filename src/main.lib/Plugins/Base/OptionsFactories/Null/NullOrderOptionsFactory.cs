using PKISharp.WACS.DomainObjects;
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
    internal class NullOrderOptionsFactory : IOrderPluginOptionsFactory, INull
    {
        Type IPluginOptionsFactory.InstanceType => typeof(object);
        Type IPluginOptionsFactory.OptionsType => typeof(object);
        string IPluginOptionsFactory.Name => "None";
        string? IPluginOptionsFactory.Description => null;
        int IPluginOptionsFactory.Order => int.MaxValue;
        (bool, string?) IPluginOptionsFactory.Disabled => (false, null);
        bool IPluginOptionsFactory.Match(string name) => false;
        Task<OrderPluginOptions?> IPluginOptionsFactory<OrderPluginOptions>.Aquire(IInputService inputService, RunLevel runLevel) => Task.FromResult<OrderPluginOptions?>(null);
        Task<OrderPluginOptions?> IPluginOptionsFactory<OrderPluginOptions>.Default() => Task.FromResult<OrderPluginOptions?>(null);
        bool IOrderPluginOptionsFactory.CanProcess(Target target) => false;
    }
}
