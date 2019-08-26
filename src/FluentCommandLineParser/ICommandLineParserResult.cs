#region License
// ICommandLineParserResult.cs
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
using Fclp.Internals;
using Fclp.Internals.Parsing;

namespace Fclp
{
	/// <summary>
	/// Represents all the information gained from the result of a parse operation.
	/// </summary>
	public interface ICommandLineParserResult
	{
		/// <summary>
		/// Gets whether the parse operation encountered any errors.
		/// </summary>
		bool HasErrors { get; }

		/// <summary>
		/// Gets whether the help text was called.
		/// </summary>
		bool HelpCalled { get; }

		/// <summary>
		/// Gets whether the parser was called with empty arguments.
		/// </summary>
		bool EmptyArgs { get; }

		/// <summary>
		/// Gets any formatted error for this result.
		/// </summary>
		string ErrorText { get; }

		/// <summary>
		/// Gets the errors which occurred during the parse operation.
		/// </summary>
		IEnumerable<ICommandLineParserError> Errors { get; }

		/// <summary>
		/// Contains a list of options that were specified in the args but not setup and therefore were not expected.
		/// </summary>
		IEnumerable<KeyValuePair<string, string>> AdditionalOptionsFound { get; }

		/// <summary>
		/// Contains all the setup options that were not matched during the parse operation.
		/// </summary>
		IEnumerable<ICommandLineOption> UnMatchedOptions { get; }

        /// <summary>
        /// 
        /// </summary>
        ParserEngineResult RawResult { get; set; }
    }
}
