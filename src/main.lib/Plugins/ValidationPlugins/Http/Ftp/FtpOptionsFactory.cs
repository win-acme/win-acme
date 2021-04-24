using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class FtpOptionsFactory : HttpValidationOptionsFactory<Ftp, FtpOptions>
    {
        private readonly ILogService _log;
        private readonly SecretServiceManager _secretServiceManager;

        public FtpOptionsFactory(
            ILogService log,
            IArgumentsService arguments,
            SecretServiceManager secretServiceManager) : base(arguments) {
            _log = log;
            _secretServiceManager = secretServiceManager;
        } 

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
                "Enter an ftp path that leads to the web root of the host for http authentication",
                " Example, ftp://domain.com:21/site/wwwroot/",
                " Example, ftps://domain.com:990/site/wwwroot/"
            };
        }

        public override async Task<FtpOptions?> Default(Target target)
        {
            return new FtpOptions(BaseDefault(target))
            {
                Credential = new NetworkCredentialOptions(_arguments)
            };
        }

        public override async Task<FtpOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            return new FtpOptions(await BaseAquire(target, inputService))
            {
                Credential = new NetworkCredentialOptions(_arguments, inputService, "FTP(S) server", _secretServiceManager)
            };
        }
    }
}
