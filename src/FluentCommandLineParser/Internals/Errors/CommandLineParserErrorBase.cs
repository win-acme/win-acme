#region License
// CommandLineParserErrorBase.cs
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

namespace Fclp.Internals.Errors
{
	/// <summary>
	/// Contains error information regarding a failed parsing of a Option.
	/// </summary>
	public abstract class CommandLineParserErrorBase : ICommandLineParserError
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="CommandLineParserErrorBase"/> class.
		/// </summary>
		/// <param name="cmdOption">The <see cref="ICommandLineOption"/> this error relates too. This must not be <c>null</c>.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="cmdOption"/> is <c>null</c>.</exception>
		protected CommandLineParserErrorBase(ICommandLineOption cmdOption)
		{
			if (cmdOption == null) throw new ArgumentNullException("cmdOption");
			this.Option = cmdOption;
		}

		/// <summary>
		/// Gets the <see cref="ICommandLineOption"/> this error belongs too.
		/// </summary>
		public virtual ICommandLineOption Option { get; private set; }
	}
}