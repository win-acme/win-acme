using System;
using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public abstract class BaseValidationPluginFactory<TPlugin, TOptions> :
        BasePluginFactory<TPlugin>, 
        IValidationPluginFactory 
        where TPlugin : IValidationPlugin
        where TOptions : ValidationPluginOptions, new()
    {
        private readonly string _challengeType;

        public BaseValidationPluginFactory(ILogService log, string challengeType = Http01ChallengeValidationDetails.Http01ChallengeType) : base(log, "", "")
        {
            _challengeType = challengeType;
        }

        // TODO: Remove
        string IHasName.Name => (new TOptions()).Name;
        string IHasName.Description => (new TOptions()).Description;
        public override bool Match(string name)
        {
            return string.Equals(name, (new TOptions()).Name, StringComparison.InvariantCultureIgnoreCase);
        }

        string IValidationPluginFactory.ChallengeType => _challengeType;
        public abstract TOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(Target target, IOptionsService optionsService);
        ValidationPluginOptions IValidationPluginFactory.Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(target, optionsService, inputService, runLevel);
        }
        ValidationPluginOptions IValidationPluginFactory.Default(Target target, IOptionsService optionsService)
        {
            return Default(target, optionsService);
        }
        public virtual bool Hidden => false;
        Type IValidationPluginFactory.OptionsType { get => typeof(TOptions); }
    }
}
