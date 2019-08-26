#region License
// UsefulExtension.cs
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

namespace Fclp.Internals.Extensions
{
	/// <summary>
	/// Contains some simple extension methods that are useful throughout the library.
	/// </summary>
	public static class UsefulExtension
	{
		/// <summary>
		/// Indicates whether the specified <see cref="System.String"/> is <c>null</c>, <c>empty</c> or contains only <c>whitespace</c>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>This method mimics the String.IsNullOrWhiteSpace method available in .Net 4 framework.</remarks>
		public static bool IsNullOrWhiteSpace(this string value)
		{
			return string.IsNullOrEmpty(value) || string.IsNullOrEmpty(value.Trim());
		}

		/// <summary>
		/// Indicates whether the specified <see cref="IEnumerable{T}"/> is <c>null</c> or contains no elements.
		/// </summary>
		/// <param name="enumerable">A <see cref="IEnumerable{T}"/> to check.</param>
		/// <returns><c>true</c> if <paramref name="enumerable"/> is <c>null</c> or contains no elements; otherwise <c>false</c>.</returns>
		public static bool IsNullOrEmpty<TSource>(this IEnumerable<TSource> enumerable)
		{
			return enumerable == null || enumerable.Any() == false;
		}

		/// <summary>
		/// Performs the specified action on each element of the <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="enumerable">A <see cref="IEnumerable{T}"/> to iterate through all the available elements.</param>
		/// <param name="action">The delegate to execute with on each element of the specified <see cref="IEnumerable{T}"/>.</param>
		/// <exception cref="ArgumentNullException">if <paramref name="enumerable"/> is <c>null</c>.</exception>
		public static void ForEach<TSource>(this IEnumerable<TSource> enumerable, Action<TSource> action)
		{
			foreach (var item in enumerable)
			{
				action(item);
			}
		}

		/// <summary>
		/// Indicates whether the specified <see cref="System.String"/> contains <c>whitespace</c>.
		/// </summary>
		/// <param name="value">The <see cref="System.String"/> to examine.</param>
		/// <returns><c>true</c> if <paramref name="value"/> contains at least one whitespace char; otherwise <c>false</c>.</returns>
		public static bool ContainsWhitespace(this string value)
		{
			return string.IsNullOrEmpty(value) == false && value.Contains(" ");
		}

		/// <summary>
		/// Wraps the specified <see cref="System.String"/> in double quotes.
		/// </summary>
		public static string WrapInDoubleQuotes(this string str)
		{
			return string.Format(@"""{0}""", str);
		}

		/// <summary>
		/// Removes and double quotes wrapping the specified <see cref="System.String"/>.
		/// </summary>
		public static string RemoveAnyWrappingDoubleQuotes(this string str)
		{
            if (str.IsNullOrWhiteSpace())
            {
                return str;
            }
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                str = str.Substring(1, str.Length - 2);
            }
            return str;
		}

		/// <summary>
		/// Wraps the specified <see cref="System.String"/> in double quotes if it contains at least one whitespace character.
		/// </summary>
		/// <param name="str">The <see cref="System.String"/> to examine and wrap.</param>
		public static string WrapInDoubleQuotesIfContainsWhitespace(this string str)
		{
			return str.ContainsWhitespace() && str.IsWrappedInDoubleQuotes() == false
				? str.WrapInDoubleQuotes()
				: str;
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.String"/> starts and ends with a double quote.
		/// </summary>
		/// <param name="str">The <see cref="System.String"/> to examine.</param>
		/// <returns><c>true</c> if <paramref name="str"/> is wrapped in double quotes; otherwise <c>false</c>.</returns>
		public static bool IsWrappedInDoubleQuotes(this string str)
		{
			return str.IsNullOrWhiteSpace() == false && str.StartsWith("\"") && str.EndsWith("\"");
		}

		/// <summary>
		/// Splits the specified <see cref="System.String"/> when each whitespace char is encountered into a collection of substrings.
		/// </summary>
		/// <param name="value">The <see cref="System.String"/> to split.</param>
		/// <returns>A collection of substrings taken from <paramref name="value"/>.</returns>
		/// <remarks>If the whitespace is wrapped in double quotes then it is ignored.</remarks>
		public static IEnumerable<string> SplitOnWhitespace(this string value)
		{
			if (string.IsNullOrEmpty(value)) return null;

			char[] parmChars = value.ToCharArray();

			bool inDoubleQuotes = false;

			for (int index = 0; index < parmChars.Length; index++)
			{
				if (parmChars[index] == '"')
					inDoubleQuotes = !inDoubleQuotes;

				if (!inDoubleQuotes && parmChars[index] == ' ')
					parmChars[index] = '\n';
			}

			return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		}

		/// <summary>
		/// Elements at or default.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items">The items.</param>
		/// <param name="index">The index.</param>
		/// <param name="defaultToUse">The default to use.</param>
		/// <returns></returns>
		public static T ElementAtOrDefault<T>(this T[] items, int index, T defaultToUse)
		{
		    if (items == null) return defaultToUse;
			return index >= 0 && index < items.Length
				? items[index]
				: defaultToUse;
		}
	}
}
