using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    public class NetworkCredentialOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public NetworkCredential GetCredential()
        {
            return new NetworkCredential(UserName, Password);
        }

        public NetworkCredentialOptions() { }

        public NetworkCredentialOptions(IOptionsService options)
        {
            UserName = options.TryGetRequiredOption(nameof(options.Options.UserName), options.Options.UserName);
            Password = options.TryGetRequiredOption(nameof(options.Options.Password), options.Options.Password);
        }

        public NetworkCredentialOptions(IOptionsService options, IInputService input)
        {
            UserName = options.TryGetOption(options.Options.UserName, input, "Username");
            Password = options.TryGetOption(options.Options.Password, input, "Password");
        }
    }
}
