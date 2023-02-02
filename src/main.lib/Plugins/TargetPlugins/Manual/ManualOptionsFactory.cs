using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class ManualOptionsFactory : PluginOptionsFactory<ManualOptions>
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
            var sanList = input.ParseCsv()?.Select(x => x.ConvertPunycode()).Select(x => Manual.ParseIdentifier(x));
            if (sanList != null)
            {
                var commonName = sanList.OfType<DnsIdentifier>().FirstOrDefault();
                return new ManualOptions()
                {
                    CommonName = commonName?.Value,
                    AlternativeNames = sanList.Select(x => x.Value).ToList()
                };
            }
            else
            {
                return null;
            }
        }

        public override IEnumerable<string> Describe(ManualOptions options)
        {
            if (options.CommonName != null)
            {
                yield return $"{Common.ArgumentName} {Escape(options.CommonName)}";
            }
            if (options.AlternativeNames != null)
            {
                yield return $"{Host.ArgumentName} {Escape(string.Join(",", options.AlternativeNames))}";
            }
        }
    }
}
