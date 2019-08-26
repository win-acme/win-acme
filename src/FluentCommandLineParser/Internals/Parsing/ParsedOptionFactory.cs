#region License
// ParsedOptionFactory.cs
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

using System.Linq;

namespace Fclp.Internals.Parsing
{
	/// <summary>
	/// Factory used to created parsed option meta data.
	/// </summary>
	public class ParsedOptionFactory
	{
	    private readonly SpecialCharacters _specialCharacters;

        /// <summary>
        /// Initialises a new instance of <see cref="ParsedOptionFactory"/>.
        /// </summary>
        /// <param name="specialCharacters"></param>
	    public ParsedOptionFactory(SpecialCharacters specialCharacters)
	    {
	        _specialCharacters = specialCharacters;
	    }

	    /// <summary>
		/// Creates parsed option meta data for the specified raw key.
		/// </summary>
		public ParsedOption Create(string rawKey)
		{
			var prefix = ExtractPrefix(rawKey);

			return new ParsedOption
			{
				RawKey = rawKey,
				Prefix = prefix,
				Key = rawKey.Remove(0, prefix.Length),
				Suffix = ExtractSuffix(rawKey)
			};			
		}


		/// <summary>
		/// Extracts the key identifier from the specified <see cref="System.String"/>.
		/// </summary>
		/// <param name="arg">The <see cref="System.String"/> to extract the key identifier from.</param>
		/// <returns>A <see cref="System.String"/> representing the key identifier if found; otherwise <c>null</c>.</returns>
		private string ExtractPrefix(string arg)
		{
			return arg != null ? _specialCharacters.OptionPrefix.FirstOrDefault(arg.StartsWith) : null;
		}

		/// <summary>
		/// Extracts the key identifier from the specified <see cref="System.String"/>.
		/// </summary>
		/// <param name="arg">The <see cref="System.String"/> to extract the key identifier from.</param>
		/// <returns>A <see cref="System.String"/> representing the key identifier if found; otherwise <c>null</c>.</returns>
		private string ExtractSuffix(string arg)
		{
			return arg != null ? _specialCharacters.OptionSuffix.FirstOrDefault(arg.EndsWith) : null;
		}
	}
}