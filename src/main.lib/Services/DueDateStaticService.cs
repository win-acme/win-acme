using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services
{
    public class DueDateStaticService
    {
        internal const int DefaultMinValidDays = 7;
        protected readonly ISettingsService _settings;
        internal readonly DueDateRuntimeService _runtime;

        public DueDateStaticService(
            DueDateRuntimeService runtime,
            ISettingsService settings)
        {
            _settings = settings;
            _runtime = runtime;
        }

        public DueDate? DueDate(Renewal renewal)
        {
            var last = Mapping(renewal).
                Where(x => x.Value != null).
                OrderBy(x => x.Value?.End).
                FirstOrDefault().
                Value;

            if (last == null)
            {
                return null;
            }
            return _runtime.ComputeDueDate(last);
        }

        public virtual bool IsDue(Renewal renewal)
        {
            var dueDate = DueDate(renewal);
            return dueDate == null || dueDate.Start < DateTime.Now;
        }

        private Dictionary<string, DueDate?> Mapping(Renewal renewal)
        {
            // Get most recent expire date for each order
            var expireMapping = new Dictionary<string, DueDate?>();
            foreach (var history in renewal.History.OrderBy(h => h.Date))
            {
                var orderResults = history.OrderResults ??
                new List<OrderResult> {
                    new OrderResult("main") {
                        Success = history.Success,
                        ExpireDate = history.ExpireDate,
                        Thumbprint = history.Thumbprints.FirstOrDefault()
                    }
                };

                foreach (var orderResult in orderResults)
                {
                    var key = orderResult.Name.ToLower();
                    if (!expireMapping.ContainsKey(key))
                    {
                        expireMapping.Add(key, null);
                    }
                    if (orderResult.Success == true)
                    {
                        expireMapping[key] =
                            orderResult.DueDate ??
                            new DueDate()
                            {
                                Start = history.Date,
                                End = history.ExpireDate ?? history.Date.AddYears(1)
                            };
                    }
                    if (orderResult.Missing == true)
                    {
                        expireMapping[key] = null;
                    }
                }
            }
            return expireMapping;
        }

        public List<string> CurrentOrders(Renewal renewal) =>
            Mapping(renewal).
            Where(x => x.Value != null).
            Select(x => x.Key).
            ToList();
    }
}
