#region License
// ICommandLineOption.cs
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
using Fclp.Internals.Parsing;

namespace Fclp.Internals
{
	/// <summary>
	/// Represents a setup command line Option
	/// </summary>
	public interface ICommandLineOption
	{
		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> is required.
		/// </summary>
		bool IsRequired { get; }

		/// <summary>
		/// Gets the description set for this <see cref="ICommandLineOption"/>.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Binds the specified <see cref="System.String"/> to this <see cref="ICommandLineOption"/>.
		/// </summary>
		/// <param name="value">The <see cref="System.String"/> to bind.</param>
		void Bind(ParsedOption value);

		/// <summary>
		/// Binds the default value for this <see cref="ICommandLineOption"/> if available.
		/// </summary>
		void BindDefault();

		/// <summary>
		/// Gets the short name of this <see cref="ICommandLineOption"/>.
		/// </summary>
		string ShortName { get; }

		/// <summary>
		/// Gets the long name of this <see cref="ICommandLineOption"/>.
		/// </summary>
		string LongName { get; }

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> has a long name.
		/// </summary>
		bool HasLongName { get; }

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> has a short name.
		/// </summary>
		bool HasShortName { get; }

		/// <summary>
		/// Gets whether this <see cred="ICommandLineOption"/> has a callback setup.
		/// </summary>
		bool HasCallback { get; }

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> has an additional arguments callback setup.
		/// </summary>
		bool HasAdditionalArgumentsCallback { get; }

		/// <summary>
		/// Gets whether this <see cref="ICommandLineOption"/> has a default value setup.
		/// </summary>
		bool HasDefault { get; }

		/// <summary>
		/// Gets the setup <see cref="System.Type"/> for this option.
		/// </summary>
		Type SetupType { get; }

        /// <summary>
        /// Gets or sets the command this options belongs too.
        /// </summary>
        ICommandLineCommand Command { get; set; }

        /// <summary>
        /// Gets whether orphan args should be assigned to this option.
        /// </summary>
        bool UseForOrphanArgs { get; }

        /// <summary>
        /// Gets whether this option is assigned to a command.
        /// </summary>
	    bool HasCommand { get; }

	    /// <summary>
	    /// Gets the default value set for this options
	    /// </summary>
	    /// <returns>The default value set or <c>null</c> if no value has been set</returns>
	    object GetDefaultValue();
	}
}
