using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System.Net;

namespace PKISharp.WACS.Configuration
{
    public class NetworkCredentialOptions
    {
        public string UserName { get; set; }
        public string PasswordSafe { get; set; }

        [JsonIgnore]
        public string Password {
            get => PasswordSafe.Unprotect();
            set => PasswordSafe = value.Protect();
        }

        public NetworkCredential GetCredential()
        {
            return new NetworkCredential(UserName, Password);
        }

        public void Show(IInputService input)
        {
            input.Show("User", UserName);
            input.Show("Password", new string('*', Password.Length));
        }

        public NetworkCredentialOptions() { }

        public NetworkCredentialOptions(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public NetworkCredentialOptions(IOptionsService options)
        {
            UserName = options.TryGetRequiredOption(nameof(options.Options.UserName), options.Options.UserName);
            Password = options.TryGetRequiredOption(nameof(options.Options.Password), options.Options.Password);
        }

        public NetworkCredentialOptions(IOptionsService options, IInputService input)
        {
            UserName = options.TryGetOption(options.Options.UserName, input, "Username");
            Password = options.TryGetOption(options.Options.Password, input, "Password", true);
        }
    }
}
