using ACMESharp;
using LetsEncrypt.ACME.Simple.Plugins;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple
{
    /// <summary>
    /// To create a new server plugin, simply create a sub-class of Plugin in this project. It will be loaded and run automatically.
    /// </summary>
    public abstract class Plugin : IHasName
    {
        /// <summary>
        /// A unique plugin identifier. ("IIS", "Manual", etc.)
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Returns the kind of challenge type (HTTP or DNS) that is supported by the plugin.
        /// </summary>
        public virtual string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_HTTP;

        /// <summary>
        /// Can add a custom menu option.
        /// </summary>
        public virtual string MenuOption => string.Empty;

        /// <summary>
        /// Can add a custom menu option.
        /// </summary>
        public virtual string Description => string.Empty;

        /// <summary>
        /// Can add a custom menu option.
        /// </summary>
        public virtual void Run() { }

        /// <summary>
        /// The code that is kicked off to authorize target, generate cert, install the cert, and setup renewal
        /// </summary>
        /// <param name="binding">The target to process</param>
        public virtual void Auto(Target target)
        {
            Program.Auto(target);
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        public virtual void BeforeAuthorize(Target target, string answerPath, string token)
        {
        }

        /// <summary>
        /// Should configure the server software to use the certificate.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pfxFilename"></param>
        /// <param name="store"></param>
        /// <param name="certificate"></param>
        public abstract void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate);

        /// <summary>
        /// Should configure the server software to use the certificate.
        /// The method with just a target is currently used to support Centralized SSL
        /// </summary>
        /// <param name="target"></param>
        public abstract void Install(Target target);

        /// <summary>
        /// Should renew the certificate
        /// </summary>
        /// <param name="target"></param>
        public abstract void Renew(Target target);

        /// <summary>
        /// Should create any directory structure needed and write the file for authorization
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="fileContents">the contents of the file to write</param>
        public virtual void CreateAuthorizationFile(string answerPath, string fileContents)
        {
        }

        /// <summary>
        /// Should delete any authorizations
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        /// <param name="webRootPath">the website root path</param>
        /// <param name="filePath">the file path for the authorization file</param>
        public virtual void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
        }
    }
}