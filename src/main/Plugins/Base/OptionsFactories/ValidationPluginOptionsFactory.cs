using System;
using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public abstract class ValidationPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>, 
        IValidationPluginOptionsFactory 
        where TPlugin : IValidationPlugin
        where TOptions : ValidationPluginOptions, new()
    {
        private readonly string _challengeType;
        Type IValidationPluginOptionsFactory.OptionsType { get => typeof(TOptions); }
        string IValidationPluginOptionsFactory.ChallengeType => _challengeType;
        public virtual bool Hidden => false;

        public ValidationPluginOptionsFactory(ILogService log, string challengeType = Http01ChallengeValidationDetails.Http01ChallengeType) : base(log)
        {
            _challengeType = challengeType;
        }

        public abstract TOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(Target target, IOptionsService optionsService);
        ValidationPluginOptions IValidationPluginOptionsFactory.Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(target, optionsService, inputService, runLevel);
        }
        ValidationPluginOptions IValidationPluginOptionsFactory.Default(Target target, IOptionsService optionsService)
        {
            return Default(target, optionsService);
        }

    }
}
