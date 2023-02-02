using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class FtpOptionsFactory : HttpValidationOptionsFactory<FtpOptions>
    {
        private readonly ILogService _log;

        public FtpOptionsFactory(ILogService log, Target target, ArgumentsInputService arguments) : 
            base(arguments, target) 
            => _log = log;

        public override bool PathIsValid(string path)
        {
            try
            {
                var uri = new Uri(path);
                return uri.Scheme == "ftp" || uri.Scheme == "ftps";
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Invalid path");
                return false;
            }
        }

        public override string[] WebrootHint(bool allowEmpty)
        {
            return new[] {
                "FTP path",
                "Example, ftp://domain.com:21/site/wwwroot/",
                "Example, ftps://domain.com:990/site/wwwroot/"
            };
        }

        public override async Task<FtpOptions?> Default()
        {
            return new FtpOptions(await BaseDefault())
            {
                Credential = await NetworkCredentialOptions.Create(_arguments)
            };
        }

        public override async Task<FtpOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            var baseOptions = await BaseAquire(inputService);
            return new FtpOptions(baseOptions)
            {
                Credential = await NetworkCredentialOptions.Create(_arguments, inputService, "FTP(S) server")
            };
        }

        public override IEnumerable<string> Describe(FtpOptions options)
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
