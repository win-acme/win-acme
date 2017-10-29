using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    public interface ITargetPlugin : IHasName
    {
        /// <summary>
        /// Aquire the target non-interactively, useful for 
        /// unattended operation with all needed information
        /// provided either through the command line or 
        /// by some other means.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Default(IOptionsService options);

        /// <summary>
        /// Aquire a target interactively based on user input
        /// and choices
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Aquire(IOptionsService options, IInputService input);

        /// <summary>
        /// Update a target before renewing the certificate
        /// </summary>
        /// <param name="options"></param>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        Target Refresh(IOptionsService options, Target scheduled);

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
