#region License
// CommandLineParserEngineMark2.cs
// Copyright (c) 2013, Simon Williams
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provide
// d that the following conditions are met:
// 
// Redistributions of source code must retain the above copyright notice, this list of conditions and the
// following disclaimer.
// 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
// the following disclaimer in the documentation and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED 
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Fclp.Internals.Extensions;

namespace Fclp.Internals.Parsing
{
	/// <summary>
	/// More advanced parser for transforming command line arguments into appropriate <see cref="ParsedOption"/>.
	/// </summary>
	public class CommandLineParserEngineMark2 : ICommandLineParserEngine
	{
	    private readonly SpecialCharacters _specialCharacters;
	    private readonly List<string> _additionalArgumentsFound = new List<string>();
		private readonly List<ParsedOption> _parsedOptions = new List<ParsedOption>();
	    private readonly OptionArgumentParser _optionArgumentParser;

        /// <summary>
        /// Initialises a new instance of <see cref="CommandLineParserEngineMark2"/>.
        /// </summary>
        /// <param name="specialCharacters"></param>
        public CommandLineParserEngineMark2(SpecialCharacters specialCharacters)
	    {
	        _specialCharacters = specialCharacters;
	        _optionArgumentParser = new OptionArgumentParser(specialCharacters);
        }

	    /// <summary>
        /// Parses the specified <see><cref>T:System.String[]</cref></see> into key value pairs.
        /// </summary>
        /// <param name="args">The <see><cref>T:System.String[]</cref></see> to parse.</param>
        /// <param name="parseCommands">true to parse any commands, false to skip commands.</param>
        /// <returns>An <see cref="ICommandLineParserResult"/> representing the results of the parse operation.</returns>
        public ParserEngineResult Parse(string[] args, bool parseCommands)
		{
			args = args ?? new string[0];

			var grouper = new CommandLineOptionGrouper(_specialCharacters);

            var grouped = grouper.GroupArgumentsByOption(args, parseCommands);

            string command = parseCommands ? ExtractAnyCommand(grouped) : null;

            foreach (var optionGroup in grouped)
			{
				string rawKey = optionGroup.First();
				ParseGroupIntoOption(rawKey, optionGroup.Skip(1));
			}

            if (command != null)
            {
                _additionalArgumentsFound.RemoveAt(0);
            }

			return new ParserEngineResult(_parsedOptions, _additionalArgumentsFound, command);
		}

	    private string ExtractAnyCommand(string[][] grouped)
	    {
	        if (grouped.Length > 0)
	        {
	            var cmdGroup = grouped.First();

	            var cmd = cmdGroup.FirstOrDefault();

	            if (IsAKey(cmd) == false)
	            {
	                return cmd;
	            }
	        }

	        return null;
	    }


	    private void ParseGroupIntoOption(string rawKey, IEnumerable<string> optionGroup)
		{
			if (IsAKey(rawKey))
			{
				var parsedOption = new ParsedOptionFactory(_specialCharacters).Create(rawKey);

				TrimSuffix(parsedOption);

				_optionArgumentParser.ParseArguments(optionGroup, parsedOption);

				AddParsedOptionToList(parsedOption);
			}
			else
			{
				AddAdditionArgument(rawKey);
				optionGroup.ForEach(AddAdditionArgument);
			}
		}

		private void AddParsedOptionToList(ParsedOption parsedOption)
		{
			if (ShortOptionNeedsToBeSplit(parsedOption))
			{
				_parsedOptions.AddRange(CloneAndSplit(parsedOption));
			}
			else
			{
				_parsedOptions.Add(parsedOption);
			}
		}

		private void AddAdditionArgument(string argument)
		{
			if (IsEndOfOptionsKey(argument) == false)
			{
				_additionalArgumentsFound.Add(argument);
			}
		}

		private bool ShortOptionNeedsToBeSplit(ParsedOption parsedOption)
		{
			return PrefixIsShortOption(parsedOption.Prefix) && parsedOption.Key.Length > 1;
		}

		private static IEnumerable<ParsedOption> CloneAndSplit(ParsedOption parsedOption)
		{
			return parsedOption.Key.Select(c => Clone(parsedOption, c)).ToList();
		}

		private static ParsedOption Clone(ParsedOption toClone, char c)
		{
			var clone = toClone.Clone();
			clone.Key = new string(new[] { c });
			return clone;
		}

		private bool PrefixIsShortOption(string key)
		{
			return _specialCharacters.ShortOptionPrefix.Contains(key);
		}

		private static void TrimSuffix(ParsedOption parsedOption)
		{
			if (parsedOption.HasSuffix)
			{
				parsedOption.Key = parsedOption.Key.TrimEnd(parsedOption.Suffix.ToCharArray());
			}
		}

		/// <summary>
		/// Gets whether the specified <see cref="System.String"/> is a Option key.
		/// </summary>
		/// <param name="arg">The <see cref="System.String"/> to examine.</param>
		/// <returns><c>true</c> if <paramref name="arg"/> is a Option key; otherwise <c>false</c>.</returns>
		private bool IsAKey(string arg)
		{ // TODO: push related special char operations into there own object
			return arg != null 
				&& _specialCharacters.OptionPrefix.Any(arg.StartsWith)
				&& _specialCharacters.OptionPrefix.Any(arg.Equals) == false;
		}

		/// <summary>
		/// Determines whether the specified string indicates the end of parsed options.
		/// </summary>
		private bool IsEndOfOptionsKey(string arg)
		{
			return string.Equals(arg, _specialCharacters.EndOfOptionsKey, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}