using MimeKit;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

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
        /// Handle created notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifyCreated(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            // Do not send emails when running interactively
            _log.Information(
                LogType.All, 
                "Certificate {friendlyName} created", 
                renewal.LastFriendlyName);
            if (_settings.Notification.EmailOnSuccess)
            {
                await _email.Send(
                    $"Certificate {renewal.LastFriendlyName} created",
                    @$"<p>Certificate <b>{HttpUtility.HtmlEncode(renewal.LastFriendlyName)}</b> successfully created.</p> 
                    {NotificationInformation(renewal)}
                    {RenderLog(log)}",
                    MessagePriority.Normal);
            }
        }

        /// <summary>
        /// Handle success notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifySuccess(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            // Do not send emails when running interactively
            _log.Information(
                LogType.All, 
                "Renewal for {friendlyName} succeeded",
                renewal.LastFriendlyName);
            if (_settings.Notification.EmailOnSuccess)
            {
                await _email.Send(
                    $"Certificate renewal {renewal.LastFriendlyName} completed",
                    @$"<p>Certificate <b>{HttpUtility.HtmlEncode(renewal.LastFriendlyName)}</b> successfully renewed.</p> 
                    {NotificationInformation(renewal)}
                    {RenderLog(log)}",
                    MessagePriority.NonUrgent);
            }
        }

        /// <summary>
        /// Handle failure notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal async Task NotifyFailure(
            RunLevel runLevel, 
            Renewal renewal, 
            RenewResult result,
            IEnumerable<MemoryEntry> log)
        {
            // Do not send emails when running interactively       
            _log.Error("Renewal for {friendlyName} failed, will retry on next run", renewal.LastFriendlyName);
            var errors = result.ErrorMessages?.ToList() ?? new List<string>();
            errors.AddRange(result.OrderResults?.SelectMany(o => o.ErrorMessages ?? Enumerable.Empty<string>()) ?? Enumerable.Empty<string>());
            if (errors.Count == 0)
            {
                errors.Add("No specific error reason provided.");
            }
            if (runLevel.HasFlag(RunLevel.Unattended))
            {
                await _email.Send(
                    $"Error processing certificate renewal {renewal.LastFriendlyName}",
                    @$"<p>Renewal for <b>{HttpUtility.HtmlEncode(renewal.LastFriendlyName)}</b> failed, will retry on next run.<br><br>Error(s):
                    <ul><li>{string.Join("</li><li>", errors.Select(x => HttpUtility.HtmlEncode(x)))}</li></ul></p>
                    {NotificationInformation(renewal)}
                    {RenderLog(log)}",
                    MessagePriority.Urgent);
            }
        }

        private string RenderLog(IEnumerable<MemoryEntry> log) => @$"<p>Log output:<ul><li>{string.Join("</li><li>", log.Select(x => RenderLogEntry(x)))}</ul></p>";

        private static string RenderLogEntry(MemoryEntry log)
        {
            var color = $"00000";
            switch (log.Level)
            {
                case LogEventLevel.Error:
                case LogEventLevel.Fatal:
                    color = "#8B0000";
                    break;

                case LogEventLevel.Warning:
                    color = "#CCCC00";
                    break;

                case LogEventLevel.Information:
                    color = "#000000";
                    break;

                case LogEventLevel.Debug:
                case LogEventLevel.Verbose:
                    color = "#a9a9a9";
                    break;
            }
            return $"<span style=\"color:{color}\">{log.Level} - {HttpUtility.HtmlEncode(log.Message)}</span>";
        }

        private string NotificationInformation(Renewal renewal)
        {
            try
            {
                var extraMessage = @$"<p>
                    <table>
                        <tr><td><b>Hosts</b><td><td></td></tr>
                        <tr><td colspan=""2"">{NotificationHosts(renewal)}</td></tr>
                        <tr><td colspan=""2"">&nbsp;</td></tr>
                        <tr><td><b>Plugins</b></td><td></td></tr>
                        <tr><td>Target: </td><td> {renewal.TargetPluginOptions.Name}</td></tr>";
                extraMessage += @$"<tr><td>Validation: </td><td> {renewal.ValidationPluginOptions.Name}</td></tr>";
                if (renewal.OrderPluginOptions != null)
                {
                    extraMessage += @$"<tr><td>Order: </td><td> {renewal.OrderPluginOptions.Name}</td></tr>";
                }
                if (renewal.CsrPluginOptions != null)
                {
                    extraMessage += @$"<tr><td>Csr: </td><td> {renewal.CsrPluginOptions.Name}</td></tr>";
                }
                extraMessage += @$"<tr><td>Store: </td><td> {string.Join(", ", renewal.StorePluginOptions.Select(x => x.Name))}</td></tr>";
                extraMessage += $"<tr><td>Installation: </td><td> {string.Join(", ", renewal.InstallationPluginOptions.Select(x => x.Name))}</td></tr>";
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
                var infos = _certificateService.CachedInfos(renewal);
                if (infos == null || !infos.Any())
                {
                    return "Unknown";
                }
                else
                {
                    return string.Join(", ", infos.SelectMany(i => i.SanNames).Distinct());
                }
            }
            catch
            {
                return "Error";
            }
        }
    }
}
