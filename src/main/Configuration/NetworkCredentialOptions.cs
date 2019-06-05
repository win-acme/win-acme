using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System.Net;

namespace PKISharp.WACS.Configuration
{
    public class NetworkCredentialOptions
    {
        public string UserName { get; set; }
        [JsonConverter(typeof(protectedStringConverter))]
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

        public NetworkCredentialOptions(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<NetworkCredentialArguments>();
            UserName = arguments.TryGetRequiredArgument(nameof(args.UserName), args.UserName);
            Password = arguments.TryGetRequiredArgument(nameof(args.Password), args.Password);
        }

        public NetworkCredentialOptions(IArgumentsService arguments, IInputService input)
        {
            var args = arguments.GetArguments<NetworkCredentialArguments>();
            UserName = arguments.TryGetArgument(args.UserName, input, "Username");
            Password = arguments.TryGetArgument(args.Password, input, "Password", true);
        }
    }
}
