using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Services
{
    internal class UserRoleService : IUserRoleService
    {
        private readonly IIISClient _iisClient;
        private readonly AdminService _adminService;

        public UserRoleService(IIISClient iisClient, AdminService adminService)
        {
            _iisClient = iisClient;
            _adminService = adminService;
        }

        public bool AllowTaskScheduler => _adminService.IsAdmin;

        public bool AllowCertificateStore => _adminService.IsAdmin;

        public bool AllowLegacy => _adminService.IsAdmin;

        public bool AllowSelfHosting => _adminService.IsAdmin;

        public State IISState
        {
            get
            {
                if (!_adminService.IsAdmin)
                {
                    return State.DisabledState("Run as administrator to allow access to IIS.");
                }
                if (_iisClient.Version.Major <= 6)
                {
                    return State.DisabledState("No supported version of IIS detected.");
                }
                return State.EnabledState();
            }
        }
    }
}
