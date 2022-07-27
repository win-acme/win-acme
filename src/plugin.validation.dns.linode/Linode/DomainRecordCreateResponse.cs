using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Linode
{
    internal class DomainRecordCreateResponse
    {
        public int id { get; set; }
        public object? errors { get; set; }
    }
}
