using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;
using System.Net.Mail;

namespace PKISharp.WACS.Services
{
    internal class NotificationService
    {
        private readonly ILogService _log;
        private readonly ICertificateService _certificateService;
        private readonly ISettingsService _settings;
        private readonly EmailClient _email;

        public NotificationService(
            ILogService log,
            ISettingsService setttings,
            EmailClient email,
            ICertificateService certificateService)
        {
            _log = log;
            _certificateService = certificateService;
            _email = email;
            _settings = setttings;
        }

        /// <summary>
        /// Handle success notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal void NotifySuccess(RunLevel runLevel, Renewal renewal)
        {
            // Do not send emails when running interactively
            _log.Information(LogType.All, "Renewal for {friendlyName} succeeded", renewal.LastFriendlyName);
            if (runLevel.HasFlag(RunLevel.Unattended) && _settings.Notification.EmailOnSuccess)
            {
                _email.Send(
                    "Certificate renewal completed",
                    $"<p>Certificate <b>{renewal.LastFriendlyName}</b> succesfully renewed.</p> {NotificationInformation(renewal)}",
                    MailPriority.Low);
            }
        }

        /// <summary>
        /// Handle failure notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal void NotifyFailure(RunLevel runLevel, Renewal renewal, string? errorMessage)
        {
            // Do not send emails when running interactively       
            _log.Error("Renewal for {friendlyName} failed, will retry on next run", renewal.LastFriendlyName);
            if (runLevel.HasFlag(RunLevel.Unattended))
            {
                _email.Send("Error processing certificate renewal",
                    $"<p>Renewal for <b>{renewal.LastFriendlyName}</b> failed with error <b>{errorMessage ?? "(null)"}</b>, will retry on next run.</p> {NotificationInformation(renewal)}",
                    MailPriority.High);
            }
        }

        private string NotificationInformation(Renewal renewal)
        {
            try
            {
                var extraMessage = "";
                extraMessage += $"<p>Hosts: {NotificationHosts(renewal)}</p>";
                extraMessage += "<p><table><tr><td>Plugins</td><td></td></tr>";
                if (renewal.TargetPluginOptions != null)
                {
                    extraMessage += $"<tr><td>Target: </td><td> {renewal.TargetPluginOptions.Name}</td></tr>";
                }
                if (renewal.ValidationPluginOptions != null)
                {
                    extraMessage += $"<tr><td>Validation: </td><td> {renewal.ValidationPluginOptions.Name}</td></tr>";
                }
                if (renewal.CsrPluginOptions != null)
                {
                    extraMessage += $"<tr><td>CSR: </td><td> {renewal.CsrPluginOptions.Name}</td></tr>";
                }
                if (renewal.StorePluginOptions != null)
                {
                    extraMessage += $"<tr><td>Store: </td><td> {string.Join(", ", renewal.StorePluginOptions.Select(x => x.Name))}</td></tr>";
                }
                if (renewal.InstallationPluginOptions != null)
                {
                    extraMessage += $"<tr><td>Installation: </td><td> {string.Join(", ", renewal.InstallationPluginOptions.Select(x => x.Name))}</td></tr>";
                }
                extraMessage += "</table></p>";
                return extraMessage;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Retrieval of metadata for email failed.");
                return "";
            }
        }

        private string NotificationHosts(Renewal renewal)
        {
            try
            {
                var cache = _certificateService.CachedInfo(renewal);
                if (cache == null)
                {
                    return "Unknown";
                }
                else
                {
                    return string.Join(", ", cache.HostNames);
                }
            }
            catch
            {
                return "Error";
            }
        }
    }
}
