#region License
// CommandLineParserErrorFormatter.cs
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

using System.Collections.Generic;
using System.Text;
using Fclp.Internals.Errors;
using Fclp.Internals.Extensions;

namespace Fclp
{
	/// <summary>
	/// A simple parser error formatter designed to create error descriptions suitable for the console.
	/// </summary>
	public class CommandLineParserErrorFormatter : ICommandLineParserErrorFormatter
	{

		/// <summary>
		/// Formats the specified list of <see cref="ICommandLineParserError"/> to a <see cref="System.String"/> suitable for the end user.
		/// </summary>
		/// <param name="parserErrors">The errors to format.</param>
		/// <returns>A <see cref="System.String"/> describing the specified errors.</returns>
		public string Format(IEnumerable<ICommandLineParserError> parserErrors)
		{
			if (parserErrors.IsNullOrEmpty()) return null;

			var builder = new StringBuilder();

			foreach (var error in parserErrors)
			{
				builder.AppendLine(Format(error));
			}

			return builder.ToString();
		}
	
		/// <summary>
		/// Formats the specified <see cref="ICommandLineParserError"/> to a <see cref="System.String"/> suitable for the end user.
		/// </summary>
		/// <param name="parserError">The error to format. This must not be null.</param>
		/// <returns>A <see cref="System.String"/> describing the specified error.</returns>
		public string Format(ICommandLineParserError parserError)
		{
			var optionSyntaxParseError = parserError as OptionSyntaxParseError;
			if (optionSyntaxParseError != null) return FormatOptionSyntaxParseError(optionSyntaxParseError);

			var expectedOptionNotFoundError = parserError as ExpectedOptionNotFoundParseError;
			if (expectedOptionNotFoundError != null) return FormatExpectedOptionNotFoundError(expectedOptionNotFoundError);

			return "unknown parse error.";
		}

		private static string FormatOptionSyntaxParseError(OptionSyntaxParseError error)
		{
			return string.Format("Option '{0}' parse error: could not parse '{1}' to '{2}'.",
			                     error.ParsedOption.RawKey,
								 error.ParsedOption.Value.RemoveAnyWrappingDoubleQuotes(),
			                     error.Option.SetupType);
		}

		private static string FormatExpectedOptionNotFoundError(ExpectedOptionNotFoundParseError error)
		{
			var optionText = GetOptionText(error);
			return string.Format("Option '{0}' parse error. option is required but was not specified.", optionText);
		}

		private static string GetOptionText(ICommandLineParserError error)
		{
			var optionText = error.Option.LongName.IsNullOrWhiteSpace()
				                 ? error.Option.ShortName
				                 : error.Option.ShortName + ":" + error.Option.LongName;
			return optionText;
		}
	}
}