using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class DueDateRuntimeService
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

        public bool ShouldRun(OrderContext order)
        {
            // Should always run, should not even ask the IDueDateService
            if (order.CachedCertificate == null)
            {
                throw new InvalidOperationException();
            }

            // If RenewalInfo is unavailable, disabled or invalid,
            // fall back to client side logic based on certificate
            // validity and fixed nr. of days setting.
            if (_settings.ScheduledTask.RenewalDisableServerSchedule == true ||
                order.RenewalInfo == null || 
                order.RenewalInfo.SuggestedWindow.Start == null ||
                order.RenewalInfo.SuggestedWindow.End == null)
            {
                _log.Verbose("Using client side renewal schedule");
                return ShouldRunClient(order, order.CachedCertificate);
            }

            // Default: use server side schedule
            _log.Verbose("Using server side renewal schedule");
            if (!string.IsNullOrWhiteSpace(order.RenewalInfo.ExplanationUrl))
            {
                _log.Warning("Renewal schedule modified: {url}", order.RenewalInfo.ExplanationUrl);
            }

            // Do no reason about what the values should be, simply
            // apply the ones provided by the server
            return ShouldRunCommon(
                order.RenewalInfo.SuggestedWindow.Start.Value,
                order.RenewalInfo.SuggestedWindow.End.Value,
                order.OrderName);
        }

        /// <summary>
        /// Should run according to client side available logic,
        /// i.e. renewal history, issue date, expire date
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private bool ShouldRunClient(OrderContext order, ICertificateInfo previous)
        {
            _log.Verbose("{name}: previous thumbprint {thumbprint}", order.OrderName, previous.Certificate.Thumbprint);
            _log.Verbose("{name}: previous expires {thumbprint}", order.OrderName, _input.FormatDate(previous.Certificate.NotAfter));

            // Check if the certificate was actually installed
            // succesfully before we decided to use it as a 
            // reference point.
            var latestDueDate = DateTime.Now;
            var history = order.Renewal.History.
                Where(x => x.OrderResults?.Any(o =>
                o.Success == true &&
                o.Thumbprint == previous.Certificate.Thumbprint) ?? false);
            if (history.Any())
            {
                // Latest date determined by the certificate validity
                // because we've established (through the history) that 
                // this certificate was succesfully stored and installed
                // at least once.
                latestDueDate = new DateTime(Math.Min(
                    previous.Certificate.NotBefore.AddDays(_settings.ScheduledTask.RenewalDays).Ticks,
                    previous.Certificate.NotAfter.AddDays(-1 * _settings.ScheduledTask.RenewalMinimumValidDays ?? DueDateStaticService.DefaultMinValidDays).Ticks));
            }
            else
            {
                _log.Verbose("{name}: no historic success found", order.OrderName);
            }

            return ShouldRunCommon(latestDueDate, latestDueDate, order.OrderName);
        }

        /// <summary>
        /// Common trigger of renewal between start and end 
        /// </summary>
        /// <param name="earliestDueDate"></param>
        /// <param name="latestDueDate"></param>
        /// <param name="orderName"></param>
        /// <returns></returns>
        private bool ShouldRunCommon(DateTime earliestDueDate, DateTime latestDueDate, string orderName)
        {
            // The RenewalDaysRange setting expands even the server suggested window
            earliestDueDate = new DateTime(Math.Min(earliestDueDate.Ticks, latestDueDate.AddDays((_settings.ScheduledTask.RenewalDaysRange ?? 0) * -1).Ticks));

            _log.Verbose("{name}: latest due date {latestDueDate}", orderName, _input.FormatDate(latestDueDate));
            _log.Verbose("{name}: earliest due date {earliestDueDate}", orderName, _input.FormatDate(earliestDueDate));

            if (earliestDueDate > DateTime.Now)
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
            var daysLeft = (latestDueDate - DateTime.Now).TotalDays;
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
