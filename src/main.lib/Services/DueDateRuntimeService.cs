using ACMESharp.Protocol.Resources;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Services
{
    public class DueDateRuntimeService
    {
        private readonly IInputService _input;
        private readonly ISettingsService _settings;
        private readonly ILogService _log;

        public DueDateRuntimeService(
            ISettingsService settings,
            ILogService logService,
            IInputService input)
        {
            _log = logService;
            _settings = settings;
            _input = input;
        }

        /// <summary>
        /// Test if the order should run based on static or dynamic information
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool ShouldRun(OrderContext order)
        {
            var previous = order.CachedCertificate?.Certificate;
            if (previous != null)
            {
                _log.Verbose("{name}: previous thumbprint {thumbprint}", order.OrderFriendlyName, previous.Thumbprint);
                _log.Verbose("{name}: previous expires {thumbprint}", order.OrderFriendlyName, _input.FormatDate(previous.NotAfter));

                // Check if the certificate was actually installed
                // succesfully before we decided to use it as a 
                // reference point.
                var history = order.Renewal.History.
                    Where(x => x.OrderResults?.Any(o =>
                    o.Success == true &&
                    o.Thumbprint == previous.Thumbprint) ?? false);
                if (!history.Any())
                {
                    // Latest date determined by the certificate validity
                    // because we've established (through the history) that 
                    // this certificate was succesfully stored and installed
                    // at least once.
                    _log.Verbose("{name}: no historic success found", order.OrderFriendlyName);
                    previous = null;
                }
            }
          
            var range = ComputeDueDate(previous, order.RenewalInfo);
            if (range.Source?.Contains("ri") ?? false)
            {
                _log.Verbose("Using server side renewal schedule");
                if (!string.IsNullOrWhiteSpace(order.RenewalInfo?.ExplanationUrl)) 
                {
                    _log.Warning("Schedule modified due to incident: {url}", order.RenewalInfo?.ExplanationUrl);
                }
            } 
            else
            {
                _log.Verbose("Using client side renewal schedule");
            }

            return ShouldRunCommon(range, order.OrderFriendlyName);
        }

        /// <summary>
        /// Get renewal date range based on a certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public DueDate ComputeDueDate(X509Certificate2? certificate, AcmeRenewalInfo? renewalInfo) => 
            ComputeDueDate(certificate?.NotBefore ?? DateTime.Now, certificate?.NotAfter ?? DateTime.Now, renewalInfo);

        /// <summary>
        /// Get renewal date range based on a certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public DueDate ComputeDueDate(DueDate input) => ComputeDueDate(input.Start, input.End, null);


        /// <summary>
        /// Get renewal date range based on a static input
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public DueDate ComputeDueDate(DateTime validFrom, DateTime validUntil, AcmeRenewalInfo? renewalInfo = null)
        {
            // Basic rule for End: NotBefore + RenewalDays
            var end = validFrom.AddDays(_settings.ScheduledTask.RenewalDays);
            var endSource = "rd";

            // Guard rail #1: certificate should at least be valid for RenewalMinimumValidDays
            var endMinValid = validUntil.AddDays(-1 * _settings.ScheduledTask.RenewalMinimumValidDays ?? DueDateStaticService.DefaultMinValidDays);
            if (endMinValid < end)
            {
                end = endMinValid;
                endSource = "mv";
            }

            // Guard rail #2: server might feel the renewal should happen even earlier,
            // for example during a security incident.
            if (_settings.ScheduledTask.RenewalDisableServerSchedule != true && 
                renewalInfo != null && 
                renewalInfo.SuggestedWindow.End != null &&
                renewalInfo.SuggestedWindow.End.Value < end)
            {
                end = renewalInfo.SuggestedWindow.End.Value;
                endSource = "ri";
            }

            // Basic rule for End: Start - RenewalDaysRange
            var start = end.AddDays((_settings.ScheduledTask.RenewalDaysRange ?? 0) * -1);
            var startSource = "rd";

            // Guard rail #3: server might feel the renewal should happen even earlier,
            // for example during a security incident.
            if (_settings.ScheduledTask.RenewalDisableServerSchedule != true &&
                renewalInfo != null &&
                renewalInfo.SuggestedWindow.Start != null &&
                renewalInfo.SuggestedWindow.Start.Value < start)
            {
                start = renewalInfo.SuggestedWindow.Start.Value;
                startSource = "ri";
            }

            // Guard rail #4: start should not be after end.
            if (start > end)
            {
                start = end;
                startSource = endSource;
            }

            // Store dates and their sources
            return new DueDate() { 
                Start = start, 
                End = end, 
                Source = $"{startSource}-{endSource}" 
            };
        }

        /// <summary>
        /// Common trigger of renewal between start and end 
        /// </summary>
        /// <param name="earliestDueDate"></param>
        /// <param name="latestDueDate"></param>
        /// <param name="orderName"></param>
        /// <returns></returns>
        private bool ShouldRunCommon(DueDate dueDate, string orderName)
        {
            var now = DateTime.Now;
            var latestDueDate = dueDate.End;
            var earliestDueDate = dueDate.Start;
            _log.Verbose("{name}: latest due date {latestDueDate}", orderName, _input.FormatDate(latestDueDate));
            _log.Verbose("{name}: earliest due date {earliestDueDate}", orderName, _input.FormatDate(earliestDueDate));

            if (earliestDueDate > now)
            {
                // No due yet
                return false;
            }

            // Over n days (so typically n runs) the chance of renewing the order
            // grows proportionally. For example in a 5 day range, the chances of
            // renewing on each day are: 0.2 (1/5), 0.25 (1/4), 0.33 (1/3), 0.5 (1/2)
            // and 1 (1/1). That works out in such a way that a priory the chance
            // of running on each day is the same.

            // How many days are romaining within this range?
            var daysLeft = (latestDueDate - now).TotalDays;
            if (daysLeft <= 1)
            {
                _log.Verbose("{name}: less than a day left", orderName);
                return true;
            }
            if (Random.Shared.NextDouble() < (1 / daysLeft))
            {
                _log.Verbose("{name}: randomly selected", orderName);
                return true;
            }
            return false;
        }
    }
}
