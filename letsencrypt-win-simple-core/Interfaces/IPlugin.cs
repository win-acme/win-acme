using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LetsEncrypt.ACME.Simple.Core.Configuration;

namespace LetsEncrypt.ACME.Simple.Core.Interfaces
{
    public interface IPlugin
    {
        /// <summary>
        /// A unique plugin identifier. ("IIS", "Manual", etc.)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Generates a list of hosts that certificates can be created for.
        /// </summary>
        /// <returns></returns>
        List<Target> GetTargets();

        /// <summary>
        /// Generates a list of sites that San certificates can be created for.
        /// </summary>
        /// <returns></returns>
        List<Target> GetSites();

        /// <summary>
        /// Can add a custom menu option.
        /// </summary>
        void PrintMenu();

        /// <summary>
        /// The code that is kicked off to authorize target, generate cert, install the cert, and setup renewal
        /// </summary>
        /// <param name="target">The target to process</param>
        void Auto(Target target);

        /// <summary>
        /// Handle custom menu option.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="targets"></param>
        void HandleMenuResponse(string response, List<Target> targets);

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        void BeforeAuthorize(Target target, string answerPath, string token);

        /// <summary>
        /// Can be used to print out helpful troubleshooting info for the user.
        /// </summary>
        /// <param name="target"></param>
        void OnAuthorizeFail(Target target);

        /// <summary>
        /// Should configure the server software to use the certificate.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pfxFilename"></param>
        /// <param name="store"></param>
        /// <param name="certificate"></param>
        void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate);

        /// <summary>
        /// Should configure the server software to use the certificate.
        /// The method with just a target is currently used to support Centralized SSL
        /// </summary>
        /// <param name="target"></param>
        void Install(Target target);

        /// <summary>
        /// Should renew the certificate
        /// </summary>
        /// <param name="target"></param>
        void Renew(Target target);

        /// <summary>
        /// Should create any directory structure needed and write the file for authorization
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="fileContents">the contents of the file to write</param>
        void CreateAuthorizationFile(string answerPath, string fileContents);

        /// <summary>
        /// Should delete any authorizations
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        /// <param name="webRootPath">the website root path</param>
        /// <param name="filePath">the file path for the authorization file</param>
        void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath);
    }
}