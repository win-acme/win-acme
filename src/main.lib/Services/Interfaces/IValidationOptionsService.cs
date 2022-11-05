using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public interface IValidationOptionsService
    {
        Task<ValidationPluginOptions?> GetValidationOptions(Identifier identifier);
    }
}
