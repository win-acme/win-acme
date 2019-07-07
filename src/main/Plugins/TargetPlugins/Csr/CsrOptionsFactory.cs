using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class CsrOptionsFactory : TargetPluginOptionsFactory<Csr, CsrOptions>
    {
        public CsrOptionsFactory(ILogService log) : base(log) { }

        public override CsrOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var args = arguments.GetArguments<CsrArguments>();
            var ret = new CsrOptions();
            do
            {
                ret.CsrFile = arguments.TryGetArgument(
                    args.CsrFile, 
                    inputService, 
                    "Enter the path to the CSR");
            }
            while (!ret.PkFile.ValidFile(_log));

            string pkFile;
            do
            {
                pkFile = arguments.TryGetArgument(args.CsrFile, 
                    inputService,
                    "Enter the path to the corresponding private key, or <ENTER> to create a certificate without one");
            }
            while (!(string.IsNullOrWhiteSpace(pkFile) || pkFile.ValidFile(_log)));

            if (!string.IsNullOrWhiteSpace(pkFile))
            {
                ret.PkFile = pkFile;
            }

            return ret;
        }

        public override CsrOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<CsrArguments>();
            if (!args.CsrFile.ValidFile(_log))
            {
                return null;
            }
            if (!string.IsNullOrEmpty(args.PkFile))
            {
                if (!args.PkFile.ValidFile(_log))
                {
                    return null;
                }
            }
            return new CsrOptions()
            {
                CsrFile = args.CsrFile,
                PkFile = string.IsNullOrWhiteSpace(args.PkFile) ? null : args.PkFile
            };
        }
    }
}
