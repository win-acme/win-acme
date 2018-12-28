using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullTargetFactory : ITargetPluginOptionsFactory, INull
    {
        Type IHasType.Instance => typeof(object);
        bool ITargetPluginOptionsFactory.Hidden => true;
        bool IHasName.Match(string name) => false;
        TargetPluginOptions ITargetPluginOptionsFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => null;
        TargetPluginOptions ITargetPluginOptionsFactory.Default(IOptionsService optionsService) => null;
        string IHasName.Name => "None";
        string IHasName.Description => null;
        Type ITargetPluginOptionsFactory.OptionsType => typeof(object);
    }
}
