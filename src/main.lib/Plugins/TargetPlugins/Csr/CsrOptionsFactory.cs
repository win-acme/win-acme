using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class CsrOptionsFactory : PluginOptionsFactory<CsrOptions>
    {
        private readonly ILogService _log;
        private readonly ArgumentsInputService _arguments;

        public CsrOptionsFactory(ILogService log, ArgumentsInputService arguments)
        {
            _log = log;
            _arguments = arguments;
        }

        public override int Order => 6;

        private ArgumentResult<string?> CsrFile => _arguments.
            GetString<CsrArguments>(x => x.CsrFile).
            Required().
            Validate(x => Task.FromResult(x.ValidFile(_log)), "invalid file");

        private ArgumentResult<string?> PkFile => _arguments.
            GetString<CsrArguments>(x => x.PkFile).
            Validate(x => Task.FromResult(x.ValidFile(_log)), "invalid file");

        public override async Task<CsrOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new CsrOptions()
            {
                PkFile = await PkFile.Interactive(inputService).GetValue(),
                CsrFile = await CsrFile.Interactive(inputService).GetValue()
            };
        }

        public override async Task<CsrOptions?> Default()
        {
            return new CsrOptions()
            {
                PkFile = await PkFile.GetValue(),
                CsrFile = await CsrFile.GetValue()
            };
        }

        public override IEnumerable<string> Describe(CsrOptions options)
        {
            if (options.CsrFile != null)
            {
                yield return $"{CsrFile.ArgumentName} {Escape(options.CsrFile)}";
            }
            if (options.PkFile != null)
            {
                yield return $"{PkFile.ArgumentName} {Escape(options.PkFile)}";
            }
        }
    }
}
