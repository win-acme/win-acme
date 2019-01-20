using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class FtpOptionsFactory : HttpValidationOptionsFactory<Ftp, FtpOptions>
    {
        public FtpOptionsFactory(ILogService log) : base(log) { }

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

        public override FtpOptions Default(Target target, IOptionsService optionsService)
        {
            return new FtpOptions(BaseDefault(target, optionsService))
            {
                Credential = new NetworkCredentialOptions(optionsService)
            };
        }

        public override FtpOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return new FtpOptions(BaseAquire(target, optionsService, inputService, runLevel))
            {
                Credential = new NetworkCredentialOptions(optionsService, inputService)
            };
        }
    }
}
