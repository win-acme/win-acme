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
        Target Default(OptionsService options);

        /// <summary>
        /// Aquire a target interactively based on user input
        /// and choices
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Aquire(OptionsService options, InputService input);

        /// <summary>
        /// Update a target before renewing the certificate
        /// </summary>
        /// <param name="options"></param>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        Target Refresh(OptionsService options, Target scheduled);
    }
}
