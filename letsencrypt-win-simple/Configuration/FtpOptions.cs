using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Configuration
{
    public class FtpOptions : NetworkCredentialOptions
    {
        public FtpOptions(): base() { }
        public FtpOptions(IOptionsService options) : base(options) { }
        public FtpOptions(IOptionsService options, IInputService input) : base(options, input) { }
    }
}
