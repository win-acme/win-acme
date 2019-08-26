#region License
// CommandLineOptionBuilderFluent.cs
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
using System.Linq.Expressions;
using System.Reflection;

namespace Fclp.Internals
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICommandLineOptionSetupFactory
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
        ICommandLineOptionFluent<T> Setup<T>(char shortOption, string longOption);

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
        ICommandLineOptionFluent<T> Setup<T>(char shortOption);

        /// <summary>
        /// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified long Option name.
        /// </summary>
        /// <param name="longOption">The long name for the Option. This must not be <c>null</c>, <c>empty</c> or only <c>whitespace</c>.</param>
        /// <exception cref="InvalidOptionNameException">if <paramref name="longOption"/> is invalid for a long option.</exception>
        /// <exception cref="OptionAlreadyExistsException">
        /// A Option with the same <paramref name="longOption"/> name already exists in the <see cref="IFluentCommandLineParser"/>.
        /// </exception>
        ICommandLineOptionFluent<T> Setup<T>(string longOption);
    }

	/// <summary>
	/// Wraps the Setup call of the fluent command line parser and defines the callback to setup the property parsed value.
	/// </summary>
	/// <typeparam name="TBuildType">The type of object being populated.</typeparam>
	/// <typeparam name="TProperty">The type of the property the value will be assigned too.</typeparam>
	public class CommandLineOptionBuilderFluent<TBuildType, TProperty> : ICommandLineOptionBuilderFluent<TProperty>
	{
	    private readonly ICommandLineCommandT<TBuildType> _command;
        private readonly ICommandLineOptionSetupFactory _setupFactory;
		private readonly TBuildType _buildObject;
		private readonly Expression<Func<TBuildType, TProperty>> _propertyPicker;

	    /// <summary>
	    /// Initializes a new instance of the <see cref="CommandLineOptionBuilderFluent{TBuildType, TProperty}" /> class.
	    /// </summary>
	    /// <param name="setupFactory">The parser.</param>
	    /// <param name="buildObject">The build object.</param>
	    /// <param name="propertyPicker">The property picker.</param>
	    /// <param name="command"></param>
	    public CommandLineOptionBuilderFluent(
            ICommandLineOptionSetupFactory setupFactory, 
			TBuildType buildObject,
			Expression<Func<TBuildType, TProperty>> propertyPicker,
            ICommandLineCommandT<TBuildType> command)
		{
			_setupFactory = setupFactory;
			_buildObject = buildObject;
			_propertyPicker = propertyPicker;
	        _command = command;
		}

	    /// <summary>
	    /// Initializes a new instance of the <see cref="CommandLineOptionBuilderFluent{TBuildType, TProperty}" /> class.
	    /// </summary>
	    /// <param name="setupFactory">The parser.</param>
	    /// <param name="buildObject">The build object.</param>
	    /// <param name="propertyPicker">The property picker.</param>
	    public CommandLineOptionBuilderFluent(
            ICommandLineOptionSetupFactory setupFactory, 
			TBuildType buildObject,
			Expression<Func<TBuildType, TProperty>> propertyPicker)
		{
			_setupFactory = setupFactory;
			_buildObject = buildObject;
			_propertyPicker = propertyPicker;
		}



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
		public ICommandLineOptionFluent<TProperty> As(char shortOption, string longOption)
		{
			return _setupFactory.Setup<TProperty>(shortOption, longOption)
                          .AssignToCommand(_command)
			              .Callback(AssignValueToPropertyCallback);
		}

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
		public ICommandLineOptionFluent<TProperty> As(char shortOption)
		{
			return _setupFactory.Setup<TProperty>(shortOption)
                          .AssignToCommand(_command)
			              .Callback(AssignValueToPropertyCallback);
		}

		/// <summary>
		/// Setup a new <see cref="ICommandLineOptionFluent{T}"/> using the specified long Option name.
		/// </summary>
		/// <param name="longOption">The long name for the Option. This must not be <c>null</c>, <c>empty</c> or only <c>whitespace</c>.</param>
		/// <exception cref="InvalidOptionNameException">if <paramref name="longOption"/> is invalid for a long option.</exception>
		/// <exception cref="OptionAlreadyExistsException">
		/// A Option with the same <paramref name="longOption"/> name already exists in the <see cref="IFluentCommandLineParser"/>.
		/// </exception>
		public ICommandLineOptionFluent<TProperty> As(string longOption)
		{
			return _setupFactory.Setup<TProperty>(longOption)
                          .AssignToCommand(_command)
			              .Callback(AssignValueToPropertyCallback);
		}

		private void AssignValueToPropertyCallback(TProperty value)
		{
			var prop = (PropertyInfo)((MemberExpression)_propertyPicker.Body).Member;
			prop.SetValue(_buildObject, value, null);
		}
	}
}