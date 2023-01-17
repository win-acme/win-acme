using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class UserRoleService : IUserRoleService
    {
        public bool AllowCertificateStore => true;

        public State IISState => State.EnabledState();

        public bool AllowTaskScheduler => true;

        public bool AllowLegacy => true;

        public bool AllowSelfHosting => true;
    }
}
