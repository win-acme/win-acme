using PKISharp.WACS.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class ArgumentResult<T, P> where T : class, IArguments, new()
    {
        /// <summary>
        /// Starting value from command line parser
        /// </summary>
        protected readonly P? _baseValue;

        /// <summary>
        /// Metadata obtained through reflection
        /// </summary>
        private readonly CommandLineAttribute _metaData;

        /// <summary>
        /// Have be produced a valid value?
        /// </summary>
        private bool _valid = true;

        /// <summary>
        /// Feedback on why we are not valid
        /// </summary>
        private string? _validationMessage = null;

        /// <summary>
        /// Action to run based on the fluently defined chain
        /// </summary>
        private readonly List<Func<P?, Task<P?>>> _actions = new();

        /// <summary>
        /// Copy of the interactive action because we might
        /// want to jump back to that one in some cases
        /// </summary>
        private Func<P?, Task<P?>>? _userInput;

        /// <summary>
        /// Ask the user for input
        /// </summary>
        private readonly Func<string, P?, Task<P?>> _input;

        /// <summary>
        /// Default value set at some point during the chain
        /// </summary>
        protected P? _defaultValue;

        private static bool HasValue(P? current)
        {
            if (current == null)
            {
                return false;
            }
            else if (current is string currentString)
            {
                return !string.IsNullOrWhiteSpace(currentString);
            }
            return true;
        }

        internal ArgumentResult(P baseValue, CommandLineAttribute metaData, Func<string, P?, Task<P?>> input)
        {
            _baseValue = baseValue;
            _metaData = metaData;
            _input = input;
        }

        /// <summary>
        /// Test the value using the validator parameter,
        /// throwing an exception with errorReason if the
        /// validator returns false
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="errorReason"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> Interactive(IInputService input, string label)
        {
            _userInput = async current =>
            {
                // Will need revalidation after new input in any case
                _valid = true;
                input.CreateSpace();
                input.Show("Description", _metaData.Description);
                if (HasValue(_defaultValue))
                {
                    if (_metaData.Secret)
                    {
                        input.Show("Default", "********");
                    } 
                    else
                    {
                        input.Show("Default", _defaultValue?.ToString());
                    }
                }
                var userInput = await _input(label, current);
                return userInput;
            };
            _actions.Add(_userInput);
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
        internal ArgumentResult<T, P> Validate(Func<P?, bool> validator, string errorReason)
        {
            _actions.Add(current =>
            {
                if (_valid)
                {
                    _valid = validator(current);
                    if (!_valid)
                    {
                        _validationMessage = errorReason;
                    }
                }
                return Task.FromResult(current);
            });
            return this;
        }

        /// <summary>
        /// Shortcut for required input validation
        /// </summary>
        /// <returns></returns>
        internal ArgumentResult<T, P> Required(bool orDefault = false) => Validate(current => ArgumentResult<T, P>.HasValue(current), "Missing");

        /// <summary>
        /// Set a default value if not value was
        /// specified so far
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> WithDefault(P value)
        {
            _defaultValue = value;
            _actions.Add(current => {
                return Task.FromResult(ArgumentResult<T, P>.HasValue(current) ? current : value);
            });
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
            _actions.Add(current => {
                var currentIsDefault = Comparer<P>.Default.Compare(current, _defaultValue) == 0;
                return Task.FromResult(currentIsDefault ? default : current);
            });
            return this;
        }

        /// <summary>
        /// Run the chain of methods required to 
        /// get the final value
        /// </summary>
        /// <returns></returns>
        public async Task<P?> GetValue()
        {
            var returnValue = _baseValue;
            var actionIndex = 0;
            var userInputIndex = _userInput != null ? _actions.IndexOf(_userInput) : -1;
            var valueBeforeUserInput = _baseValue;
            while (actionIndex < _actions.Count)
            {
                var action = _actions[actionIndex];
                if (actionIndex == userInputIndex)
                {
                    valueBeforeUserInput = returnValue;
                }
                returnValue = await action(returnValue);
                if (!_valid)
                {   
                    if (_userInput == null)
                    {
                        throw new Exception($"Invalid argument --{_metaData.Name?.ToLower()}: {_validationMessage}");
                    } 
                    else
                    {
                        // Go back to the user input stage
                        returnValue = valueBeforeUserInput;
                        actionIndex = _actions.IndexOf(_userInput);
                        continue;
                    }
                }
                actionIndex++;
            }
            return returnValue;
        }
    }
}