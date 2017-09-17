using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    public class FtpOptions : NetworkCredentialOptions
    {
        public FtpOptions(Options options) : base(options) { }
        public FtpOptions(Options options, InputService input) : base(options, input) { }
    }
}
