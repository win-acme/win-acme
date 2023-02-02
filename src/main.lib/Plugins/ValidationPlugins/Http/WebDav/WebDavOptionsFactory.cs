using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class WebDavOptionsFactory : HttpValidationOptionsFactory<WebDavOptions>
    {
        public WebDavOptionsFactory(Target target, ArgumentsInputService arguments) : base(arguments, target) { }

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
                "WebDav path",
                "Example, \\\\domain.com:80\\",
                "Example, \\\\domain.com:443\\"
            };
        }

        public override async Task<WebDavOptions?> Default()
        {
            return new WebDavOptions(await BaseDefault())
            {
                Credential = await NetworkCredentialOptions.Create(_arguments)
            };
        }

        public override async Task<WebDavOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new WebDavOptions(await BaseAquire(inputService))
            {
                Credential = await NetworkCredentialOptions.Create(_arguments, inputService, "WebDav server")
            };
        }

        public override IEnumerable<string> Describe(WebDavOptions options)
        {
            foreach (var x in base.Describe(options))
            {
                yield return x;
            }
            if (options.Credential != null)
            {
                foreach (var x in options.Credential.Describe(_arguments))
                {
                    yield return x;
                }
            }
        }
    }
}
