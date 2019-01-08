using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services.Legacy
{
    class LegacyAzureDnsOptions
    {
        public string ClientId { get; set; }
        public string ResourceGroupName { get; set; }
        public string Secret { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
    }
}
