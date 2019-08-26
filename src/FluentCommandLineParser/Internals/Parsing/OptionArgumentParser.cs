#region License
// OptionArgumentParser.cs
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
	/// 
	/// </summary>
	public class OptionArgumentParser
	{
	    private readonly SpecialCharacters _specialCharacters;

        /// <summary>
        /// Initialises a new instance of <see cref="OptionArgumentParser"/>.
        /// </summary>
        /// <param name="specialCharacters"></param>
        public OptionArgumentParser(SpecialCharacters specialCharacters)
	    {
	        _specialCharacters = specialCharacters;
	    }

	    /// <summary>
		/// Parses the values.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <param name="option">The option.</param>
		public void ParseArguments(IEnumerable<string> args, ParsedOption option)
		{
			if (option.Key != null && _specialCharacters.ValueAssignments.Any(option.Key.Contains))
			{
				TryGetArgumentFromKey(option);
			}

			var allArguments = new List<string>();
			var additionalArguments = new List<string>();

			var otherArguments = CollectArgumentsUntilNextKey(args).ToList();

			if (option.HasValue) allArguments.Add(option.Value);
			if (otherArguments.Any())
			{
				allArguments.AddRange(otherArguments);
                if (otherArguments.Count() > 1)
                {
                    additionalArguments.AddRange(otherArguments);
                    additionalArguments.RemoveAt(0);
                }
            }
            if (allArguments.Count > 1 && 
                allArguments.First().StartsWith("\"") && 
                allArguments.Last().EndsWith("\""))
            {
                option.Value = string.Join(" ", allArguments.ToArray());
            }
            else
            {
                option.Value = allArguments.FirstOrDefault();
            }
            option.Value = string.Join(" ", allArguments.ToArray());
			option.Values = allArguments.ToArray();
			option.AdditionalValues = additionalArguments.ToArray();
		}

		private void TryGetArgumentFromKey(ParsedOption option)
		{
			var split = option.Key.Split(_specialCharacters.ValueAssignments, 2, StringSplitOptions.RemoveEmptyEntries);

			option.Key = split[0];
			option.Value = split.Length > 1 
				               ? split[1].WrapInDoubleQuotesIfContainsWhitespace()
				               : null;
		}

	    private IEnumerable<string> CollectArgumentsUntilNextKey(IEnumerable<string> args)
		{
			return from argument in args
			       where !IsEndOfOptionsKey(argument)
			       select argument.WrapInDoubleQuotesIfContainsWhitespace();
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