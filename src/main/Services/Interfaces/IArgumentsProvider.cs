using Fclp;
using Fclp.Internals;
using System.Collections.Generic;

namespace PKISharp.WACS.Services.Interfaces
{
    public interface IArgumentsProvider
    {
        /// <summary>
        /// Name for this group of options
        /// </summary>
        string Name { get; }

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
    }

    public interface IArgumentsProvider<T> : IArgumentsProvider where T : new()
    {
        /// <summary>
        /// Get the parsed result
        /// </summary>
        new T GetResult(string[] args);
    } 
}
