#region License
// FluentCommandLineParser.cs
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
using System.Globalization;
using System.Linq;
using Fclp.Internals;
using Fclp.Internals.Errors;
using Fclp.Internals.Extensions;
using Fclp.Internals.Parsing;
using Fclp.Internals.Validators;

namespace Fclp
{
	/// <summary>
	/// A command line parser which provides methods and properties 
	/// to easily and fluently parse command line arguments. 
	/// </summary>
	public class FluentCommandLineParser : IFluentCommandLineParser
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="FluentCommandLineParser"/> class.
		/// </summary>
		public FluentCommandLineParser()
		{
			IsCaseSensitive = true;
		}

		/// <summary>
		/// The <see cref="StringComparison"/> type used for case sensitive comparisons.
		/// </summary>
		public const StringComparison CaseSensitiveComparison = StringComparison.CurrentCulture;

		/// <summary>
		/// The <see cref="StringComparison"/> type used for case in-sensitive comparisons.
		/// </summary>
		public const StringComparison IgnoreCaseComparison = StringComparison.CurrentCultureIgnoreCase;

		List<ICommandLineOption> _options;
        List<ICommandLineCommand> _commands;
		ICommandLineOptionFactory _optionFactory;
		ICommandLineParserEngine _parserEngine;
		ICommandLineOptionFormatter _optionFormatter;
		IHelpCommandLineOption _helpOption;
		ICommandLineParserErrorFormatter _errorFormatter;
		ICommandLineOptionValidator _optionValidator;
	    SpecialCharacters _specialCharacters;

	    /// <summary>
		/// Gets or sets whether values that differ by case are considered different. 
		/// </summary>
		public bool IsCaseSensitive
		{
			get { return StringComparison == CaseSensitiveComparison; }
			set { StringComparison = value ? CaseSensitiveComparison : IgnoreCaseComparison; }
		}

	    /// <summary>
	    /// Configures the <see cref="IFluentCommandLineParser"/> so that short and long options that differ by case are considered the same.
	    /// </summary>
	    /// <returns></returns>
        public IFluentCommandLineParser MakeCaseInsensitive()
	    {
	        IsCaseSensitive = false;
	        return this;
	    }

	    /// <summary>
	    /// Configures the <see cref="IFluentCommandLineParser"/> so that short options are treated the same as long options, thus
	    /// unique short option behaviour is ignored.
	    /// </summary>
	    /// <returns></returns>
        public IFluentCommandLineParser DisableShortOptions()
	    {
            SpecialCharacters.ShortOptionPrefix.Clear();
	        return this;
	    }

	    /// <summary>
	    /// Configures the <see cref="IFluentCommandLineParser"/> to use the specified option prefixes instead of the default.
	    /// </summary>
	    /// <param name="prefix"></param>
	    /// <returns></returns>
        public IFluentCommandLineParser UseOwnOptionPrefix(params string[] prefix)
	    {
	        SpecialCharacters.OptionPrefix.Clear();
	        SpecialCharacters.OptionPrefix.AddRange(prefix);
	        return this;
	    }

	    /// <summary>
	    /// Configures the <see cref="IFluentCommandLineParser"/> to skip the first of the specified arguments.
	    /// This can be useful when Windows inserts the application name in the command line arguments for your application.
	    /// </summary>
	    /// <returns>this <see cref="IFluentCommandLineParser"/></returns>
        public IFluentCommandLineParser SkipFirstArg()
	    {
	        SkipTheFirstArg = true;
	        return this;
	    }

	    /// <summary>
		/// Gets the <see cref="StringComparison"/> to use when matching values.
		/// </summary>
		internal StringComparison StringComparison { get; private set; }

        /// <summary>
        /// Gets or sets whether to skip the first of the specified arguments.
        /// This can be useful when Windows inserts the application name in the command line arguments for your application.
        /// </summary>
        /// <returns><c>true</c> if the first arg is to be ignored; otherwise <c>false</c>.</returns>
        internal bool SkipTheFirstArg { get; set; }

