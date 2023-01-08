using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins;
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
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel);

        /// <summary>
        /// For different targets split up by the order plugin
        /// </summary>
        /// <param name="main"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        ILifetimeScope Order(ILifetimeScope execution, Order order);

        /// <summary>
        /// Fake scope to check validation availability
        /// </summary>
        /// <param name="main"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        ILifetimeScope Target(ILifetimeScope execution, Target target);

        /// <summary>
        /// Sub-scopes for specific plugins
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        ILifetimeScope PluginBackend<TBackend, TCapability, TOptions>(ILifetimeScope execution, TOptions options, string key = "default")
            where TBackend : IPlugin
            where TCapability : IPluginCapability
            where TOptions : PluginOptions;

        /// <summary>
        /// Plugin selection/configuration methods
        /// </summary>
        /// <typeparam name="TOptionsFactory"></typeparam>
        /// <typeparam name="TCapability"></typeparam>
        /// <param name="execution"></param>
        /// <param name="plugin"></param>
        /// <returns></returns>
        ILifetimeScope PluginFrontend<TCapability, TOptions>(ILifetimeScope execution, Plugin plugin)
            where TCapability : IPluginCapability
            where TOptions : PluginOptions, new();
    }
}
