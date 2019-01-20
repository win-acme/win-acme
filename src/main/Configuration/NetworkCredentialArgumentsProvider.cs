using Fclp;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Configuration
{
    class NetworkCredentialArgumentsProvider : BaseArgumentsProvider<NetworkCredentialArguments>
    {
        public override string Name => "Credentials";
        public override string Group => "Validation";
        public override string Condition => "--validation ftp|sftp|webdav";

        public override void Configure(FluentCommandLineParser<NetworkCredentialArguments> parser)
        {
            parser.Setup(o => o.UserName)
                .As("username")
                .WithDescription("User name for WebDav/(s)ftp server");
            parser.Setup(o => o.Password)
                .As("password")
                .WithDescription("Password for WebDav/(s)ftp server");
        }

        public override bool Active(NetworkCredentialArguments current)
        {
            return !string.IsNullOrEmpty(current.UserName) ||
                !string.IsNullOrEmpty(current.Password);
        }
    }
}
