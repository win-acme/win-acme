using PKISharp.WACS.Clients;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Sftp validation
    /// </summary>
    internal class SftpOptionsFactory : BaseHttpValidationOptionsFactory<Sftp, SftpOptions>
    {
        public SftpOptionsFactory(ILogService log, IISClient iisClient) : base(log, iisClient) { }

        public override bool PathIsValid(string path) => path.StartsWith("sftp://");

        public override string[] WebrootHint(bool allowEmpty)
        {
            return new[] {
                "Enter an sftp path that leads to the web root of the host for sftp authentication",
                " Example, sftp://domain.com:22/site/wwwroot/"
            };
        }

        public override SftpOptions Default(Target target, IOptionsService optionsService)
        {
            return new SftpOptions(BaseDefault(target, optionsService))
            {
                Credential = new NetworkCredentialOptions(optionsService)
            };
        }

        public override SftpOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return new SftpOptions(BaseAquire(target, optionsService, inputService, runLevel))
            {
                Credential = new NetworkCredentialOptions(optionsService, inputService)
            };
        }
    }
}