using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Services
{
    internal interface IAutofacBuilder
    {
        /// <summary>
        /// This is used to import renewals from 1.9.x
        /// </summary>
        /// <param name="main"></param>
        /// <param name="fromUri"></param>
        /// <param name="toUri"></param>
        /// <returns></returns>
        ILifetimeScope Legacy(ILifetimeScope main, string fromUri, string toUri);

        /// <summary>
        /// For revocation and configuration
        /// </summary>
        /// <param name="main"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        ILifetimeScope Configuration(ILifetimeScope main, Renewal renewal, RunLevel runLevel);

        /// <summary>
        /// For configuration and renewal
        /// </summary>
        /// <param name="main"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        ILifetimeScope Target(ILifetimeScope main, Renewal renewal, RunLevel runLevel);

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel);

        /// <summary>
        /// Validation of a single identifier
        /// </summary>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        ILifetimeScope Validation(ILifetimeScope execution, ValidationPluginOptions options, TargetPart target, string identifier);
    }
}
