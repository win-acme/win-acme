using System.Security.Cryptography.X509Certificates;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Schedules;
using Serilog;

namespace LetsEncrypt.ACME.Simple
{
    public static class DefaultRunner
    {
        public static void Auto(Target binding)
        {
            var auth = App.LetsEncryptService.Authorize(binding);
            if (auth.Status == "valid")
            {
                var pfxFilename = App.LetsEncryptService.GetCertificate(binding);

                if (App.Options.Test && !App.Options.Renew)
                {
                    if (!$"\nDo you want to install the .pfx into the Certificate Store/ Central SSL Store?".PromptYesNo())
                        return;
                }

                if (!App.Options.CentralSsl)
                {
                    X509Store store;
                    X509Certificate2 certificate;
                    Log.Information("Installing Non-Central SSL Certificate in the certificate store");
                    App.CertificateService.InstallCertificate(binding, pfxFilename, out store, out certificate);

                    if (App.Options.Test && !App.Options.Renew)
                        if (!$"\nDo you want to add/update the certificate to your server software?".PromptYesNo())
                            return;

                    Log.Information("Installing Non-Central SSL Certificate in server software");
                    binding.Plugin.Install(binding, pfxFilename, store, certificate);
                    if (!App.Options.KeepExisting)
                        App.CertificateService.UninstallCertificate(binding.Host, out store, certificate);
                }
                else if (!App.Options.Renew || !App.Options.KeepExisting)
                {
                    //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
                    Log.Information("Updating new Central SSL Certificate");
                    binding.Plugin.Install(binding);
                }

                if (App.Options.Test && !App.Options.Renew)
                {
                    if (!$"\nDo you want to automatically renew this certificate in {App.Options.RenewalPeriodDays} days? This will add a task scheduler task.".PromptYesNo())
                        return;
                }

                if (!App.Options.Renew)
                {
                    Log.Information("Adding renewal for {binding}", binding);
                    Scheduler.ScheduleRenewal(binding);
                }
            }
        }
    }
}
