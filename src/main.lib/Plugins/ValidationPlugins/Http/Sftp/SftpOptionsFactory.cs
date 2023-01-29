using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Sftp validation
    /// </summary>
    internal class SftpOptionsFactory : HttpValidationOptionsFactory<SftpOptions>
    {
        public SftpOptionsFactory(Target target, ArgumentsInputService arguments) : base(arguments, target) { }

        public override bool PathIsValid(string path) => path.StartsWith("sftp://");

        public override string[] WebrootHint(bool allowEmpty)
        {
            return new[] {
                "SFTP path",
                "Example, sftp://domain.com:22/site/wwwroot/",
            };
        }

        public override async Task<SftpOptions?> Default()
        {
            return new SftpOptions(await BaseDefault())
            {
                Credential = await NetworkCredentialOptions.Create(_arguments)
            };
        }

        public override async Task<SftpOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new SftpOptions(await BaseAquire(inputService))
            {
                Credential = await NetworkCredentialOptions.Create(_arguments, inputService, "SFTP server")
            };
        }
    }
}