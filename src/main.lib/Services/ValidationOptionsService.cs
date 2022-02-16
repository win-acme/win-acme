using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;

namespace PKISharp.WACS.Services
{
    internal class ValidationOptionsService : IValidationOptionsService
    {
        public ValidationPluginOptions? GetValidationOptions(Identifier identifier) => new SelfHostingOptions();
    }
}
