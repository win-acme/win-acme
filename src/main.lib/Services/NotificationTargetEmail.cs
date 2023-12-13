using MimeKit;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services.Interfaces;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PKISharp.WACS.Services
{
    internal class NotificationTargetEmail : INotificationTarget
    {
        private readonly ILogService _log;
        private readonly ICacheService _cacheService;
        private readonly IPluginService _plugin;
        private readonly EmailClient _email;
        private readonly DueDateStaticService _dueDate;

        public NotificationTargetEmail(
            ILogService log,
            IPluginService pluginService,
            EmailClient email,
            DueDateStaticService dueDate,
            ICacheService certificateService)
        {
            _log = log;
            _cacheService = certificateService;
            _plugin = pluginService;
            _email = email;
            _plugin = pluginService;
            _dueDate = dueDate;
        }

        /// <summary>
        /// Handle created notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        public async Task SendCreated(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            await _email.Send(
                    $"Certificate {renewal.LastFriendlyName} created",
                    @$"<p>Certificate <b>{HttpUtility.HtmlEncode(renewal.LastFriendlyName)}</b> successfully created.</p> 
                    {NotificationInformation(renewal)}
                    {RenderLog(log)}",
                    MessagePriority.Normal);
        }

        /// <summary>
        /// Handle success notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        public async Task SendSuccess(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            var withErrors = log.Any(l => l.Level == LogEventLevel.Error);
            await _email.Send(
                $"Certificate renewal {renewal.LastFriendlyName} completed" + (withErrors ? " with errors" : ""),
                @$"<p>Certificate <b>{HttpUtility.HtmlEncode(renewal.LastFriendlyName)}</b> {(withErrors ? "renewed with errors" : "succesfully renewed")}.</p> 
                {NotificationInformation(renewal)}
                {RenderLog(log)}",
                withErrors ? MessagePriority.Urgent : MessagePriority.NonUrgent);
        }

        /// <summary>
        /// Handle failure notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        public async Task SendFailure(
            Renewal renewal,
            IEnumerable<MemoryEntry> log,
            IEnumerable<string> errors)
        {
            await _email.Send(
                $"Error processing certificate renewal {renewal.LastFriendlyName}",
                @$"<p>Renewal for <b>{HttpUtility.HtmlEncode(renewal.LastFriendlyName)}</b> failed, will retry on next run.<br><br>Error(s):
                <ul><li>{string.Join("</li><li>", errors.Select(x => HttpUtility.HtmlEncode(x)))}</li></ul></p>
                {NotificationInformation(renewal)}
                {RenderLog(log)}",
                MessagePriority.Urgent);
        }

        public async Task SendTest()
        {
            if (!_email.Enabled)
            {
                _log.Error("Email notifications not enabled. Configure an SMTP server, sender and receiver in settings.json to enable this.");
            }
            else
            {
                _log.Information("Sending test message...");
                var success = await _email.Send("Test notification",
                    "<p>If you are reading this, it means you will receive notifications in the future.</p>",
                    MessagePriority.Normal);
                if (success)
                {
                    _log.Information("Test message sent!");
                }
            }
        }

        private static string RenderLog(IEnumerable<MemoryEntry> log) => @$"<p>Log output:<ul><li>{string.Join("</li><li>", log.Select(x => RenderLogEntry(x)))}</ul></p>";

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
                        <tr><td>Target: </td><td> {_plugin.GetPlugin(renewal.TargetPluginOptions).Name}</td></tr>";
                extraMessage += @$"<tr><td>Validation: </td><td> {_plugin.GetPlugin(renewal.ValidationPluginOptions).Name}</td></tr>";
                if (renewal.OrderPluginOptions != null)
                {
                    extraMessage += @$"<tr><td>Order: </td><td> {_plugin.GetPlugin(renewal.OrderPluginOptions).Name}</td></tr>";
                }
                if (renewal.CsrPluginOptions != null)
                {
                    extraMessage += @$"<tr><td>Csr: </td><td> {_plugin.GetPlugin(renewal.CsrPluginOptions).Name}</td></tr>";
                }
                extraMessage += @$"<tr><td>Store: </td><td> {string.Join(", ", renewal.StorePluginOptions.Select(x => _plugin.GetPlugin(x).Name))}</td></tr>";
                extraMessage += $"<tr><td>Installation: </td><td> {string.Join(", ", renewal.InstallationPluginOptions.Select(x => _plugin.GetPlugin(x).Name))}</td></tr>";
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
            var hosts = new List<string>();
            try
            {
                var orders = _dueDate.CurrentOrders(renewal);
                foreach (var order in orders)
                {
                    var cert = _cacheService.PreviousInfo(renewal, order.Key);
                    if (cert != null)
                    {
                        hosts.AddRange(cert.SanNames.Select(x => x.Value));
                    }
                }
            }
            catch
            {
                return "Error";
            }
            if (!hosts.Any())
            {
                return "Unknown";
            }
            return string.Join(", ", hosts.Distinct());
        }


    }
}
