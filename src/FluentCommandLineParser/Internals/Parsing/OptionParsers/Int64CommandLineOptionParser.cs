using System.Globalization;

namespace Fclp.Internals.Parsing.OptionParsers
{
    /// <summary>
    /// Parser used to convert to <see cref="System.Int64"/>.
    /// </summary>
    public class Int64CommandLineOptionParser : ICommandLineOptionParser<long>
    {
        /// <summary>
        /// Converts the string representation of a number in a specified culture-specific format to its 64-bit signed integer equivalent.
        /// </summary>
        /// <param name="parsedOption"></param>
        /// <returns></returns>
        public long Parse(ParsedOption parsedOption)
        {
            return long.Parse(parsedOption.Value, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>.
        /// </summary>
        /// <param name="parsedOption"></param>
        /// <returns><c>true</c> if the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>; otherwise <c>false</c>.</returns>
        public bool CanParse(ParsedOption parsedOption)
        {
            long result;
            return long.TryParse(parsedOption.Value, out result);
        }
    }
}
