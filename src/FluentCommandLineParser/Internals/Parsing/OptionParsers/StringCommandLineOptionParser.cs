#region License
// StringCommandLineOptionParser.cs
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

using System.Linq;
using Fclp.Internals.Extensions;

namespace Fclp.Internals.Parsing.OptionParsers
{
	/// <summary>
	/// Parser used to convert to <see cref="System.String"/>.
	/// </summary>
	public class StringCommandLineOptionParser : ICommandLineOptionParser<string>
	{
		/// <summary>
		/// Parses the specified <see cref="System.String"/> into a <see cref="System.String"/>.
		/// </summary>
		/// <param name="parsedOption"></param>
		/// <returns></returns>
		public string Parse(ParsedOption parsedOption)
		{
            if (parsedOption.Value == null)
            {
                return null;
            }
            if (parsedOption.Values.Count() > 1)
            {
                return parsedOption.Value.RemoveAnyWrappingDoubleQuotes();
            }
            return parsedOption.Value;
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>.
		/// </summary>
		/// <param name="parsedOption"></param>
		/// <returns><c>true</c> if the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>; otherwise <c>false</c>.</returns>
		public bool CanParse(ParsedOption parsedOption)
		{
            if (parsedOption.Value.IsNullOrWhiteSpace()) return true;
            if (parsedOption.HasValue == false) return true;

            string value = (parsedOption.Value??"").Trim();

			var items = value.SplitOnWhitespace();

			return items.Count() == 1 || (value.StartsWith("\"") && value.EndsWith("\""));
		}
	}
}