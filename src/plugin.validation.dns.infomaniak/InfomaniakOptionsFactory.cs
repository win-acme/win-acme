using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns;

/// <summary>
/// Infomaniak DNS validation
/// </summary>
internal class InfomaniakOptionsFactory : PluginOptionsFactory<InfomaniakOptions>
{
    private readonly ArgumentsInputService _arguments;

    public InfomaniakOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

    private ArgumentResult<ProtectedString?> ApiKey => _arguments.
        GetProtectedString<InfomaniakArguments>(a => a.ApiToken).
        Required();

    public override async Task<InfomaniakOptions?> Aquire(IInputService input, RunLevel runLevel)
    {
        return new InfomaniakOptions()
        {
            ApiToken = await ApiKey.Interactive(input).GetValue()
        };
    }

    public override async Task<InfomaniakOptions?> Default()
    {
        return new InfomaniakOptions()
        {
            ApiToken = await ApiKey.GetValue()
        };
    }

    public override IEnumerable<(CommandLineAttribute, object?)> Describe(InfomaniakOptions options)
    {
        yield return (ApiKey.Meta, options.ApiToken);
    }
}
