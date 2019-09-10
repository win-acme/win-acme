using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class ManualOptionsFactory : TargetPluginOptionsFactory<Manual, ManualOptions>
    {
        private readonly IArgumentsService _arguments;
        public ManualOptionsFactory(IArgumentsService arguments) => _arguments = arguments;

        public override Task<ManualOptions> Aquire(IInputService inputService, RunLevel runLevel)
        {
            var input = inputService.RequestString("Enter comma-separated list of host names, starting with the common name");
            if (string.IsNullOrEmpty(input))
            {
                return Task.FromResult(default(ManualOptions));
            }
            else
            {
                return Task.FromResult(Create(input));
            }
        }

        public override Task<ManualOptions> Default()
        {
            var args = _arguments.GetArguments<ManualArguments>();
            var input = _arguments.TryGetRequiredArgument(nameof(args.Host), args.Host);
            var ret = Create(input);
            var commonName = args.CommonName;
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                commonName = commonName.ToLower().Trim().ConvertPunycode();
                ret.CommonName = commonName;
                if (!ret.AlternativeNames.Contains(commonName))
                {
                    ret.AlternativeNames.Insert(0, commonName);
                }
            }
            return Task.FromResult(ret);
        }

        private ManualOptions Create(string input)
        {
            var sanList = input.ParseCsv().Select(x => x.ConvertPunycode());
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
