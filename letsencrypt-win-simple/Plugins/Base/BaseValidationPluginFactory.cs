using ACMESharp;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base
{
    public abstract class BaseValidationPluginFactory<T> : BasePluginFactory<T>, IValidationPluginFactory where T : IValidationPlugin
    {
        private string _challengeType;

        public BaseValidationPluginFactory(ILogService log, string name, string description = null, string challengeType = AcmeProtocol.CHALLENGE_TYPE_HTTP) : base(log, name, description)
        {
            _challengeType = challengeType;
        }

        string IValidationPluginFactory.ChallengeType => _challengeType;
        public virtual bool CanValidate(Target target) { return true; }
        public virtual void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel) { }
        public virtual void Default(Target target, IOptionsService optionsService) { }
        public virtual bool Hidden => false;
    }
}
