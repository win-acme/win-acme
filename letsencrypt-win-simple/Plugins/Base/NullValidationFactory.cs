using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.Base
{
    /// <summary>
    /// Null implementation
    /// </summary>
    class NullValidationFactory : IValidationPluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        string IValidationPluginFactory.ChallengeType => string.Empty;
        void IValidationPluginFactory.Aquire(Target target, IOptionsService optionsService, IInputService inputService) { }
        bool IValidationPluginFactory.CanValidate(Target target) => false;
        void IValidationPluginFactory.Default(Target target, IOptionsService optionsService) { }
        bool IHasName.Match(string name) => false;
    }
}
