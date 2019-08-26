#region License
// CommandLineOption.cs
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
using Fclp.Internals.Extensions;
using Fclp.Internals.Parsing;
using Fclp.Internals.Parsing.OptionParsers;

namespace Fclp.Internals
{
	/// <summary>
	/// A command line Option
	/// </summary>
	/// <typeparam name="T">The type of value this Option requires.</typeparam>
	public class CommandLineOption<T> : ICommandLineOptionResult<T>
	{
		#region Constructors

		/// <summary>
		/// Initialises a new instance of the <see cref="CommandLineOption{T}"/> class.
		/// </summary>
		/// <param name="shortName">The short name for this Option or <c>null</c> if not required. Either <paramref name="shortName"/> or <paramref name="longName"/> must not be <c>null</c>, <c>empty</c> or contain only <c>whitespace</c>.</param>
		/// <param name="longName">The long name for this Option or <c>null</c> if not required. Either <paramref name="shortName"/> or <paramref name="longName"/> must not be <c>null</c>, <c>empty</c> or contain only <c>whitespace</c>.</param>
		/// <param name="parser">The parser to use for this Option.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if both <paramref name="shortName"/> and <paramref name="longName"/> are <c>null</c>, <c>empty</c> or contain only <c>whitespace</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="parser"/> is <c>null</c>.</exception>
		public CommandLineOption(string shortName, string longName, ICommandLineOptionParser<T> parser)
		{
			if (parser == null) throw new ArgumentNullException("parser");

			this.ShortName = shortName;
			this.LongName = longName;
			this.Parser = parser;
		}

		#endregion

		#region Properties

		/// <inheritdoc/>
		/// <summary>
		/// Gets or sets the parser to use for this <see cref="CommandLineOption{T}"/>.
		/// </summary>
		ICommandLineOptionParser<T> Parser { get; set; }

		/// <summary>
		/// Gets the description set for this <see cref="CommandLineOption{T}"/>.
		/// </summary>
		public string Description { get; set; }

		internal Action<T> ReturnCallback { get; set; }

		internal Action<IEnumerable<string>> AdditionalArgumentsCallback { get; set; }

		internal T Default { get; set; }

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> is required.
		/// </summary>
		public bool IsRequired { get; set; }

		/// <summary>
		/// Gets the short name of this <see cref="ICommandLineOption"/>.
		/// </summary>
		public string ShortName { get; set; }

		/// <summary>
		/// Gets the long name of this <see cref="ICommandLineOption"/>.
		/// </summary>
		public string LongName { get; set; }

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> has a default value setup.
		/// </summary>
		public bool HasDefault { get; set; }

        /// <summary>
        /// Gets or sets the command this options is for.
        /// </summary>
        public ICommandLineCommand Command { get; set; }

