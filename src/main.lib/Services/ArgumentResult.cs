using PKISharp.WACS.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class ArgumentResult<T, P> where T : class, IArguments, new()
    {
        protected readonly P? _baseValue;
        private readonly CommandLineAttribute _metaData;
        private bool _valid = true;
        private readonly Func<string?, P> _parser;
        private string? _validationMessage = null;
        private readonly List<Func<P?, Task<P?>>> _actions = new();

        // Interactive mode
        private string? _label;
        private Func<P?, Task<P?>>? _userInput;

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

        internal ArgumentResult(P baseValue, CommandLineAttribute metaData, Func<string?, P> parser)
        {
            _baseValue = baseValue;
            _metaData = metaData;
            _parser = parser;
        }

        /// <summary>
        /// Test the value using the validator parameter,
        /// throwing an exception with errorReason if the
        /// validator returns false
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="errorReason"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> Interactive(string label, IInputService input, RunLevel runLevel)
        {
            _label = label;
            _userInput = async current =>
            {
                // Will need revalidation after new input in any case
                _valid = true;
                input.Show("Description", _metaData.Description);
                if (HasValue(current))
                {
                    input.Show("Default", current?.ToString());
                }
                var raw = await input.RequestString(label);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return current;
                }
                else
                {
                    try
                    {
                        return _parser(raw);
                    }
                    catch
                    {
                        return default;
                    }
                }
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
        internal ArgumentResult<T, P> Required() => Validate(current => ArgumentResult<T, P>.HasValue(current), "Missing");

        /// <summary>
        /// Set a default value if not value was
        /// specified so far
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal ArgumentResult<T, P> WithDefault(P value)
        {
            _actions.Add(current => Task.FromResult<P?>(ArgumentResult<T, P>.HasValue(current) ? current : value));
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