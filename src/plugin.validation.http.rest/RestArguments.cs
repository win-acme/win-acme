using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal sealed class RestArguments : BaseArguments
    {
        public override string Name => "Rest plugin";
        public override string Condition => "--validation rest";
        public override string Group => "Validation";

        [CommandLine(Name = "rest-securitytoken", Description = "The bearer token needed to authenticate with the REST API on the server for PUT / DELETE requests.", Secret = true)]
        public string? SecurityToken { get; set; }

        [CommandLine(Name = "rest-usehttps", Description = "If HTTPS should be used instead of HTTP. Must be true if the server has HTTP to HTTPS redirection configured, as the redirected request always uses the GET method.")]
        public bool UseHttps { get; set; }
    }
}
