using Newtonsoft.Json;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Configuration
{
    public class NetworkCredentialOptions
    {
        public string? UserName { get; set; }

        [JsonProperty(propertyName: "PasswordSafe")]
        public ProtectedString? Password { get; set; }

        public NetworkCredential GetCredential(
            SecretServiceManager secretService) =>
            new(UserName, secretService.EvaluateSecret(Password?.Value));

        public void Show(IInputService input)
        {
            input.Show("Username", UserName);
            input.Show("Password", Password?.DisplayValue);
        }

        public NetworkCredentialOptions() { }

        public NetworkCredentialOptions(string? userName, string? password) : this(userName, password.Protect()) { }
        public NetworkCredentialOptions(string? userName, ProtectedString? password)
        {
            UserName = userName;
            Password = password;
        }

        public static async Task<NetworkCredentialOptions> Create(ArgumentsInputService arguments)
        {
            return new NetworkCredentialOptions(
                await arguments.GetString<NetworkCredentialArguments>(x => x.UserName).GetValue(),
                await arguments.GetProtectedString<NetworkCredentialArguments>(x => x.Password).GetValue()
            );
        }

        public static async Task<NetworkCredentialOptions> Create(ArgumentsInputService arguments, IInputService input, string purpose)
        {
            return new NetworkCredentialOptions(
                await arguments.GetString<NetworkCredentialArguments>(x => x.UserName).Interactive(input, purpose + " username").GetValue(),
                await arguments.GetProtectedString<NetworkCredentialArguments>(x => x.Password).Interactive(input, purpose + "password").GetValue()
            );
        }
    }
}
