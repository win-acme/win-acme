using System;
using System.Security.Principal;

namespace PKISharp.WACS.Services
{
    public class AdminService
    {
        public bool IsAdmin => IsAdminLazy.Value;

        private Lazy<bool> IsAdminLazy => new(DetermineAdmin);

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
