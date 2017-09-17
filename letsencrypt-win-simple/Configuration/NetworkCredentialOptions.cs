using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    public class NetworkCredentialOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public NetworkCredentialOptions() { }

        public NetworkCredentialOptions(Options options)
        {
            UserName = options.TryGetRequiredOption(nameof(options.UserName), options.UserName);
            Password = options.TryGetRequiredOption(nameof(options.Password), options.Password);
        }

        public NetworkCredentialOptions(Options options, InputService input)
        {
            UserName = options.TryGetOption(options.UserName, input, "User name");
            Password = options.TryGetOption(options.Password, input, "Password");
        }
    }
}
