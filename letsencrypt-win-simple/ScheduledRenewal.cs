using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    public class ScheduledRenewal
    {
        public string Host { get; set; }
        public string PhysicalPath { get; set; }
        public string SiteName { get; set; }

        public override string ToString() => $"{Host} ({PhysicalPath})";
    }
}
