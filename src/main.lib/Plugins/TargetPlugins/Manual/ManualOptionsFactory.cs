using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
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
            GetString<ManualArguments>(x => x.CommonName).
            Validate(x => Task.FromResult(x?.Length <= Constants.MaxCommonName), $"Common name too long (max {Constants.MaxCommonName} characters)");

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
            var sanList = input.
                ParseCsv()?.
                Select(x => x.ConvertPunycode()).
                Select(Manual.ParseIdentifier);
            if (sanList != null)
            {
                var commonName = sanList.
                    OfType<DnsIdentifier>().
                    Where(x => x.Value.Length <= Constants.MaxCommonName).
                    FirstOrDefault();
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

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(ManualOptions options)
        {
            if (options.AlternativeNames.FirstOrDefault() != options.CommonName)
            {
                yield return (Common.Meta, options.CommonName);
            }
            yield return (Host.Meta, options.AlternativeNames);
        }
    }
}
