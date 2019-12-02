using Fclp;
using Fclp.Internals;
using PKISharp.WACS.Configuration;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IArgumentsProvider
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
        string Condition { get; }

        /// <summary>
        /// Precondition to use these parameters
        /// </summary>
        bool Default { get; }

        /// <summary>
        /// Which options are available
        /// </summary>
        IEnumerable<ICommandLineOption> Configuration { get; }

        /// <summary>
        /// Feedback about the parsing
        /// </summary>
        ICommandLineParserResult GetParseResult(string[] args);

        /// <summary>
        /// Get the parsed result
        /// </summary>
        object GetResult(string[] args);

        /// <summary>
        /// Validate against the main arguments
        /// </summary>
        /// <param name="current"></param>
        /// <param name="main"></param>
        /// <returns></returns>
        bool Validate(ILogService log, object current, MainArguments main);

        /// <summary>
        /// Are the arguments provided?
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        bool Active(object current);
    }

    public interface IArgumentsProvider<T> : IArgumentsProvider where T : new()
    {
        /// <summary>
        /// Get the parsed result
        /// </summary>
        new T GetResult(string[] args);

        /// <summary>
        /// Validate against the main arguments
        /// </summary>
        /// <param name="current"></param>
        /// <param name="main"></param>
        /// <returns></returns>
        bool Validate(ILogService log, T current, MainArguments main);
    }
}
