#region License
// HelpCommandLineOption.cs
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
using Fclp.Internals.Parsing;

namespace Fclp.Internals
{
	/// <summary>
	/// Represents a command line option that determines whether to show the help text.
	/// </summary>
	public class HelpCommandLineOption : IHelpCommandLineOptionResult
	{
		ICommandLineOptionFormatter _optionFormatter;

		/// <summary>
		/// Initialises a new instance of <see cref="HelpCommandLineOption"/> class.
		/// </summary>
		/// <param name="helpArgs">The registered help arguments.</param>
		public HelpCommandLineOption(IEnumerable<string> helpArgs)
		{
			HelpArgs = helpArgs ?? new List<string>();
		}

		/// <summary>
		/// Gets the registered help arguments.
		/// </summary>
		public IEnumerable<string> HelpArgs { get; private set; }

		/// <summary>
		/// Gets or sets the callback method.
		/// </summary>
		internal Action<string> ReturnCallback { get; set; }

		private Action ReturnCallbackWithoutParser { get; set; }

		private bool ShouldUseForEmptyArgs { get; set; }

		/// <summary>
		/// Gets or sets any header to display at the top of the printed options.
		/// </summary>
		public string Header { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="ICommandLineOptionFormatter"/> to use to format the options.
		/// </summary>
		public ICommandLineOptionFormatter OptionFormatter
		{
			get { return _optionFormatter ?? (_optionFormatter = new CommandLineOptionFormatter { Header = this.Header }); }
			set { _optionFormatter = value; }
		}

		/// <summary>
		/// Specifies the method to invoke with the formatted command line options when any of the setup 
		/// help arguments are found. If a callback is not required either do not call it, or specify <c>null</c>.
		/// </summary>
		/// <param name="callback">
		/// The callback to execute with the formatted command line options. 
		/// </param>
		/// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
		public IHelpCommandLineOptionFluent Callback(Action<string> callback)
		{
			ReturnCallback = callback;
			return this;
		}

		/// <summary>
		/// Specified the method to invoke when any of the setup help arguments are found. If a callback is not required
		/// either do not call it, or specified <c>null</c>.
		/// </summary>
		/// <param name="callback">
		/// The callback to execute. If you have also setup the other help callback this will be called last.
		/// </param>
		/// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
		public IHelpCommandLineOptionFluent Callback(Action callback)
		{
			ReturnCallbackWithoutParser = callback;
			return this;
		}

		/// <summary>
		/// Registers a custom <see cref="ICommandLineOptionFormatter"/> to use to generate the help text.
		/// </summary>
		/// <param name="formatter">The custom formatter to use. This must not be <c>null</c>.</param>
		public IHelpCommandLineOptionFluent WithCustomFormatter(ICommandLineOptionFormatter formatter)
		{
			this.OptionFormatter = formatter;
			return this;
		}


		/// <summary>
		/// Provides a custom header to be printed before the registered options.
		/// </summary>
		/// <param name="header">The header to use.</param>
		public IHelpCommandLineOptionFluent WithHeader(string header)
		{
			this.Header = header;
			return this;
		}

		/// <summary>
		/// Specifies that if empty arguments are found then the behaviour should be the same as when any help arguments
		/// are found.
		/// </summary>
		/// <returns>A <see cref="IHelpCommandLineOptionFluent"/>.</returns>
		public IHelpCommandLineOptionFluent UseForEmptyArgs()
		{
			this.ShouldUseForEmptyArgs = true;
			return this;
		}

		/// <summary>
		/// Determines whether the help text should be shown.
		/// </summary>
		/// <param name="parsedOptions">The parsed command line arguments</param>
		/// <param name="comparisonType">The type of comparison to use when comparing Option names.</param>
		/// <returns>
		/// true if the parser operation should cease and <see cref="ShowHelp" /> should be called; otherwise false if the parse operation to continue.
		/// </returns>
		public bool ShouldShowHelp(IEnumerable<ParsedOption> parsedOptions, StringComparison comparisonType)
		{
			var parsed = parsedOptions != null ? parsedOptions.ToList() : new List<ParsedOption>();

			if (parsed.Any() == false && ShouldUseForEmptyArgs)
			{
				return true;
			}

			return this.HelpArgs.Any(helpArg => parsed.Any(cmdArg => helpArg.Equals(cmdArg.Key, comparisonType)));
		}

		/// <summary>
		/// Shows the help text for the specified registered options.
		/// </summary>
		/// <param name="options">The options to generate the help text for.</param>
		public void ShowHelp(IEnumerable<ICommandLineOption> options)
		{
			if (ReturnCallback != null)
			{
				var formattedOutput = this.OptionFormatter.Format(options);
				this.ReturnCallback(formattedOutput);    
			}

			if (ReturnCallbackWithoutParser != null)
			{
				this.ReturnCallbackWithoutParser();
			}
		}
	}
}