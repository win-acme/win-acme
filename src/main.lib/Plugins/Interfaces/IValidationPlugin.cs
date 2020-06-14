using PKISharp.WACS.Context;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Instance interface
    /// </summary>
    public interface IValidationPlugin : IPlugin
    {
        /// <summary>
        /// Prepare challenge
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        Task PrepareChallenge(ValidationContext context);

        /// <summary>
        /// Clean up after validation attempt
        /// </summary>
        Task CleanUp(ValidationContext context);

        /// <summary>
        /// Indicate level of supported parallelism
        /// </summary>
        ParallelOperations Parallelism { get; }
    }

    [Flags]
    public enum ParallelOperations
    {
        None = 0,
        Prepare = 1,
        Answer = 2,
        Clean = 4
    }
}
