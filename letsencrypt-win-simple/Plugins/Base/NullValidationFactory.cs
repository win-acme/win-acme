using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullValidationFactory : IValidationPluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        string IValidationPluginFactory.ChallengeType => string.Empty;
        void IValidationPluginFactory.Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel) { }
        bool IValidationPluginFactory.CanValidate(Target target) => false;
        void IValidationPluginFactory.Default(Target target, IOptionsService optionsService) { }
        bool IHasName.Match(string name) => false;
        bool IValidationPluginFactory.Hidden => true;
    }
}
