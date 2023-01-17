using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Services
{
    public interface IUserRoleService
    {
        bool AllowCertificateStore { get; }
        State IISState { get; }
        bool AllowTaskScheduler { get; }
        bool AllowLegacy { get; }
        bool AllowSelfHosting { get; }
    }
}
