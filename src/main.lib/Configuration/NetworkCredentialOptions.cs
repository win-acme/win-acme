using Newtonsoft.Json;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Net;

namespace PKISharp.WACS.Configuration
{
    public class NetworkCredentialOptions
    {
        public string? UserName { get; set; }

        [JsonProperty(propertyName: "PasswordSafe")]
        public ProtectedString? Password { get; set; }

        public NetworkCredential GetCredential() => new(UserName, Password?.Value);

        public void Show(IInputService input)
        {
            input.Show("Username", UserName);
            input.Show("Password", Password?.DisplayValue);
        }

        public NetworkCredentialOptions() { }

        public NetworkCredentialOptions(string? userName, string? password)
        {
            UserName = userName;
            Password = new ProtectedString(password);
        }

        public NetworkCredentialOptions(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<NetworkCredentialArguments>();
            UserName = arguments.TryGetRequiredArgument(nameof(args.UserName), args?.UserName);
            Password = new ProtectedString(arguments.TryGetRequiredArgument(nameof(args.Password), args?.Password));
        }

        public NetworkCredentialOptions(IArgumentsService arguments, IInputService input, string purpose, SecretServiceManager secretService)
        {
            var args = arguments.GetArguments<NetworkCredentialArguments>();
            UserName = arguments.TryGetArgument(args?.UserName, input, $"{purpose} username").Result;
            Password = new ProtectedString(secretService.GetSecret($"{purpose} password", args?.Password).Result);
        }
    }
}
