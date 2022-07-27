using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Linode
{
    internal class DomainListResponse
    {
        public int page { get; set; }
        public int pages { get; set; }
        public int results { get; set; }

        public List<Domain>? data { get; set; }
    }

    internal class Domain
    {
        public int id { get; set; }
        public string? domain { get; set; }
    }
}