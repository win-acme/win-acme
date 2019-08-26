#region License
// CommandLineOptionValidator.cs
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

namespace Fclp.Internals.Validators
{
	/// <summary>
	/// Wrapping validator that executes all the individual validation rules.
	/// </summary>
	public class CommandLineOptionValidator : ICommandLineOptionValidator
	{
		private readonly IList<ICommandLineOptionValidator> _rules;

		/// <summary>
		/// Initialises a new instance of the <see cref="CommandLineOptionValidator"/> class.
		/// </summary>
        public CommandLineOptionValidator(ICommandLineOptionContainer container, SpecialCharacters specialCharacters)
		{
			_rules = new List<ICommandLineOptionValidator>
			{
				new OptionNameValidator(specialCharacters),
				new NoDuplicateOptionValidator(container)
			};
		}

	    /// <summary>
	    /// Validates the specified <see cref="ICommandLineOption"/> against all the registered rules.
	    /// </summary>
	    /// <param name="commandLineOption">The <see cref="ICommandLineOption"/> to validate.</param>
	    /// <param name="stringComparison"></param>
	    public void Validate(ICommandLineOption commandLineOption, StringComparison stringComparison)
		{
			foreach (var rule in _rules)
			{
				rule.Validate(commandLineOption, stringComparison);
			}
		}
	}
}