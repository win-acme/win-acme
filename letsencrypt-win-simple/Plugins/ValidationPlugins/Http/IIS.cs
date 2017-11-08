using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class IISFactory : BaseValidationPluginFactory<IIS>
    {
        public IISFactory() :
            base(nameof(IIS),
            "Create temporary application in IIS (recommended)",
            AcmeProtocol.CHALLENGE_TYPE_HTTP)
        { }

        public override bool CanValidate(Target target) => target.IIS == true && target.SiteId > 0;
    }

    class IIS : FileSystem
    {
        private IISClient _iisClient;

        public IIS(ScheduledRenewal target, IISClient iisClient, ILogService logService, 
            IInputService inputService, IOptionsService optionsService, ProxyService proxyService) :
            base(target, logService, inputService, optionsService, proxyService)
        {
            _iisClient = iisClient;
        }

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
