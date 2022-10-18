using PKISharp.WACS.Clients.IIS;

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

        public (bool, string?) AllowIIS
        {
            get
            {
                if (!_adminService.IsAdmin)
                {
                    return (false, "Run as administrator to allow access to IIS.");
                }
                if (_iisClient.Version.Major <= 6)
                {
                    return (false, "No supported version of IIS detected.");
                }
                return (true, null);
            }
        }

        public bool AllowLegacy => _adminService.IsAdmin;

        public bool AllowSelfHosting => _adminService.IsAdmin;
    }
}
