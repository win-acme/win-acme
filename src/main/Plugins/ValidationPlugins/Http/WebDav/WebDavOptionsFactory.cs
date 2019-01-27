using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class WebDavOptionsFactory : HttpValidationOptionsFactory<WebDav, WebDavOptions>
    {
        public WebDavOptionsFactory(ILogService log) : base(log) { }

        public override bool PathIsValid(string webRoot) => webRoot.StartsWith("\\\\");

        public override string[] WebrootHint(bool allowEmtpy)
        {
            return new[] {
                "Enter a webdav path that leads to the web root of the host for http authentication",
                " Example, \\\\domain.com:80\\",
                " Example, \\\\domain.com:443\\"
            };
        }

        public override WebDavOptions Default(Target target, IArgumentsService arguments)
        {
            return new WebDavOptions(BaseDefault(target, arguments))
            {
                Credential = new NetworkCredentialOptions(arguments)
            };
        }

        public override WebDavOptions Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return new WebDavOptions(BaseAquire(target, arguments, inputService, runLevel))
            {
                Credential = new NetworkCredentialOptions(arguments, inputService)
            };
        }
    }
}
