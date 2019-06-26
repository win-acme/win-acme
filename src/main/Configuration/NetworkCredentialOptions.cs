using Newtonsoft.Json;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Net;

namespace PKISharp.WACS.Configuration
{
    public class NetworkCredentialOptions
    {
        public string UserName { get; set; }

        [JsonProperty(propertyName: "PasswordSafe")]
        public ProtectedString Password { get; set; }

        public NetworkCredential GetCredential()
        {
            return new NetworkCredential(UserName, Password.Value);
        }

        public void Show(IInputService input)
        {
            input.Show("Username", UserName);
            input.Show("Password", new string('*', Password.Value.Length));
        }

        public NetworkCredentialOptions() { }

        public NetworkCredentialOptions(string userName, string password)
        {
            UserName = userName;
            Password = new ProtectedString(password);
        }

        public NetworkCredentialOptions(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<NetworkCredentialArguments>();
            UserName = arguments.TryGetRequiredArgument(nameof(args.UserName), args.UserName);
            Password = new ProtectedString(arguments.TryGetRequiredArgument(nameof(args.Password), args.Password));
        }

        public NetworkCredentialOptions(IArgumentsService arguments, IInputService input)
        {
            var args = arguments.GetArguments<NetworkCredentialArguments>();
            UserName = arguments.TryGetArgument(args.UserName, input, "Username");
            Password = new ProtectedString(arguments.TryGetArgument(args.Password, input, "Password", true));
        }
    }
}
