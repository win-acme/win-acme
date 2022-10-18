namespace PKISharp.WACS.Services
{
    public interface IUserRoleService
    {
        bool AllowCertificateStore { get; }
        (bool, string?) AllowIIS { get; }
        bool AllowTaskScheduler { get; }
        bool AllowLegacy { get; }
        bool AllowSelfHosting { get; }
    }
}
