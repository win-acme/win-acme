using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    public class ScheduledRenewal
    {
        public DateTime Date { get; set; }
        public TargetBinding Binding { get; set; }

        public override string ToString() => $"{Date.ToShortDateString()} ({Binding})";
    }
}
