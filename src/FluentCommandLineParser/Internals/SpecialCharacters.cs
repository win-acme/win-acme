#region License
// SpecialCharacters.cs
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

namespace Fclp.Internals
{
	/// <summary>
	/// Contains special characters used throughout the parser.
	/// </summary>
	public class SpecialCharacters
	{
		/// <summary>
		/// Characters used for value assignment.
		/// </summary>
		public char[] ValueAssignments { get; private set; } = new[] { '=', ':' };

		/// <summary>
		/// Assign a name to the whitespace character.
		/// </summary>
		public char Whitespace { get; set; } = ' ';

		/// <summary>
		/// Characters that define the start of an option.
		/// </summary>
		public List<string> OptionPrefix { get; private set; } = new List<string> { "/", "--", "-" };

		/// <summary>
		/// Characters that have special meaning at the end of an option key.
		/// </summary>
		public List<string> OptionSuffix { get; private set; } = new List<string> { "+", "-" };

		/// <summary>s
		/// Characters that define an explicit short option.
		/// </summary>
		public List<string> ShortOptionPrefix { get; private set; } = new List<string> { "-" };

		/// <summary>
		/// The key that indicates the end of any options.
		/// Any following arguments should be treated as operands, even if they begin with the '-' character.
		/// </summary>
		public string EndOfOptionsKey { get; set; } = "--";
	}
}
