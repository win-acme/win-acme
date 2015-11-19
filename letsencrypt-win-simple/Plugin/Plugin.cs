using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    /// <summary>
    /// To create a new server plugin, simply create a sub-class of Plugin in this project. It will be loaded and run automatically.
    /// </summary>
    public abstract class Plugin
    {
        /// <summary>
        /// A unique plugin identifier. ("IIS", "Manual", etc.)
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Generates a list of hosts that certificates can be created for.
        /// </summary>
        /// <returns></returns>
        public abstract List<Target> GetTargets();

        /// <summary>
        /// Can add a custom menu option.
        /// </summary>
        public virtual void PrintMenu() { }

        /// <summary>
        /// Handle custom menu option.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="targets"></param>
        public virtual void HandleMenuResponse(string response, List<Target> targets) { }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        public virtual void BeforeAuthorize(Target target, string answerPath) { }

        /// <summary>
        /// Can be used to print out helpful troubleshooting info for the user.
        /// </summary>
        /// <param name="target"></param>
        public virtual void OnAuthorizeFail(Target target) { }

        /// <summary>
        /// Should configure the server software to use the certificate.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pfxFilename"></param>
        /// <param name="store"></param>
        /// <param name="certificate"></param>
        public abstract void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate);
    }
}
