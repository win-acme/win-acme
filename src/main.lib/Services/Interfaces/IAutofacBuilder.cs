using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
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
        Task<ILifetimeScope> MainTarget(ILifetimeScope main, Renewal renewal);

        /// <summary>
        /// For different targets split up by the order plugin
        /// </summary>
        /// <param name="main"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        ILifetimeScope SubTarget(ILifetimeScope main, Target target);

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel);

        /// <summary>
        /// Sub-scopes for specif plugins
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        ILifetimeScope Plugin<T>(ILifetimeScope execution, PluginOptions? options) where T : IPlugin;
    }
}
