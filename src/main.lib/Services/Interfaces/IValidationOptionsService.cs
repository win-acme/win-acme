using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Services
{
    public interface IValidationOptionsService
    {
        ValidationPluginOptions? GetValidationOptions(Identifier identifier);
    }
}
