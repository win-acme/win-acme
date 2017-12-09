using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// FileSystem validation with IIS preparation
    /// </summary>
    class IISFactory : BaseHttpValidationFactory<IIS>
    {
        private IISClient _iisClient;

        public IISFactory(ILogService log, IISClient iisClient) : 
            base(log, nameof(IIS), "Create temporary application in IIS")
        {
            _iisClient = iisClient;
        }

        public override bool CanValidate(Target target) => target.IIS == true && target.TargetSiteId > 0;

        public override void Default(Target target, IOptionsService optionsService)
        {
            var validationSiteIdRaw = optionsService.Options.ValidationSiteId;
            if (long.TryParse(validationSiteIdRaw, out long validationSiteId))
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

    class IIS : FileSystem
    {
        public IIS(ScheduledRenewal target, IISClient iisClient, ILogService log, IInputService input, ProxyService proxy, string identifier) :
            base(target, iisClient, log, input, proxy, identifier)
        {
            _iisClient.PrepareSite(target.Binding);
        }

        public override void Dispose()
        {
            _iisClient.UnprepareSite(_renewal.Binding);
            base.Dispose();
        }
    }
}
