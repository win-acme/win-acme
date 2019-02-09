using ACMESharp.ACME;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// FileSystem validation with IIS preparation
    /// </summary>
    internal class IISFactory : BaseHttpValidationFactory<IIS>
    {
        private IISClient _iisClient;

        public IISFactory(ILogService log, IISClient iisClient) : 
            base(log, nameof(IIS), "Create temporary application in IIS")
        {
            _iisClient = iisClient;
        }

        public override bool CanValidate(Target target) => target.IIS == true;

        public override void Default(Target target, IOptionsService optionsService)
        {
            var validationSiteIdRaw = optionsService.Options.ValidationSiteId;
            long validationSiteId;
            if (long.TryParse(validationSiteIdRaw, out validationSiteId))
            {
                _iisClient.GetWebSite(validationSiteId); // Throws exception when not found
                target.ValidationSiteId = validationSiteId;
            }
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            if (inputService.PromptYesNo("Use different site for validation?"))
            {
                target.ValidationSiteId = inputService.ChooseFromList("Validation site, must receive requests for all hosts on port 80",
                    _iisClient.WebSites,
                    x => new Choice<long>(x.Id) { Command = x.Id.ToString(), Description = x.Name }, true);
            }
        }
    }

    internal class IIS : FileSystem
    {
        public IIS(ScheduledRenewal renewal, Target target, IISClient iisClient, ILogService log, IInputService input, ProxyService proxy, string identifier) :
            base(renewal, target, iisClient, log, input, proxy, identifier)
        {
            _iisClient.PrepareSite(target);
        }

        public override void CleanUp()
        {
            _iisClient.UnprepareSite(_target);
            base.CleanUp();
        }
    }
}
