#region License
// ListTests.cs
// Copyright (c) 2015, Simon Williams
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

namespace Fclp.Internals.Parsing.OptionParsers
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public class NullableEnumCommandLineOptionParser<TEnum> : ICommandLineOptionParser<TEnum?> where TEnum : struct
    {
        private readonly ICommandLineOptionParserFactory _parserFactory;

        /// <summary>
        /// Initialises a new instance of the <see cref="ListCommandLineOptionParser{T}"/>.
        /// </summary>
        /// <param name="parserFactory"></param>
        public NullableEnumCommandLineOptionParser(ICommandLineOptionParserFactory parserFactory)
        {
            var type = typeof(TEnum);
            if (!type.IsEnum) throw new ArgumentException(string.Format("T must be an System.Enum but is '{0}'", type));
            _parserFactory = parserFactory;
        }

        /// <summary>
        /// Parses the specified <see cref="System.String"/> into the return type.
        /// </summary>
        /// <param name="parsedOption"></param>
        /// <returns>The parsed value.</returns>
        public TEnum? Parse(ParsedOption parsedOption)
        {
            if (parsedOption.HasValue == false) return null;
            var parser = _parserFactory.CreateParser<TEnum>();
            return parser.Parse(parsedOption);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>.
        /// </summary>
        /// <param name="parsedOption"></param>
        /// <returns><c>true</c> if the specified <see cref="System.String"/> can be parsed by this <see cref="ICommandLineOptionParser{T}"/>; otherwise <c>false</c>.</returns>
        public bool CanParse(ParsedOption parsedOption)
        {
            if (parsedOption == null) return false;
            if (parsedOption.HasValue == false) return true;

            var parser = _parserFactory.CreateParser<TEnum>();

            return parser.CanParse(parsedOption);
        }
    }
}