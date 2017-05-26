using System;
using FubarDev.FtpServer.AccountManagement;

namespace letsencrypt_tests.Support
{
    internal class MockFtpMembershipProvider : IMembershipProvider
    {
        public MemberValidationResult ValidateUser(string username, string password)
        {
            return new MemberValidationResult(MemberValidationStatus.AuthenticatedUser, new FtpUser(username));
        }
    }
}