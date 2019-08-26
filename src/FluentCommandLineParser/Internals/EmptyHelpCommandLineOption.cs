#region License
// EmptyHelpCommandLineOption.cs
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
	/// Help command line options used when there have been non setup.
	/// </summary>
	public class EmptyHelpCommandLineOption : IHelpCommandLineOption
	{
		/// <summary>
		/// Always returns false.
		/// </summary>
		/// <param name="commandLineArgs">The command line args.</param>
		/// <param name="comparisonType">Type of the comparison.</param>
		/// <returns></returns>
		public bool ShouldShowHelp(IEnumerable<ParsedOption> commandLineArgs, StringComparison comparisonType)
		{
			return false;
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		public void ShowHelp(IEnumerable<ICommandLineOption> options)
		{
			throw new NotSupportedException();
		}
	}
}