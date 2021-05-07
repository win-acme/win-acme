using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace PKISharp.WACS.Services
{
    public partial class ArgumentsInputService
    {
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;
        private readonly IInputService _input;
        private readonly SecretServiceManager _secretService;

        public ArgumentsInputService(
            ILogService log,
            IArgumentsService arguments,
            IInputService input,
            SecretServiceManager secretService)
        {
            _log = log;
            _arguments = arguments;
            _input = input;
            _secretService = secretService;
        }
        public ArgumentResult<T, ProtectedString?> GetProtectedString<T>(Expression<Func<T, string?>> expression, bool allowEmtpy = false)
            where T : class, IArguments,
            new() => new(GetArgument(expression).Protect(allowEmtpy), GetMetaData(expression),
                async (label, value) => (await _secretService.GetSecret(label, value?.Value, allowEmtpy ? "" : null)).Protect(allowEmtpy));

        public ArgumentResult<T, string?> GetString<T>(Expression<Func<T, string?>> expression)
            where T : class, IArguments, new() =>
            new(GetArgument(expression), GetMetaData(expression),
                async (label, value) => await _input.RequestString(label));

        public ArgumentResult<T, bool?> GetBool<T>(Expression<Func<T, bool?>> expression)
            where T : class, IArguments, new() =>
            new(GetArgument(expression), GetMetaData(expression),
                async (label, value) => await _input.PromptYesNo(label, value == true));

        public ArgumentResult<T, long?> GetLong<T>(Expression<Func<T, long?>> expression)
            where T : class, IArguments, new() =>
            new(GetArgument(expression), GetMetaData(expression),
                async (label, value) => {
                    var str = await _input.RequestString(label);
                    if (long.TryParse(str, out var ret))
                    {
                        return ret;
                    }
                    else
                    {
                        _log.Warning("Invalid number: {ret}", ret);
                        return null;
                    }
                });

        protected CommandLineAttribute GetMetaData(LambdaExpression action)
        {
            if (action.Body is MemberExpression expression)
            {
                var property = expression.Member;
                return property.CommandLineOptions();
            }
            else
            {
                throw new NotImplementedException("Unsupported expression");
            }
        }

        /// <summary>
        /// Interactive
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="providedValue"></param>
        /// <param name="what"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        protected P GetArgument<T, P>(Expression<Func<T, P>> action) where T : class, IArguments, new()
        {
            var returnValue = default(P);
            var args = _arguments.GetArguments<T>();
            string? optionName;
            if (args != null)
            {
                if (action.Body is MemberExpression expression)
                {
                    var property = expression.Member;
                    var commandLineOptions = property.CommandLineOptions();
                    optionName = (commandLineOptions.Name ?? property.Name).ToLower();
                    var func = action.Compile();
                    returnValue = func(args);
                }
                else
                {
                    throw new NotImplementedException("Unsupported expression");
                }
            }
            else
            {
                throw new InvalidOperationException($"Missing argumentprovider for type {typeof(T).Name}");
            }

            if (returnValue == null)
            {
                _log.Debug("No value provided for --{optionName}", optionName);
            }
            else
            {
                var censor = ArgumentsParser.CensoredParameters.Any(c => optionName!.Contains(c));
                _log.Debug("Parsed value for --{optionName}: {providedValue}",
                    optionName,
                    censor ? "********" : returnValue);
            }
            return returnValue;
        }
    }
}