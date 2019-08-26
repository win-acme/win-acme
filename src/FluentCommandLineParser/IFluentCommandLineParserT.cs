#region License
// IFluentCommandLineParserT.cs
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
using System.Linq.Expressions;
using Fclp.Internals;

namespace Fclp
{
    /// <summary>
    /// A command line parser which provides methods and properties 
    /// to easily and fluently parse command line arguments into
    /// a predefined arguments object.
    /// </summary>
	public interface IFluentCommandLineParser<TBuildType> where TBuildType : class
	{
		/// <summary>
		/// Gets the constructed object.
		/// </summary>
		TBuildType Object { get; }

		/// <summary>
		/// Sets up an Option for a write-able property on the type being built.
		/// </summary>
		ICommandLineOptionBuilderFluent<TProperty> Setup<TProperty>(Expression<Func<TBuildType, TProperty>> propertyPicker);

		/// <summary>
		/// Parses the specified <see><cref>T:System.String[]</cref></see> using the setup Options.
		/// </summary>
		/// <param name="args">The <see><cref>T:System.String[]</cref></see> to parse.</param>
		/// <returns>An <see cref="ICommandLineParserResult"/> representing the results of the parse operation.</returns>
		ICommandLineParserResult Parse(string[] args);

		/// <summary>
		/// Setup the help args.
		/// </summary>
		/// <param name="helpArgs">The help arguments to register.</param>
		IHelpCommandLineOptionFluent SetupHelp(params string[] helpArgs);

		/// <summary>
		/// Gets or sets whether values that differ by case are considered different. 
		/// </summary>
		bool IsCaseSensitive { get; set; }

        /// <summary>
        /// Returns the Options that have been setup for this parser.
        /// </summary>
        IEnumerable<ICommandLineOption> Options { get; }

        /// <summary>
        /// Configures the <see cref="IFluentCommandLineParser{TBuildType}"/> so that short and long options that differ by case are considered the same.
        /// </summary>
        /// <returns></returns>
        IFluentCommandLineParser<TBuildType> MakeCaseInsensitive();

        /// <summary>
        /// Configures the <see cref="IFluentCommandLineParser{TBuildType}"/> so that short options are treated the same as long options, thus
        /// unique short option behaviour is ignored.
        /// </summary>
        /// <returns></returns>
        IFluentCommandLineParser<TBuildType> DisableShortOptions();

        /// <summary>
        /// Configures the <see cref="IFluentCommandLineParser{TBuildType}"/> to use the specified option prefixes instead of the default.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        IFluentCommandLineParser<TBuildType> UseOwnOptionPrefix(params string[] prefix);

        /// <summary>
        /// Configures the <see cref="IFluentCommandLineParser"/> to skip the first of the specified arguments.
        /// This can be useful when Windows inserts the application name in the command line arguments for your application.
        /// </summary>
        /// <returns>this <see cref="IFluentCommandLineParser{TBuildType}"/></returns>
	    IFluentCommandLineParser<TBuildType> SkipFirstArg();
	}
}