using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullValidationFactory : IValidationPluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        bool IHasName.Match(string name) => false;
        Type IHasType.Instance => typeof(object);
        string IValidationPluginFactory.ChallengeType => string.Empty;
        ValidationPluginOptions IValidationPluginFactory.Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => null;
        ValidationPluginOptions IValidationPluginFactory.Default(Target target, IOptionsService optionsService) => null;
        Type IValidationPluginFactory.OptionsType => null;
    }
}
