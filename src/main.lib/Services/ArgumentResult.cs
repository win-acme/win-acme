using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class ArgumentResult<T, P> where T : class, IArguments, new()
    {
        /// <summary>
        /// Metadata obtained through reflection
        /// </summary>
        private readonly CommandLineAttribute _metaData;

        /// <summary>
        /// Starting value from command line parser
        /// </summary>
        protected readonly P? _argumentValue;

        /// <summary>
        /// Default value set at some point during the chain
        /// </summary>
        protected P? _defaultValue;

        /// <summary>
        /// User input set at some point during the chain
        /// </summary>
        protected P? _inputValue;

        /// <summary>
        /// Label to show to the user in interactive mode
        /// </summary>
        protected string? _inputLabel;

        /// <summary>
        /// Allow null input from interactive mode
        /// </summary>
        protected bool _allowEmpty;

        /// <summary>
        /// Inputservice available (e.g. interactive mode)
        /// </summary>
        protected IInputService? _inputService;

        /// <summary>
        /// Ask the user for input
        /// </summary>
        private readonly Func<string, P?, Task<P?>> _inputFunction;

        /// <summary>
        /// Validator to run
        /// </summary>
        private readonly List<Tuple<Func<P?, Task<bool>>, string>> _validators = new();

        /// <summary>
        /// Do not emit default value
        /// </summary>
        private bool _defaultAsNull = false;

        /// <summary>
        /// Required value
        /// </summary>
        private bool _required = false;

        /// <summary>
        /// Test if we currently have a value
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private bool HasValue(P? current)
        {
            if (current == null)
            {
                return false;
            }
            else if (current is string currentString)
            {
                if (_allowEmpty)
                {
                    return true;
                } 
                else
                {
                    return !string.IsNullOrWhiteSpace(currentString);
                }
            }
            else if (current is ProtectedString protectedString)
            {
                if (protectedString.Value == null)
                {
                    return false;
                }
                else if (_allowEmpty)
                {
                    return true;
                }
                else
                {
                    return !string.IsNullOrWhiteSpace(protectedString.Value);
                }
            }
            return true;
        }

        internal ArgumentResult(P baseValue, CommandLineAttribute metaData, Func<string, P?, Task<P?>> input, bool allowEmtpy = false)
        {
            _argumentValue = baseValue;
            _metaData = metaData;
            _inputFunction = input;
            _allowEmpty = allowEmtpy;
        }

        /// <summary>
        /// Allow interactive input
        /// </summary>
        /// <param name="input"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> Interactive(IInputService input, string label, bool? allowEmtpy = null)
        {
            if (allowEmtpy == true)
            {
                if (_required)
                {
                    throw new InvalidOperationException("Required cannot be combined with AllowNull");
                }
                _allowEmpty = true;
            }
            _inputService = input;
            _inputLabel = label;
            return this;
        }

        /// <summary>
        /// Test the value using the validator parameter,
        /// throwing an exception with errorReason if the
        /// validator returns false
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="errorReason"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> Validate(Func<P?, Task<bool>> validator, string errorReason)
        {
            _validators.Add(new Tuple<Func<P?, Task<bool>>, string>(validator, errorReason));
            return this;
        }

        /// <summary>
        /// Shortcut for required input validation
        /// </summary>
        /// <returns></returns>
        internal ArgumentResult<T, P> Required()
        {
            if (_allowEmpty)
            {
                throw new InvalidOperationException("Required cannot be combined with AllowNull");
            }
            _required = true;
            return this;
        }

        /// <summary>
        /// Set a default value if not value was
        /// specified so far
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> WithDefault(P value)
        {
            _defaultValue = value;
            return this;
        }

        /// <summary>
        /// Set a default value if not value was
        /// specified so far
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> DefaultAsNull()
        {
            _defaultAsNull = true;
            return this;
        }

        private async Task<P?> GetInput(IInputService input, P? current)
        {
            input.CreateSpace();
            input.Show("Description", _metaData.Description);
            if (HasValue(_defaultValue))
            {
                var showValue = _metaData.Secret ? "********" : _defaultValue?.ToString();
                input.Show("Default", showValue);
            }
            if (HasValue(_argumentValue))
            {
                var showValue = _metaData.Secret ? "********" : _argumentValue?.ToString();
                input.Show("Argument", showValue);
            }
            return await _inputFunction(_inputLabel ?? "Input", current);
        }

        private async Task<(bool, string?)> IsValid(P? returnValue)
        {
            // Validate
            if (_required && !HasValue(returnValue))
            {
                if (!string.IsNullOrWhiteSpace(_inputLabel))
                {
                    return (false, "This is a required value");
                } 
                else
                {
                    return (false, "Missing value --{_metaData.Name}");
                }
            }
            if (HasValue(returnValue))
            {
                foreach (var validator in _validators)
                {
                    var validationResult = await validator.Item1(returnValue);
                    if (!validationResult)
                    {
                        if (!string.IsNullOrWhiteSpace(_inputLabel))
                        {
                            return (false, $"Invalid value: {validator.Item2}");
                        }
                        else
                        {
                            return (false, $"Invalid --{_metaData.Name}: {validator.Item2}");
                        }
                    }
                }
            }
            return (true, null);
        }

        /// <summary>
        /// Run the chain of methods required to 
        /// get the final value
        /// </summary>
        /// <returns></returns>
        public async Task<P?> GetValue()
        {
            var returnValue = _argumentValue;
            if (!HasValue(returnValue))
            {
                returnValue = _defaultValue;
            }
            while (true)
            {
                if (_inputService != null)
                {
                    _inputValue = await GetInput(_inputService, _defaultValue);
                    if (HasValue(_inputValue))
                    {
                        returnValue = _inputValue;
                    }
                }
                var (valid, validationError) = await IsValid(returnValue);
                if (!valid)
                {
                    if (_inputService == null)
                    {
                        throw new Exception(validationError);
                    }
                }
                else
                {
                    break;
                }
            }

            // Sometime we don't want to store the default result
            // even when it comes straight from the user
            if (_defaultAsNull && Comparer<P>.Default.Compare(returnValue, _defaultValue) == 0)
            {
                return default;
            }
            return returnValue;
        }
    }
}