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
        Type IHasType.InstanceType => typeof(object);
        Type IHasType.OptionsType => typeof(object);
        bool ITargetPluginOptionsFactory.Hidden => true;
        bool IHasName.Match(string name) => false;
        Task<TargetPluginOptions> ITargetPluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => null;
        Task<TargetPluginOptions> ITargetPluginOptionsFactory.Default() => null;
        string IHasName.Name => "None";
        string IHasName.Description => null;
        public int Order => int.MaxValue;
    }
}
