using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    interface IHasName {
        /// <summary>
        /// Unique identifier
        /// </summary>
        string Name { get; }
    }

    interface ITargetPlugin : IHasName
    {
        /// <summary>
        /// Short description of the plugin
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Aquire the target non-interactively, useful for 
        /// unattended operation with all needed information
        /// provided either through the command line or 
        /// by some other means.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Default(Options options);

        /// <summary>
        /// Aquire a target interactively based on user input
        /// and choices
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Target Aquire(Options options);

        /// <summary>
        /// Update a target before renewing the certificate
        /// </summary>
        /// <param name="options"></param>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        Target Refresh(Options options, Target scheduled);
    }
}
