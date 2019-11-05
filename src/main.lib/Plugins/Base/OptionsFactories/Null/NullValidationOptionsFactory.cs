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
    internal class NullValidationFactory : IValidationPluginOptionsFactory, INull
    {
        Type IPluginOptionsFactory.InstanceType => typeof(object);
        Type IPluginOptionsFactory.OptionsType => typeof(object);
        string IValidationPluginOptionsFactory.ChallengeType => string.Empty;
        Task<ValidationPluginOptions> IValidationPluginOptionsFactory.Aquire(Target target, IInputService inputService, RunLevel runLevel) => null;
        Task<ValidationPluginOptions> IValidationPluginOptionsFactory.Default(Target target) => null;
        bool IPluginOptionsFactory.Match(string name) => false;
        string IPluginOptionsFactory.Name => "None";
        string IPluginOptionsFactory.Description => null;
        bool IValidationPluginOptionsFactory.CanValidate(Target target) => false;
        int IPluginOptionsFactory.Order => int.MaxValue;
        bool IPluginOptionsFactory.Disabled => true;
    }
}
