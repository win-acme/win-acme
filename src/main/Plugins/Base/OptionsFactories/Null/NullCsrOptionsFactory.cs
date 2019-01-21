using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullCsrFactory : ICsrPluginOptionsFactory, INull
    {
        Type IHasType.Instance => typeof(object);
        string IHasName.Name => "None";
        string IHasName.Description => null;
        bool IHasName.Match(string name) => false;
        CsrPluginOptions ICsrPluginOptionsFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => null;
        CsrPluginOptions ICsrPluginOptionsFactory.Default(IOptionsService optionsService) => null;
        Type ICsrPluginOptionsFactory.OptionsType => null;
    }
}
