using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins.Interfaces
{
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
        Target Default(IOptionsService optionService);

        /// <summary>
        /// Aquire a target interactively based on user input
        /// and choices
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Aquire(IOptionsService optionService, IInputService inputService);

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
