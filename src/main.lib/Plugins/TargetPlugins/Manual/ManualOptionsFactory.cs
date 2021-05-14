using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class ManualOptionsFactory : TargetPluginOptionsFactory<Manual, ManualOptions>
    {
        private readonly ArgumentsInputService _arguments;
        public ManualOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;
        public override int Order => 5;

        private ArgumentResult<string?> Host => _arguments.
            GetString<ManualArguments>(x => x.Host).
            Required();

        private ArgumentResult<string?> Common => _arguments.
            GetString<ManualArguments>(x => x.CommonName);

        public override async Task<ManualOptions?> Aquire(IInputService inputService, RunLevel runLevel) => 
            Create(await Host.Interactive(inputService).GetValue());

        public override async Task<ManualOptions?> Default()
        {
            var ret = Create(await Host.GetValue());
            if (ret != null)
            {
                var commonName = await Common.GetValue();
                if (!string.IsNullOrWhiteSpace(commonName))
                {
                    commonName = commonName.ToLower().Trim().ConvertPunycode();
                    ret.CommonName = commonName;
                    if (!ret.AlternativeNames.Contains(commonName))
                    {
                        ret.AlternativeNames.Insert(0, commonName);
                    }
                }
            }
            return ret;
        }

        private static ManualOptions? Create(string? input)
        {
            var sanList = input.ParseCsv()?.Select(x => x.ConvertPunycode());
            if (sanList != null)
            {
                return new ManualOptions()
                {
                    CommonName = sanList.First(),
                    AlternativeNames = sanList.ToList()
                };
            }
            else
            {
                return null;
            }
        }
    }
}
