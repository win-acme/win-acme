using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IArguments
    {
        /// <summary>
        /// Name for this group of options
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Group (e.g. Target, Validation, Store)
        /// </summary>
        string Group { get; }

        /// <summary>
        /// Precondition to use these parameters
        /// </summary>
        string? Condition { get; }

        /// <summary>
        /// Precondition to use these parameters
        /// </summary>
        bool Default { get; }
    }

    public interface IArgumentsStandalone : IArguments
    {
        /// <summary>
        /// Validate against the main arguments
        /// </summary>
        /// <param name="current"></param>
        /// <param name="main"></param>
        /// <returns></returns>
        bool Validate(MainArguments main, ILogService log);

        /// <summary>
        /// Are the arguments provided?
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        bool Active();
    }

    public interface IArgumentsProvider : IArguments
    {
        /// <summary>
        /// Reference to the logging service
        /// </summary>
        ILogService? Log { get; set; }

        /// <summary>
        /// Which options are available
        /// </summary>
        IEnumerable<CommandLineAttribute> Configuration { get; }

        /// <summary>
        /// Feedback about the parsing
        /// </summary>
        IEnumerable<string> GetExtraArguments(string[] args);

        /// <summary>
        /// Get the parsed result
        /// </summary>
        object? GetResult(string[] args);

        /// <summary>
        /// Validate against the main arguments
        /// </summary>
        /// <param name="current"></param>
        /// <param name="main"></param>
        /// <returns></returns>
        bool Validate(object current, MainArguments main);

        /// <summary>
        /// Are the arguments provided?
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        bool Active(object current);
    }

    public interface IArgumentsProvider<T> : IArgumentsProvider where T : class, new()
    {
        /// <summary>
        /// Get the parsed result
        /// </summary>
        new T? GetResult(string[] args);

        /// <summary>
        /// Validate against the main arguments
        /// </summary>
        /// <param name="current"></param>
        /// <param name="main"></param>
        /// <returns></returns>
        bool Validate(T current, MainArguments main);
    }
}
