using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services
{
    public class DueDateStaticService
    {
        internal const int DefaultMinValidDays = 7;
        private readonly ILogService _log;
        private readonly DueDateRuntimeService _runtime;

        public DueDateStaticService(
            DueDateRuntimeService runtime,
            ILogService logService)
        {
            _runtime = runtime;
            _log = logService;
        }

        public DueDate? DueDate(Renewal renewal)
        {
            var currentOrders = CurrentOrders(renewal);
            if (currentOrders.Any(x => x.Revoked))
            {
                return null;
            }
            return currentOrders.
                OrderBy(x => x.DueDate.Start).
                FirstOrDefault()?.
                DueDate;
        }

        public virtual bool IsDue(Renewal renewal)
        {
            var dueDate = DueDate(renewal);
            return dueDate == null || dueDate.Start < DateTime.Now;
        }

        private List<StaticOrderInfo> Mapping(Renewal renewal)
        {
            // Get most recent expire date for each order
            var expireMapping = new Dictionary<string, StaticOrderInfo>();
            foreach (var history in renewal.History.OrderBy(h => h.Date))
            {
                try
                {
                    var orderResults = history.OrderResults;
                    foreach (var orderResult in orderResults)
                    {
                        var info = default(StaticOrderInfo);
                        var key = orderResult.Name.ToLower();
                        var dueDate = orderResult.DueDate ??
                                _runtime.ComputeDueDate(new DueDate()
                                {
                                    Start = history.Date,
                                    End = history.ExpireDate ?? history.Date.AddYears(1)
                                });

                        if (orderResult.Success != false)
                        {
                            if (!expireMapping.ContainsKey(key))
                            {
                                info = new StaticOrderInfo(key, dueDate);
                                expireMapping.Add(key, info);
                            }
                            else
                            {
                                info = expireMapping[key];
                                info.DueDate = dueDate;
                            }
                            if (orderResult.Success == true)
                            {
                                info.LastRenewal = history.Date;
                                info.RenewCount += 1;
                                info.LastThumbprint = orderResult.Thumbprint;
                            }
                        }
                        if (info != null)
                        {
                            info.Missing = orderResult.Missing == true;
                            info.Revoked = orderResult.Revoked == true;
                        }

                    }
                } 
                catch (Exception ex)
                {
                    _log.Error(ex, "Error reading history for {renewal}: {ex}", renewal.Id, ex.Message);
                }
            }
            return expireMapping.Values.ToList();
        }

        public List<StaticOrderInfo> CurrentOrders(Renewal renewal) =>
            Mapping(renewal).
            Where(x => !x.Missing).
            ToList();
    }

    /// <summary>
    /// Information about a sub-order derived 
    /// and summarized from history entries
    /// </summary>
    public class StaticOrderInfo
    {
        public StaticOrderInfo(string key, DueDate dueDate)
        {
            Key = key;
            DueDate = dueDate;
        }

        public string Key { get; set; }
        public bool Missing { get; set; }
        public bool Revoked { get; set; }
        public DateTime? LastRenewal { get; set; }
        public string? LastThumbprint { get; set; }
        public int RenewCount { get; set; }
        public DueDate DueDate { get; set; }
    }
}
