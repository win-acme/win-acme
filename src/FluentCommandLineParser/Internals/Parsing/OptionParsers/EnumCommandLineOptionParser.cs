#region License
// EnumCommandLineOptionParser.cs
// Copyright (c) 2014, Simon Williams
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

namespace Fclp.Internals.Parsing.OptionParsers
{
	/// <summary>
	/// Parser used to convert to <see cref="Enum"/>.
	/// </summary>
	/// <remarks>For <see cref="System.Boolean"/> types the value is optional. If no value is provided for the Option then <c>true</c> is returned.</remarks>
	/// /// <typeparam name="TEnum">The <see cref="System.Enum"/> that will be parsed by this parser.</typeparam>
	public class EnumCommandLineOptionParser<TEnum> : ICommandLineOptionParser<TEnum>
	{
		private readonly IList<TEnum> _all;
		private readonly Dictionary<string, TEnum> _insensitiveNames;
		private readonly Dictionary<int, TEnum> _values;

		/// <summary>
		/// Initialises a new instance of the <see cref="EnumCommandLineOptionParser{TEnum}"/> class.
		/// </summary>
		/// <exception cref="ArgumentException">If {TEnum} is not a <see cref="System.Enum"/>.</exception>
		public EnumCommandLineOptionParser()
		{
			var type = typeof(TEnum);
			if (!type.IsEnum) throw new ArgumentException(string.Format("T must be an System.Enum but is '{0}'", type));

			_all = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
			_insensitiveNames = _all.ToDictionary(k => Enum.GetName(typeof(TEnum), k).ToLowerInvariant());
			_values = _all.ToDictionary(k => Convert.ToInt32(k));
		}

		/// <summary>
		/// Parses the specified <see cref="System.String"/> into a <see cref="System.Boolean"/>.
		/// </summary>
		/// <param name="parsedOption"></param>
		/// <returns>
		/// A <see cref="System.Boolean"/> representing the parsed value.
		/// The value is optional. If no value is provided then <c>true</c> is returned.
		/// </returns>
		public TEnum Parse(ParsedOption parsedOption)
		{
			return (TEnum)Enum.Parse(typeof(TEnum), parsedOption.Value.ToLowerInvariant(), true);
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>.
		/// </summary>
		/// <param name="parsedOption"></param>
		/// <returns><c>true</c> if the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>; otherwise <c>false</c>.</returns>
		public bool CanParse(ParsedOption parsedOption)
		{
			if (parsedOption.HasValue == false) return false;
			if (parsedOption.Value.IsNullOrWhiteSpace()) return false;
			return IsDefined(parsedOption.Value);
		}

		/// <summary>
		/// Determines whether the specified <paramref name="value"/> can be parsed into {TEnum}.
		/// </summary>
		/// <param name="value">The value to be parsed</param>
		/// <returns>true if <paramref name="value"/> can be parsed; otherwise false.</returns>
		private bool IsDefined(string value)
		{
			int asInt;
			return int.TryParse(value, out asInt) 
				? IsDefined(asInt) 
				: _insensitiveNames.Keys.Contains(value.ToLowerInvariant());
		}

		/// <summary>
		/// Determines whether the specified <paramref name="value"/> represents a {TEnum} value.
		/// </summary>
		/// <param name="value">The <see cref="System.Int32"/> that represents a {TEnum} value.</param>
		/// <returns>true if <paramref name="value"/> represents a {TEnum} value; otherwise false.</returns>
		private bool IsDefined(int value)
		{
			return _values.Keys.Contains(value);
		}
	}
}