#region License
// CommandLineOptionFactory.cs
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
using Fclp.Internals.Parsing;
using Fclp.Internals.Parsing.OptionParsers;

namespace Fclp.Internals
{
	/// <summary>
	/// Factory used to create command line Options
	/// </summary>
	public class CommandLineOptionFactory : ICommandLineOptionFactory
	{

		ICommandLineOptionParserFactory _parserFactory;

		/// <summary>
		/// Gets or sets the <see cref="ICommandLineOptionParserFactory"/> to use.
		/// </summary>
		/// <remarks>If <c>null</c> a new instance of the <see cref="ParserFactory"/> will be returned.</remarks>
		public ICommandLineOptionParserFactory ParserFactory
		{
			get { return _parserFactory ?? (_parserFactory = new CommandLineOptionParserFactory()); }
			set { _parserFactory = value; }
		}

		/// <summary>
		/// Creates a new <see cref="ICommandLineOptionFluent{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of <see cref="ICommandLineOptionResult{T}"/> to create.</typeparam>
		/// <param name="shortName">The short name for this Option. This must not be <c>null</c>, <c>empty</c> or contain only <c>whitespace</c>.</param>
		/// <param name="longName">The long name for this Option or <c>null</c> if not required.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="shortName"/> is <c>null</c>, <c>empty</c> or contains only <c>whitespace</c>.</exception>
		/// <returns>A <see cref="ICommandLineOptionResult{T}"/>.</returns>
		public ICommandLineOptionResult<T> CreateOption<T>(string shortName, string longName)
		{
			return new CommandLineOption<T>(shortName, longName, this.ParserFactory.CreateParser<T>());
		}

		/// <summary>
		/// Create a new <see cref="IHelpCommandLineOptionResult"/> using the specified args.
		/// </summary>
		/// <param name="helpArgs">The args used to display the help option.</param>
		public IHelpCommandLineOptionResult CreateHelpOption(string[] helpArgs)
		{
			return new HelpCommandLineOption(helpArgs);
		}
	}
}