		/// <summary>
		/// Gets the list of Options
		/// </summary>
		public List<ICommandLineOption> Options
		{
			get { return _options ?? (_options = new List<ICommandLineOption>()); }
		}

        /// <summary>
        /// Gets the list of Commands
        /// </summary>
	    public List<ICommandLineCommand> Commands
	    {
            get { return _commands ?? (_commands = new List<ICommandLineCommand>()); }
	    }

        /// <summary>
        /// options/callback Execute sequence, default as the setup sequence
        /// </summary>
        public ParseSequence ParseSequence { get; set; }

		/// <summary>
		/// Gets or sets the default option formatter.
		/// </summary>
		public ICommandLineOptionFormatter OptionFormatter
		{
			get { return _optionFormatter ?? (_optionFormatter = new CommandLineOptionFormatter()); }
			set { _optionFormatter = value; }
		}

		/// <summary>
		/// Gets or sets the default option formatter.
		/// </summary>
		public ICommandLineParserErrorFormatter ErrorFormatter
		{
			get { return _errorFormatter ?? (_errorFormatter = new CommandLineParserErrorFormatter()); }
			set { _errorFormatter = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="ICommandLineOptionFactory"/> to use for creating <see cref="ICommandLineOptionFluent{T}"/>.
		/// </summary>
		/// <remarks>If this property is set to <c>null</c> then the default <see cref="OptionFactory"/> is returned.</remarks>
		public ICommandLineOptionFactory OptionFactory
		{
			get { return _optionFactory ?? (_optionFactory = new CommandLineOptionFactory()); }
			set { _optionFactory = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="ICommandLineOptionValidator"/> used to validate each setup Option.
		/// </summary>
		public ICommandLineOptionValidator OptionValidator
		{
			get { return _optionValidator ?? (_optionValidator = new CommandLineOptionValidator(this, SpecialCharacters)); }
			set { _optionValidator = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="ICommandLineParserEngine"/> to use for parsing the command line args.
		/// </summary>
		public ICommandLineParserEngine ParserEngine
		{
			get { return _parserEngine ?? (_parserEngine = new CommandLineParserEngineMark2(SpecialCharacters)); }
			set { _parserEngine = value; }
		}

        /// <summary>
        /// Gets or sets the option used for when help is detected in the command line args.
        /// </summary>
        public IHelpCommandLineOption HelpOption
		{
			get { return _helpOption ?? (_helpOption = new EmptyHelpCommandLineOption()); }
			set { _helpOption = value; }
		}

        /// <summary>
        /// Gets whether commands have been setup for this parser.
        /// </summary>
	    public bool HasCommands
	    {
            get { return Commands.Any(); }
	    }

        /// <summary>
        /// Gets the special characters used by the parser.
        /// </summary>
	    public SpecialCharacters SpecialCharacters
	    {
	        get { return _specialCharacters ?? (_specialCharacters = new SpecialCharacters()); }
	    }

        /// <summary>
        /// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified short and long Option name.
        /// </summary>
        /// <param name="shortOption">The short name for the Option. This must not be <c>null</c>, <c>empty</c> or only <c>whitespace</c>.</param>
        /// <param name="longOption">The long name for the Option or <c>null</c> if not required.</param>
        /// <returns></returns>
        /// <exception cref="OptionAlreadyExistsException">
        /// A Option with the same <paramref name="shortOption"/> name or <paramref name="longOption"/> name
        /// already exists in the <see cref="IFluentCommandLineParser"/>.
        /// </exception>
        public ICommandLineOptionFluent<T> Setup<T>(char shortOption, string longOption)
		{
			return SetupInternal<T>(shortOption.ToString(CultureInfo.InvariantCulture), longOption);
		}

		/// <summary>
		/// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified short and long Option name.
		/// </summary>
		/// <param name="shortOption">The short name for the Option. This must not be <c>whitespace</c> or a control character.</param>
		/// <param name="longOption">The long name for the Option. This must not be <c>null</c>, <c>empty</c> or only <c>whitespace</c>.</param>
		/// <returns></returns>
		/// <exception cref="OptionAlreadyExistsException">
		/// A Option with the same <paramref name="shortOption"/> name or <paramref name="longOption"/> name already exists in the <see cref="IFluentCommandLineParser"/>.
		/// </exception>
		/// <exception cref="InvalidOptionNameException">
		/// Either <paramref name="shortOption"/> or <paramref name="longOption"/> are not valid. <paramref name="shortOption"/> must not be <c>whitespace</c>
		/// or a control character. <paramref name="longOption"/> must not be <c>null</c>, <c>empty</c> or only <c>whitespace</c>.
		/// </exception>
		[Obsolete("Use new overload Setup<T>(char, string) to specify both a short and long option name instead.")]
		public ICommandLineOptionFluent<T> Setup<T>(string shortOption, string longOption)
		{
			return SetupInternal<T>(shortOption, longOption);
		}

		private ICommandLineOptionFluent<T> SetupInternal<T>(string shortOption, string longOption)
		{
			var argOption = this.OptionFactory.CreateOption<T>(shortOption, longOption);

			if (argOption == null)
				throw new InvalidOperationException("OptionFactory is producing unexpected results.");

			OptionValidator.Validate(argOption, IsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);

			this.Options.Add(argOption);

			return argOption;
		}

		/// <summary>
		/// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified short Option name.
		/// </summary>
		/// <param name="shortOption">The short name for the Option. This must not be <c>whitespace</c> or a control character.</param>
		/// <returns></returns>
		/// <exception cref="OptionAlreadyExistsException">
		/// A Option with the same <paramref name="shortOption"/> name already exists in the <see cref="IFluentCommandLineParser"/>.
		/// </exception>
		public ICommandLineOptionFluent<T> Setup<T>(char shortOption)
		{
			return SetupInternal<T>(shortOption.ToString(CultureInfo.InvariantCulture), null);
		}

		/// <summary>
		/// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified long Option name.
		/// </summary>
		/// <param name="longOption">The long name for the Option. This must not be <c>null</c>, <c>empty</c> or only <c>whitespace</c>.</param>
		/// <returns></returns>
		/// <exception cref="OptionAlreadyExistsException">
		/// A Option with the same <paramref name="longOption"/> name already exists in the <see cref="IFluentCommandLineParser"/>.
		/// </exception>
		public ICommandLineOptionFluent<T> Setup<T>(string longOption)
		{
			return SetupInternal<T>(null, longOption);
		}

        /// <summary>
        /// Setup a new command using the specified name.
        /// </summary>
        /// <typeparam name="TBuildType">The type of arguments to be built for this command</typeparam>
        /// <param name="name">The name for the Command. This must be unique, not <c>null</c>, <c>empty</c> or contain only <c>whitespace</c>.</param>
        /// <returns></returns>
        /// <exception cref="CommandAlreadyExistsException">
        /// A Command with the same <paramref name="name"/> name already exists in the <see cref="IFluentCommandLineParser"/>.
        /// </exception>
        public ICommandLineCommandFluent<TBuildType> SetupCommand<TBuildType>(string name) where TBuildType : new()
        {
            var command = new CommandLineCommand<TBuildType>(this) { Name = name };
            Commands.Add(command);
            return command;
        }

		/// <summary>
		/// Parses the specified <see><cref>T:System.String[]</cref></see> using the setup Options.
		/// </summary>
		/// <param name="args">The <see><cref>T:System.String[]</cref></see> to parse.</param>
		/// <returns>An <see cref="ICommandLineParserResult"/> representing the results of the parse operation.</returns>
		public ICommandLineParserResult Parse(string[] args)
		{
		    if (SkipTheFirstArg) args = args.Skip(1).ToArray();

			var parserEngineResult = this.ParserEngine.Parse(args, HasCommands);
			var parsedOptions = parserEngineResult.ParsedOptions.ToList();

			var result = new CommandLineParserResult { EmptyArgs = parsedOptions.IsNullOrEmpty(), RawResult = parserEngineResult };

			if (this.HelpOption.ShouldShowHelp(parsedOptions, StringComparison))
			{
				result.HelpCalled = true;
				this.HelpOption.ShowHelp(this.Options);
				return result;
			}

		    if (parserEngineResult.HasCommand)
		    {
		        var match = Commands.SingleOrDefault(cmd => cmd.Name.Equals(parserEngineResult.Command, this.StringComparison));
		        if (match != null)
		        {
                    var result2 = ParseOptions(match.Options, parsedOptions, result);
		            if (result2.HasErrors == false)
		            {
                        match.ExecuteOnSuccess();    
		            }
		            return result2;
		        }
		    }

            return ParseOptions(this.Options, parsedOptions, result);
		}

	    private ICommandLineParserResult ParseOptions(IEnumerable<ICommandLineOption> options, List<ParsedOption> parsedOptions, CommandLineParserResult result)
	    {
            /*
            * Step 1. match the setup Option to one provided in the args by either long or short names
            * Step 2. if the key has been matched then bind the value
            * Step 3. if the key is not matched and it is required, then add a new error
            * Step 4. the key is not matched and optional, bind the default value if available
            */
	        var matchedOptions = new HashSet<ParsedOption>();
	        var optionIndex = 0;
	        foreach (var setupOption in options)
	        {
	            // Step 1
	            ICommandLineOption option = setupOption;
	            var matchIndex = parsedOptions.FindIndex(pair =>
	                    !matchedOptions.Contains(pair) &&
	                    (pair.Key.Equals(option.ShortName, this.StringComparison) // tries to match the short name
	                     || pair.Key.Equals(option.LongName, this.StringComparison))// or else the long name
	            );

	            if (matchIndex > -1) // Step 2
	            {
	                var match = parsedOptions[matchIndex];

	                match.Order = matchIndex;
	                match.SetupCommand = option;
	                match.SetupOrder = optionIndex++;
	                matchedOptions.Add(match);

	                //parsedOptions.Remove(match);//will affect the matchIndex
	            }
	            else if (setupOption.UseForOrphanArgs && result.RawResult.AdditionalValues.Any())
	            {
	                try
	                {
	                    var parser = new OptionArgumentParser(SpecialCharacters);
	                    var blankOption = new ParsedOption();
	                    parser.ParseArguments(result.RawResult.AdditionalValues, blankOption);
	                    setupOption.Bind(blankOption);
	                }
	                catch (OptionSyntaxException)
	                {
	                    result.Errors.Add(new OptionSyntaxParseError(option, null));
	                    if (option.HasDefault)
	                        option.BindDefault();
	                }
	            }
                else
	            {
	                if (option.IsRequired) // Step 3
	                    result.Errors.Add(new ExpectedOptionNotFoundParseError(option));
	                else if (option.HasDefault)
	                    option.BindDefault(); // Step 4

	                result.UnMatchedOptions.Add(option);
	            }

	        }

	        foreach (var match in ParseSequence == ParseSequence.SameAsSetup ? matchedOptions.OrderBy(o => o.SetupOrder) : matchedOptions.OrderBy(o => o.Order))
	        {
	            try
	            {
	                match.SetupCommand.Bind(match);
	            }
	            catch (OptionSyntaxException)
	            {
	                result.Errors.Add(new OptionSyntaxParseError(match.SetupCommand, match));
	                if (match.SetupCommand.HasDefault)
	                    match.SetupCommand.BindDefault();
	            }
	        }

	        parsedOptions.Where(item => !matchedOptions.Contains(item)).ForEach(item => result.AdditionalOptionsFound.Add(new KeyValuePair<string, string>(item.Key, item.Value)));

	        result.ErrorText = ErrorFormatter.Format(result.Errors);

	        return result;
        }

        /// <summary>
        /// Setup the help args.
        /// </summary>
        /// <param name="helpArgs">The help arguments to register.</param>
        public IHelpCommandLineOptionFluent SetupHelp(params string[] helpArgs)
		{
			var helpOption = this.OptionFactory.CreateHelpOption(helpArgs);
			this.HelpOption = helpOption;
			return helpOption;
		}

		/// <summary>
		/// Returns the Options that have been setup for this parser.
		/// </summary>
		IEnumerable<ICommandLineOption> ICommandLineOptionContainer.Options
		{
			get { return Options; }
		}
	}
}
