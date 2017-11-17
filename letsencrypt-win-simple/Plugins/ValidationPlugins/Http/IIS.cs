using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class IISFactory : BaseValidationPluginFactory<IIS>
    {
        public IISFactory() :
            base(nameof(IIS),
            "Create temporary application in IIS (recommended)",
            AcmeProtocol.CHALLENGE_TYPE_HTTP)
        { }

        public override bool CanValidate(Target target) => target.IIS == true && target.TargetSiteId > 0;
    }

    class IIS : FileSystem
    {

        public IIS(ScheduledRenewal target, IISClient iisClient, ILogService logService, IInputService inputService, ProxyService proxyService) :
            base(target, iisClient, logService, inputService, proxyService) { }

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

        public override void Default(Target target, IOptionsService optionsService)
        {
            var validationSiteIdRaw = optionsService.Options.ValidationSiteId;
            long validationSiteId;
            if (long.TryParse(validationSiteIdRaw, out validationSiteId))
            {
                _iisClient.GetSite(validationSiteId); // Throws exception when not found
                target.ValidationSiteId = validationSiteId;
            }
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            if (inputService.PromptYesNo("Use different site for validation?"))
            {
                target.ValidationSiteId = inputService.ChooseFromList("Validation site, must receive requests for all hosts on port 80",
                    _iisClient.RunningWebsites(),
                    x => new Choice<long>(x.Id) { Command = x.Id.ToString(), Description = x.Name }, true);
            }
        }

    }
}
