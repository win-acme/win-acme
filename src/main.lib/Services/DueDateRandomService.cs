using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class DueDateRandomService : DueDateStaticService
    {
        private readonly IInputService _input;

        public DueDateRandomService(
            ISettingsService settings,
            ICertificateService certificateService,
            ILogService logService,
            IInputService input) :
            base(settings, certificateService, logService) => _input = input;

        public override bool ShouldRun(Renewal renewal) => true;

        public override bool ShouldRun(OrderContext order)
        {
            // Run any in nay case if a difference in source is detected.
            var previous = _certificateService.CachedInfo(order.Order);
            if (previous == null)
            {
                _logService.Verbose("{name}: no cached information found", order.OrderName);
                if (!order.Renewal.New)
                {
                    _logService.Information(LogType.All, "Renewal {renewal} running prematurely due to source change in order {order}", order.Renewal.LastFriendlyName, order.OrderName);
                }
                return true;
            } 
            else
            {
                _logService.Verbose("{name}: previous thumbprint {thumbprint}", order.OrderName, previous.Certificate.Thumbprint);
                _logService.Verbose("{name}: previous expires {thumbprint}", order.OrderName, _input.FormatDate(previous.Certificate.NotAfter));
            }

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
                    previous.Certificate.NotAfter.AddDays(-1 * _settings.ScheduledTask.RenewalMinimumValidDays ?? DefaultMinValidDays).Ticks));
            } 
            else
            {
                _logService.Verbose("{name}: no historic success found", order.OrderName);
            }

            // Randomize over the course of 10 days
            var earliestDueDate = latestDueDate.AddDays((_settings.ScheduledTask.RenewalDaysRange ?? 0) * -1);
            _logService.Verbose("{name}: latest due date {latestDueDate}", order.OrderName, _input.FormatDate(latestDueDate));
            _logService.Verbose("{name}: earliest due date {earliestDueDate}", order.OrderName, _input.FormatDate(earliestDueDate));
            if (earliestDueDate > DateTime.Now)
            {
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
                _logService.Verbose("{name}: less than a day left", order.OrderName);
                return true;
            }
            if (Random.Shared.NextDouble() < (1 / daysLeft))
            {
                _logService.Verbose("{name}: randomly selected", order.OrderName);
                return true;
            }
            return false;
        }
    }
}
