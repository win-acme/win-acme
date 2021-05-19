namespace PKISharp.WACS.Configuration.Arguments
{
    internal class NetworkCredentialArguments : BaseArguments
    {
        public override string Name => "Credentials";
        public override string Group => "Validation";
        public override string Condition => "--validation ftp|sftp|webdav";

        [CommandLine(Description = "Username for remote server")]
        public string? UserName { get; set; }

        [CommandLine(Description = "Password for remote server", Secret = true)]
        public string? Password { get; set; }
    }
}
