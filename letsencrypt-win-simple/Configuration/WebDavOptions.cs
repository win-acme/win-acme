using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Configuration
{
    public class WebDavOptions : NetworkCredentialOptions
    {
        public WebDavOptions(): base() { }
        public WebDavOptions(IOptionsService options) : base(options) { }
        public WebDavOptions(IOptionsService options, IInputService input) : base(options, input) { }
    }
}
