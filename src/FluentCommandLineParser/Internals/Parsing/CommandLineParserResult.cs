#region License
// CommandLineParserResult.cs
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
using System.Linq;

namespace Fclp.Internals.Parsing
{
	/// <summary>
	/// Contains all information about the result of a parse operation.
	/// </summary>
	public class CommandLineParserResult : ICommandLineParserResult
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="CommandLineParserResult"/> class.
		/// </summary>
		public CommandLineParserResult()
		{
			this.Errors = new List<ICommandLineParserError>();
			this.AdditionalOptionsFound = new List<KeyValuePair<string,string>>();
			this.UnMatchedOptions = new List<ICommandLineOption>();
		}

		/// <summary>
		/// Gets whether the parse operation encountered any errors or the help text was shown.
		/// </summary>
		public bool HasErrors
		{
			get { return this.Errors.Any(); }
		}

		/// <summary>
		/// 
		/// </summary>
		internal IList<ICommandLineParserError> Errors { get; set; }

		/// <summary>
		/// Gets the errors which occurred during the parse operation.
		/// </summary>
		IEnumerable<ICommandLineParserError> ICommandLineParserResult.Errors
		{
			get { return this.Errors; }
		}

		/// <summary>
		/// Contains a list of options that were specified in the args but not setup and therefore were not expected.
		/// </summary>
		IEnumerable<KeyValuePair<string, string>> ICommandLineParserResult.AdditionalOptionsFound
		{
			get { return this.AdditionalOptionsFound; }
		}

		/// <summary>
		/// Contains a list of options that were specified in the args but not setup and therefore were not expected.
		/// </summary>
		public IList<KeyValuePair<string, string>> AdditionalOptionsFound { get; set; }

		/// <summary>
		/// Contains all the setup options that were not matched during the parse operation.
		/// </summary>
		IEnumerable<ICommandLineOption> ICommandLineParserResult.UnMatchedOptions
		{
			get { return this.UnMatchedOptions; }
		}

        /// <summary>
        /// 
        /// </summary>
	    public ParserEngineResult RawResult { get; set; }

	    /// <summary>
		/// Contains all the setup options that were not matched during the parse operation.
		/// </summary>
		public IList<ICommandLineOption> UnMatchedOptions { get; set; }

		/// <summary>
		/// Gets whether the help text was called.
		/// </summary>
		public bool HelpCalled { get; set; }

		/// <summary>
		/// Gets whether the parser was called with empty arguments.
		/// </summary>
		public bool EmptyArgs { get; set; }

		/// <summary>
		/// Gets or sets the formatted error for this result.
		/// </summary>
		public string ErrorText { get; set; }
	}
}