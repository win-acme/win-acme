#region License
// ICommandLineOptionParserFactory.cs
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

using Fclp.Internals.Parsing.OptionParsers;

namespace Fclp.Internals.Parsing
{
	/// <summary>
	/// Represents a factory capable of creating <see cref="ICommandLineOptionParser{T}"/>.
	/// </summary>
	public interface ICommandLineOptionParserFactory
	{
		/// <summary>
		/// Creates a <see cref="ICommandLineOptionParser{T}"/> to handle the specified type.
		/// </summary>
		/// <typeparam name="T">The type of parser to create.</typeparam>
		/// <returns>A <see cref="ICommandLineOptionParser{T}"/> suitable for the specified type.</returns>
		/// <exception cref="UnsupportedTypeException">If the specified type is not supported by this factory.</exception>
		ICommandLineOptionParser<T> CreateParser<T>();
	}
}