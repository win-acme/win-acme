using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class IISFactory : IValidationPluginFactory
    {
        public string Name => nameof(IIS);
        public string Description => "Create temporary application in IIS (recommended)";
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_HTTP;
        public bool CanValidate(Target target) => target.IIS == true && target.SiteId > 0;
        public Type Instance => typeof(IIS);
    }

    class IIS : FileSystem
    {
        private IISClient _iisClient = new IISClient();

        public IIS(Target target, ILogService logService, IInputService inputService, IOptionsService optionsService) : base(target, logService, inputService, optionsService) { }

        public override void BeforeAuthorize(Target target, HttpChallenge challenge)
        {
            _iisClient.PrepareSite(target);
            base.BeforeAuthorize(target, challenge);
        }

        public override void BeforeDelete(Target target, HttpChallenge challenge)
        {
            _iisClient.UnprepareSite(target);
            base.BeforeDelete(target, challenge);
        }

        public override void Default(Target target) { }

        public override void Aquire(Target target) { }
    }
}
