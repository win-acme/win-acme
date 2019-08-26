#region License
// ICommandLineOptionBuilderFluent.cs
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
namespace Fclp
{
	/// <summary>
	/// Defines the fluent interface for setting up a <see cref="ICommandLineOptionFluent{TProperty}"/>.
	/// </summary>
	public interface ICommandLineOptionBuilderFluent<TProperty>
	{
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
		ICommandLineOptionFluent<TProperty> As(char shortOption, string longOption);

		/// <summary>
		/// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified short Option name.
		/// </summary>
		/// <param name="shortOption">The short name for the Option. This must not be <c>whitespace</c> or a control character.</param>
		/// <returns></returns>
		/// <exception cref="InvalidOptionNameException">if <paramref name="shortOption"/> is invalid for a short option.</exception>
		/// <exception cref="OptionAlreadyExistsException">
		/// A Option with the same <paramref name="shortOption"/> name 
		/// already exists in the <see cref="IFluentCommandLineParser"/>.
		/// </exception>
		ICommandLineOptionFluent<TProperty> As(char shortOption);

		/// <summary>
		/// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified long Option name.
		/// </summary>
		/// <param name="longOption">The long name for the Option. This must not be <c>null</c>, <c>empty</c> or only <c>whitespace</c>.</param>
		/// <exception cref="InvalidOptionNameException">if <paramref name="longOption"/> is invalid for a long option.</exception>
		/// <exception cref="OptionAlreadyExistsException">
		/// A Option with the same <paramref name="longOption"/> name already exists in the <see cref="IFluentCommandLineParser"/>.
		/// </exception>
		ICommandLineOptionFluent<TProperty> As(string longOption);
	}
}