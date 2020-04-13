using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IOrderPluginOptionsFactory : IPluginOptionsFactory<OrderPluginOptions>
    {
        /// <summary>
        /// Is the order splitting option available for a specific target?
        /// Used to rule out unfit orders
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanProcess(Target target);
    }
}
