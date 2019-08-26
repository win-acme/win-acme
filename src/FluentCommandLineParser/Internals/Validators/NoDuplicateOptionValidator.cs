#region License
// NoDuplicateOptionValidator.cs
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

namespace Fclp.Internals.Validators
{
    /// <summary>
    /// Represents a container that contains setup options.
    /// </summary>
    public interface ICommandLineOptionContainer
    {
        /// <summary>
        /// Gets or sets a list of <see cref="ICommandLineOption"/>.
        /// </summary>
        IEnumerable<ICommandLineOption> Options { get; }
    }

	/// <summary>
	/// Validator used to ensure no there are duplicate Options setup.
	/// </summary>
	public class NoDuplicateOptionValidator : ICommandLineOptionValidator
	{
        private readonly ICommandLineOptionContainer _container;

		/// <summary>
		/// Initialises a new instance of the <see cref="NoDuplicateOptionValidator"/> class.
		/// </summary>
        /// <param name="container">The <see cref="IFluentCommandLineParser"/> containing the setup options. This must not be null.</param>
        public NoDuplicateOptionValidator(ICommandLineOptionContainer container)
		{
            if (container == null) throw new ArgumentNullException("container");
            _container = container;
		}

        ///// <summary>
        ///// Gets the <see cref="StringComparison"/> type used for duplicates.
        ///// </summary>
        //private StringComparison ComparisonType
        //{
        //    get { return _container.IsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase; }
        //}

	    /// <summary>
	    /// Verifies that the specified <see cref="ICommandLineOption"/> will not cause any duplication.
	    /// </summary>
	    /// <param name="commandLineOption">The <see cref="ICommandLineOption"/> to validate.</param>
	    /// <param name="stringComparison"></param>
	    public void Validate(ICommandLineOption commandLineOption, StringComparison stringComparison)
		{
            foreach (var option in _container.Options)
			{
			    if (option.HasCommand)
			    {
			        if (CommandsAreEqual(option.Command, commandLineOption.Command, stringComparison))
			        {
                        if (string.IsNullOrEmpty(commandLineOption.ShortName) == false)
                        {
                            ValuesAreEqual(commandLineOption.ShortName, option.ShortName, stringComparison);
                        }

                        if (string.IsNullOrEmpty(commandLineOption.LongName) == false)
                        {
                            ValuesAreEqual(commandLineOption.LongName, option.LongName, stringComparison);
                        }
                    }
			    }
			    else
			    {
                    if (string.IsNullOrEmpty(commandLineOption.ShortName) == false)
                    {
                        ValuesAreEqual(commandLineOption.ShortName, option.ShortName, stringComparison);
                    }

                    if (string.IsNullOrEmpty(commandLineOption.LongName) == false)
                    {
                        ValuesAreEqual(commandLineOption.LongName, option.LongName, stringComparison);
                    }
                }
			
			}
		}

        private void ValuesAreEqual(string value, string otherValue, StringComparison stringComparison)
		{
            if (string.Equals(value, otherValue, stringComparison))
			{
				throw new OptionAlreadyExistsException(value);
			}
		}

        private bool CommandsAreEqual(ICommandLineCommand command, ICommandLineCommand otherCommand, StringComparison stringComparison)
        {
            if (command == null && otherCommand == null) return true;
            if (command == null) return false;
            if (otherCommand == null) return false;
            return string.Equals(command.Name, otherCommand.Name, stringComparison);
	    }
	}
}