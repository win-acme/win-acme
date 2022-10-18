using PKISharp.WACS.Services;
namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class UserRoleService : IUserRoleService
    {
        public bool AllowCertificateStore => true;

        public (bool, string?) AllowIIS => (true, null);

        public bool AllowTaskScheduler => true;

        public bool AllowLegacy => true;

        public bool AllowSelfHosting => true;
    }
}
