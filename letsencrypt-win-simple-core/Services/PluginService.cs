using System.Security.Cryptography.X509Certificates;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Interfaces;
using LetsEncrypt.ACME.Simple.Core.Schedules;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Core.Services
{
    public class PluginService : IPluginService
    {
        protected IOptions Options;
        protected ICertificateService CertificateService;
        protected ILetsEncryptService LetsEncryptService;
        protected IConsoleService ConsoleService;
        public PluginService(IOptions options, ICertificateService certificateService,
            ILetsEncryptService letsEncryptService, IConsoleService consoleService)
        {
            Options = options;
            CertificateService = certificateService;
            LetsEncryptService = letsEncryptService;
            ConsoleService = consoleService;
        }

        public void DefaultAction(Target target)
        {
            var auth = LetsEncryptService.Authorize(target);
            if (auth.Status != "valid")
                return;

            var pfxFilename = LetsEncryptService.GetCertificate(target);

            if (Options.Test && !Options.Renew && !ConsoleService
                    .PromptYesNo(
                        $"\nDo you want to install the .pfx into the Certificate Store/ Central SSL Store?"))
                return;

            if (!Options.CentralSsl)
            {
                X509Store store;
                X509Certificate2 certificate;
                Log.Information("Installing Non-Central SSL Certificate in the certificate store");
                CertificateService.InstallCertificate(target, pfxFilename, out store, out certificate);

                if (Options.Test && !Options.Renew)
                    if (!ConsoleService.PromptYesNo(
                        $"\nDo you want to add/update the certificate to your server software?"))
                        return;

                Log.Information("Installing Non-Central SSL Certificate in server software");
                var plugin = Options.Plugins[target.PluginName];
                plugin.Install(target, pfxFilename, store, certificate);
                if (!Options.KeepExisting)
                    CertificateService.UninstallCertificate(target.Host, out store, certificate);
            }
            else if (!Options.Renew || !Options.KeepExisting)
            {
                //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
                Log.Information("Updating new Central SSL Certificate");
                var plugin = Options.Plugins[target.PluginName];
                plugin.Install(target);
            }

            if (Options.Test && !Options.Renew)
            {
                if (!ConsoleService.PromptYesNo(
                    $"\nDo you want to automatically renew this certificate in {Options.RenewalPeriodDays} days? This will add a task scheduler task.")
                )
                    return;
            }

            if (!Options.Renew)
            {
                Log.Information("Adding renewal for {binding}", this);
                var scheduler = new Scheduler(Options, ConsoleService);
                scheduler.ScheduleRenewal(target);
            }
        }
    }
}
