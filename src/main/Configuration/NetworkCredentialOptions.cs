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
            UserName = options.TryGetRequiredOption(nameof(options.MainArguments.UserName), options.MainArguments.UserName);
            Password = options.TryGetRequiredOption(nameof(options.MainArguments.Password), options.MainArguments.Password);
        }

        public NetworkCredentialOptions(IOptionsService options, IInputService input)
        {
            UserName = options.TryGetOption(options.MainArguments.UserName, input, "Username");
            Password = options.TryGetOption(options.MainArguments.Password, input, "Password", true);
        }
    }
}
