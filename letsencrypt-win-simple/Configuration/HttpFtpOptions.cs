using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    public class HttpFtoOptions : NetworkCredentialOptions
    {
        public HttpFtoOptions(): base() { }
        public HttpFtoOptions(Options options) : base(options) { }
        public HttpFtoOptions(Options options, InputService input) : base(options, input) { }
    }
}
