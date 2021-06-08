using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Sftp validation
    /// </summary>
    internal class SftpOptionsFactory : HttpValidationOptionsFactory<Sftp, SftpOptions>
    {
        public SftpOptionsFactory(ArgumentsInputService arguments) : base(arguments) { }

        public override bool PathIsValid(string path) => path.StartsWith("sftp://");

        public override string[] WebrootHint(bool allowEmpty)
        {
            return new[] {
                "Enter an sftp path that leads to the web root of the host for sftp authentication",
                " Example, sftp://domain.com:22/site/wwwroot/"
            };
        }

        public override async Task<SftpOptions?> Default(Target target)
        {
            return new SftpOptions(await BaseDefault(target))
            {
                Credential = await NetworkCredentialOptions.Create(_arguments)
            };
        }

        public override async Task<SftpOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            return new SftpOptions(await BaseAquire(target, inputService))
            {
                Credential = await NetworkCredentialOptions.Create(_arguments, inputService, "SFTP server")
            };
        }
    }
}