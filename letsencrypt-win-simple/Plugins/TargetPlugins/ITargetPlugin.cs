using System;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    public interface ITargetPluginFactory : IHasName
    {
        /// <summary>
        /// Which type is used as instance
        /// </summary>
        Type Instance { get; }
    }

    class NullTargetFactory : ITargetPluginFactory, IIsNull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type ITargetPluginFactory.Instance => typeof(object);
    }

    public interface ITargetPlugin
    {
        /// <summary>
        /// Aquire the target non-interactively, useful for 
        /// unattended operation with all needed information
        /// provided either through the command line or 
        /// by some other means.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Default();

        /// <summary>
        /// Aquire a target interactively based on user input
        /// and choices
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Aquire();

        /// <summary>
        /// Update a target before renewing the certificate
        /// </summary>
        /// <param name="options"></param>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        Target Refresh(Target scheduled);

        /// <summary>
        /// Split a single scheduled target into multiple actual targets
        /// this exists to replicate the behaviour of the old IISSiteServer plugin
        /// </summary>
        /// <param name="options"></param>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        IEnumerable<Target> Split(Target scheduled);
    }
}
