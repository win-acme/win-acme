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
        /// Prepare single challenge
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        Task PrepareChallenge(ValidationContext context);

        /// <summary>
        /// Commit changes after all the challenges have been prepared
        /// </summary>
        /// <returns></returns>
        Task Commit();

        /// <summary>
        /// Clean up after validation attempt
        /// </summary>
        Task CleanUp();

        /// <summary>
        /// Indicate level of supported parallelism
        /// </summary>
        ParallelOperations Parallelism { get; }
    }

    [Flags]
    public enum ParallelOperations
    {
        /// <summary>
        /// Nothing can be done in parallel
        /// </summary>
        None = 0,

        /// <summary>
        /// Prepare for multiple challenge answers in parallel
        /// </summary>
        Prepare = 1,

        /// <summary>
        /// Answer multiple challenges in parallel 
        /// </summary>
        Answer = 2,

        /// <summary>
        /// Reuse a single plugin instance for multiple *serial* validations.
        /// Plugin should properly maintain internal state during multiple
        /// prepare/answer/cleanup cycles.
        /// </summary>
        Reuse = 4
    }
}