		/// <summary>
		/// Gets the setup <see cref="System.Type"/> for this option.
		/// </summary>
		public Type SetupType
		{
			get
			{
				var type = typeof (T);
				var genericArgs = type.GetGenericArguments();
				return genericArgs.Any() ? genericArgs.First() : type;
			}
		}

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOptionFluent{T}"/> has a long name.
		/// </summary>
		public bool HasLongName
		{
			get { return this.LongName.IsNullOrWhiteSpace() == false; }
		}

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOptionFluent{T}"/> has a short name.
		/// </summary>
		public bool HasShortName
		{
			get { return this.ShortName.IsNullOrWhiteSpace() == false; }
		}

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> has a callback setup.
		/// </summary>
		public bool HasCallback
		{
			get { return this.ReturnCallback != null; }
		}

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> has an additional arguments callback setup.
		/// </summary>
		public bool HasAdditionalArgumentsCallback
		{
			get { return this.AdditionalArgumentsCallback != null; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Binds the specified <see cref="System.String"/> to the Option.
		/// </summary>
		/// <param name="value">The <see cref="System.String"/> to bind.</param>
		public void Bind(ParsedOption value)
		{
			if (this.Parser.CanParse(value) == false) throw new OptionSyntaxException();

            var input = this.Parser.Parse(value);
            if (typeof(T).IsClass && object.Equals(input, default(T)) && this.HasDefault)
                BindDefault();
            else
            {
                this.Bind(input);

                this.BindAnyAdditionalArgs(value);
            }
        }

		/// <summary>
		/// Binds the default value for this <see cref="ICommandLineOption"/> if available.
		/// </summary>
		public void BindDefault()
		{
			if (this.HasDefault)
				this.Bind(this.Default);
		}

		void Bind(T value)
		{
			if (this.HasCallback)
				this.ReturnCallback(value);
		}

		void BindAnyAdditionalArgs(ParsedOption option)
		{
			if (!this.HasAdditionalArgumentsCallback) return;

			if (option.AdditionalValues.Any())
			{
				this.AdditionalArgumentsCallback(option.AdditionalValues);
			}
		}

		/// <summary>
		/// Adds the specified description to the <see cref="ICommandLineOptionFluent{T}"/>.
		/// </summary>
		/// <param name="description">The <see cref="System.String"/> representing the description to use. This should be localised text.</param>
		/// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
		public ICommandLineOptionFluent<T> WithDescription(string description)
		{
			this.Description = description;
			return this;
		}

		/// <summary>
		/// Declares that this <see cref="ICommandLineOptionFluent{T}"/> is required and a value must be specified to fulfil it.
		/// </summary>
		/// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
		public ICommandLineOptionFluent<T> Required()
		{
			this.IsRequired = true;
			return this;
		}

		/// <summary>
		/// Specifies the method to invoke when the <see cref="ICommandLineOptionFluent{T}"/>. 
		/// is parsed. If a callback is not required either do not call it, or specify <c>null</c>.
		/// </summary>
		/// <param name="callback">The return callback to execute with the parsed value of the Option.</param>
		/// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
		public ICommandLineOptionFluent<T> Callback(Action<T> callback)
		{
			this.ReturnCallback = callback;
			return this;
		}

		/// <summary>
		/// Specifies the default value to use if no value is found whilst parsing this <see cref="ICommandLineOptionFluent{T}"/>.
		/// </summary>
		/// <param name="value">The value to use.</param>
		/// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
		public ICommandLineOptionFluent<T> SetDefault(T value)
		{
			this.Default = value;
			this.HasDefault = true;
			return this;
		}

		/// <summary>
		/// Specified the method to invoke with any addition arguments parsed with the Option.
		/// If additional arguments are not required either do not call it, or specify <c>null</c>.
		/// </summary>
		/// <param name="callback">The return callback to execute with the parsed addition arguments found for this Option.</param>
		/// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
		public ICommandLineOptionFluent<T> CaptureAdditionalArguments(Action<IEnumerable<string>> callback)
		{
			this.AdditionalArgumentsCallback = callback;
			return this;
		}

        /// <summary>
        /// Specifies a command to attached the option too.
        /// </summary>
        /// <param name="command">The command to attach the option too. This must not be <c>null</c> and already be setup with the parser.</param>
        /// <returns>A <see cref="ICommandLineOptionFluent{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> is <c>null</c>.</exception>
        /// <exception cref="CommandNotFoundException">Thrown if the specified <paramref name="command"/> does not exist in the parser.</exception>
        public ICommandLineOptionFluent<T> AssignToCommand(ICommandLineCommand command)
	    {
	        this.Command = command;
            return this;
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
	    public ICommandLineOptionFluent<T> UseForOrphanArguments()
	    {
	        this.UseForOrphanArgs = true;
	        return this;
	    }

        /// <summary>
        /// 
        /// </summary>
	    public bool UseForOrphanArgs { get; set; }

	    /// <summary>
	    /// Gets whether this option is associated with a command.
	    /// </summary>
	    public bool HasCommand
	    {
	        get { return Command != null; }
	    }

	    /// <summary>
	    /// Gets the default value set for this options
	    /// </summary>
	    /// <returns>The default value set or <c>null</c> if no value has been set</returns>
        public object GetDefaultValue()
	    {
	        if (HasDefault)
	        {
	            return Default;
	        }
	        return null;
	    }

	    #endregion Methods
	}
}
