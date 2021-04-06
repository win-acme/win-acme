using Fclp;

namespace PKISharp.WACS.Configuration.Arguments
{
    internal class NetworkCredentialArgumentsProvider : BaseArgumentsProvider<NetworkCredentialArguments>
    {
        public override string Name => "Credentials";
        public override string Group => "Validation";
        public override string Condition => "--validation ftp|sftp|webdav";

        public override void Configure(FluentCommandLineParser<NetworkCredentialArguments> parser)
        {
            parser.Setup(o => o.UserName)
                .As("username")
                .WithDescription("User name for remote server");
            parser.Setup(o => o.Password)
                .As("password")
                .WithDescription("Password for remote server");
        }
    }
}
