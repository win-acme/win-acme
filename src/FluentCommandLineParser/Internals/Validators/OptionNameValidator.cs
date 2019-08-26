#region License
// OptionNameValidator.cs
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
using System.Globalization;
using System.Linq;

namespace Fclp.Internals.Validators
{
	/// <summary>
	/// Validator to ensure a new Option has a valid long name.
	/// </summary>
	public class OptionNameValidator : ICommandLineOptionValidator
	{
	    private readonly char[] _reservedChars;

        /// <summary>
        /// Initialises a new instance of <see cref="OptionNameValidator"/>.
        /// </summary>
        /// <param name="specialCharacters"></param>
        public OptionNameValidator(SpecialCharacters specialCharacters)
	    {
	        _reservedChars = specialCharacters.ValueAssignments.Union(new[] { specialCharacters.Whitespace }).ToArray();
        }

	    /// <summary>
	    /// Verifies that the specified <see cref="ICommandLineOption"/> has a valid short/long name combination.
	    /// </summary>
	    /// <param name="commandLineOption">The <see cref="ICommandLineOption"/> to validate. This must not be null.</param>
	    /// <param name="stringComparison"></param>
	    /// <exception cref="ArgumentNullException">if <paramref name="commandLineOption"/> is null.</exception>
	    public void Validate(ICommandLineOption commandLineOption, StringComparison stringComparison)
		{
			if (commandLineOption == null) throw new ArgumentNullException("commandLineOption");

            ValidateShortName(commandLineOption.ShortName);
			ValidateLongName(commandLineOption.LongName);
			ValidateShortAndLongName(commandLineOption.ShortName, commandLineOption.LongName);
		}

		private void ValidateShortAndLongName(string shortName, string longName)
		{
			if (string.IsNullOrEmpty(shortName) && string.IsNullOrEmpty(longName))
			{
				ThrowInvalid(string.Empty, "A short or long name must be provided.");
			}
		}

		private void ValidateLongName(string longName)
		{
			if (string.IsNullOrEmpty(longName)) return;

			VerifyDoesNotContainsReservedChar(longName);

			if (longName.Length == 1)
			{
				ThrowInvalid(longName, "Long names must be longer than a single character. Single characters are reserved for short options only.");
			}
		}

		private void ValidateShortName(string shortName)
		{
			if (string.IsNullOrEmpty(shortName)) return;

			if (shortName.Length > 1)
			{
				ThrowInvalid(shortName, "Short names must be a single character only.");
			}

			VerifyDoesNotContainsReservedChar(shortName);

			if (char.IsControl(shortName, 0))
			{
				ThrowInvalid(shortName, "The character '" + shortName + "' is not valid for a short name.");
			}
		}

		private void VerifyDoesNotContainsReservedChar(string value)
		{
			if (string.IsNullOrEmpty(value)) return;

			foreach (char reservedChar in _reservedChars)
			{
				if (value.Contains(reservedChar))
				{
					ThrowInvalid(value, "The character '" + reservedChar + "' is not valid within a short or long name.");
				}
			}
		}

		private static void ThrowInvalid(string value, string message)
		{
			throw new InvalidOptionNameException(
				string.Format(CultureInfo.InvariantCulture, "Invalid option name '{0}'. {1}", value, message));
		}
	}
}