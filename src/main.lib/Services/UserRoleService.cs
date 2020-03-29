using PKISharp.WACS.Clients.IIS;
using System;
using System.Security.Principal;

namespace PKISharp.WACS.Services
{
    internal class UserRoleService : IUserRoleService
    {
        private readonly IIISClient _iisClient;

        public UserRoleService(IIISClient iisClient) => _iisClient = iisClient;

        public bool AllowTaskScheduler => IsAdmin;
        public bool AllowCertificateStore => IsAdmin;
        public (bool, string?) AllowIIS
        {
            get
            {
                if (!(_iisClient.Version.Major > 6))
                {
                    return (false, "No supported version of IIS detected.");
                }
                else if (!IsAdmin)
                {
                    return (false, "Run as administrator to allow access to IIS.");
                }
                return (true, null);
            }
        }

        public bool IsAdmin => IsAdminLazy.Value;

        private Lazy<bool> IsAdminLazy => new Lazy<bool>(DetermineAdmin);

        private bool DetermineAdmin()
        {
            bool isAdmin;
            WindowsIdentity? user = null;
            try
            {
                //get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            finally
            {
                if (user != null)
                {
                    user.Dispose();
                }
            }
            return isAdmin;
        }
    }
}
