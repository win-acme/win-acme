using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    public class HttpWebDavOptions : NetworkCredentialOptions
    {
        public HttpWebDavOptions(): base() { }
        public HttpWebDavOptions(Options options) : base(options) { }
        public HttpWebDavOptions(Options options, InputService input) : base(options, input) { }
    }
}
