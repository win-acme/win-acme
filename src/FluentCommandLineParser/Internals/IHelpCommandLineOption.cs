#region License
// IHelpCommandLineOption.cs
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
using Fclp.Internals.Parsing;

namespace Fclp.Internals
{
	/// <summary>
	/// Represents a command line option that determines whether to show the help text.
	/// </summary>
	public interface IHelpCommandLineOption
	{
		/// <summary>
		/// Determines whether the help text should be shown.
		/// </summary>
		/// <param name="parsedOptions">The parsed command line arguments</param>
		/// <param name="comparisonType">The type of comparison to use when comparing Option names.</param>
		/// <returns>true if the parser operation should cease and <see cref="ShowHelp"/> should be called; otherwise false if the parse operation to continue.</returns>
		bool ShouldShowHelp(IEnumerable<ParsedOption> parsedOptions, StringComparison comparisonType);

		/// <summary>
		/// Shows the help text for the specified registered options.
		/// </summary>
		/// <param name="options">The options to generate the help text for.</param>
		void ShowHelp(IEnumerable<ICommandLineOption> options);
	}
}