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
            input.Show("Username", UserName);
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
            var args = options.GetArguments<NetworkCredentialArguments>();
            UserName = options.TryGetRequiredOption(nameof(args.UserName), args.UserName);
            Password = options.TryGetRequiredOption(nameof(args.Password), args.Password);
        }

        public NetworkCredentialOptions(IOptionsService options, IInputService input)
        {
            var args = options.GetArguments<NetworkCredentialArguments>();
            UserName = options.TryGetOption(args.UserName, input, "Username");
            Password = options.TryGetOption(args.Password, input, "Password", true);
        }
    }
}
