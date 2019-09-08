using PKISharp.WACS.DomainObjects;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ITargetPlugin
    {
        /// <summary>
        /// Generate target based on the specified options
        /// </summary>
        /// <returns></returns>
        Task<Target> Generate();
    }
}
