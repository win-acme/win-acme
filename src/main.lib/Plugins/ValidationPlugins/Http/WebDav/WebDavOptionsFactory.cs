using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class WebDavOptionsFactory : HttpValidationOptionsFactory<WebDav, WebDavOptions>
    {
        public WebDavOptionsFactory(IArgumentsService arguments) : base(arguments) { }

        public override bool PathIsValid(string webRoot)
        {
            return
                webRoot.StartsWith("\\\\") ||
                webRoot.StartsWith("dav://") ||
                webRoot.StartsWith("webdav://") ||
                webRoot.StartsWith("https://") ||
                webRoot.StartsWith("http://");
        }

        public override string[] WebrootHint(bool allowEmtpy)
        {
            return new[] {
                "Enter a webdav path that leads to the web root of the host for http authentication",
                " Example, \\\\domain.com:80\\",
                " Example, \\\\domain.com:443\\"
            };
        }

        public override async Task<WebDavOptions?> Default(Target target)
        {
            return new WebDavOptions(BaseDefault(target))
            {
                Credential = new NetworkCredentialOptions(_arguments)
            };
        }

        public override async Task<WebDavOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            return new WebDavOptions(await BaseAquire(target, inputService))
            {
                Credential = new NetworkCredentialOptions(_arguments, inputService)
            };
        }
    }
}
