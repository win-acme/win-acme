using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Services
{
    internal class ValidationOptionsService : IValidationOptionsService
    {
        public ValidationPluginOptions? GetValidationOptions(Identifier identifier) => null; // new SelfHostingOptions();
    }
}
