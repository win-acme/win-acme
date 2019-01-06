using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Extensions
{
    public static class OptionsExtensions
    {
        /// <summary>
        /// Reset the options for a(nother) run through the main menu
        /// </summary>
        /// <param name="options"></param>
        public static void Clear(this Options options)
        {
            options.Target = null;
            options.Renew = false;
            options.FriendlyName = null;
            options.ForceRenewal = false;
            options.ExcludeBindings = null;
            options.CommonName = null;
        }

        /// <summary>
        /// Validat whether or not the provided combination of options is acceptable
        /// </summary>
        /// <param name="result"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static bool Validate(this Options result, ILogService log)
        {
            if (result.Renew)
            {
                if (
                    !string.IsNullOrEmpty(result.AzureClientId) ||
                    !string.IsNullOrEmpty(result.AzureResourceGroupName) ||
                    !string.IsNullOrEmpty(result.AzureSecret) ||
                    !string.IsNullOrEmpty(result.AzureSubscriptionId) ||
                    !string.IsNullOrEmpty(result.AzureTenantId) ||
                    !string.IsNullOrEmpty(result.CentralSslStore) ||
                    !string.IsNullOrEmpty(result.CertificateStore) ||
                    !string.IsNullOrEmpty(result.CommonName) ||
                    !string.IsNullOrEmpty(result.DnsCreateScript) ||
                    !string.IsNullOrEmpty(result.DnsDeleteScript) ||
                    !string.IsNullOrEmpty(result.ExcludeBindings) ||
                    !string.IsNullOrEmpty(result.FriendlyName) ||
                    !string.IsNullOrEmpty(result.FtpSiteId) ||
                    !string.IsNullOrEmpty(result.Host) ||
                    result.Installation.Count() > 0 ||
                    !string.IsNullOrEmpty(result.InstallationSiteId) ||
                    result.KeepExisting ||
                    result.ManualTargetIsIIS ||
                    !string.IsNullOrEmpty(result.Password) ||
                    !string.IsNullOrEmpty(result.PfxPassword) ||
                    !string.IsNullOrEmpty(result.Script) ||
                    !string.IsNullOrEmpty(result.ScriptParameters) ||
                    !string.IsNullOrEmpty(result.SiteId) ||
                    result.SSLIPAddress != IISClient.DefaultBindingIp ||
                    result.SSLPort != IISClient.DefaultBindingPort ||
                    !string.IsNullOrEmpty(result.Store) ||
                    !string.IsNullOrEmpty(result.Target) ||
                    !string.IsNullOrEmpty(result.UserName) ||
                    !string.IsNullOrEmpty(result.Validation) ||
                    result.ValidationPort != null ||
                    !string.IsNullOrEmpty(result.ValidationSiteId) ||
                    result.Warmup ||
                    !string.IsNullOrEmpty(result.WebRoot)
                )
                {
                    log.Error("It's not possible to change properties during renewal. Edit the .json files or overwrite the renewal if you wish to change any settings.");
                    return false;
                }
            }
            return true;
        }
    }
}
