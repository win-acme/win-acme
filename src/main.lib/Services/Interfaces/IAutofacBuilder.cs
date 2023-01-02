using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Threading.Tasks;

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
        ILifetimeScope Legacy(ILifetimeScope main, Uri fromUri, Uri toUri);

        /// <summary>
        /// For configuration and renewal
        /// </summary>
        /// <param name="main"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        Task<ILifetimeScope> Target(ILifetimeScope main, Renewal renewal);

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel);

        /// <summary>
        /// For a single order, each order needs
        /// it own instance of the ICsrPlugin
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        ILifetimeScope Order(ILifetimeScope execution);

        /// <summary>
        /// Validation of a single identifier
        /// </summary>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        ILifetimeScope Validation(ILifetimeScope execution, ValidationPluginOptions options);
    }
}
