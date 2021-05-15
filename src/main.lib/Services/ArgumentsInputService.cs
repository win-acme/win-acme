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
        private readonly ArgumentsParser _arguments;
        private readonly IInputService _input;
        private readonly SecretServiceManager _secretService;

        public ArgumentsInputService(
            ILogService log,
            ArgumentsParser arguments,
            IInputService input,
            SecretServiceManager secretService)
        {
            _log = log;
            _arguments = arguments;
            _input = input;
            _secretService = secretService;
        }
        public ArgumentResult<ProtectedString?> GetProtectedString<T>(Expression<Func<T, string?>> expression, bool allowEmtpy = false)
            where T : class, IArguments,
            new() => new(GetArgument(expression).Protect(allowEmtpy), GetMetaData(expression),
                async (args) => (await _secretService.GetSecret(args.Label, args.Default?.Value, allowEmtpy ? "" : null, args.Required, args.Multiline)).Protect(allowEmtpy),
                _log, allowEmtpy);

        public ArgumentResult<string?> GetString<T>(Expression<Func<T, string?>> expression)
            where T : class, IArguments, new() =>
            new(GetArgument(expression), GetMetaData(expression),
                async (args) => await _input.RequestString(args.Label), _log);

        public ArgumentResult<bool?> GetBool<T>(Expression<Func<T, bool?>> expression)
            where T : class, IArguments, new() =>
            new(GetArgument(expression), GetMetaData(expression),
                async (args) => await _input.PromptYesNo(args.Label, args.Default == true), _log);

        public ArgumentResult<long?> GetLong<T>(Expression<Func<T, long?>> expression)
            where T : class, IArguments, new() => 
            new(GetArgument(expression), GetMetaData(expression),
                async (args) => {
                    var str = await _input.RequestString(args.Label);
                    if (long.TryParse(str, out var ret))
                    {
                        return ret;
                    }
                    else
                    {
                        _log.Warning("Invalid number: {ret}", ret);
                        return null;
                    }
                }, _log);

        public ArgumentResult<int?> GetInt<T>(Expression<Func<T, int?>> expression)
            where T : class, IArguments, new() =>
            new(GetArgument(expression), GetMetaData(expression),
                async (args) => {
                    var str = await _input.RequestString(args.Label);
                    if (int.TryParse(str, out var ret))
                    {
                        return ret;
                    }
                    else
                    {
                        _log.Warning("Invalid number: {ret}", ret);
                        return null;
                    }
                }, _log);

        protected static CommandLineAttribute GetMetaData(LambdaExpression action)
        {
            if (action.Body is MemberExpression member)
            {
                var property = member.Member;
                return property.CommandLineOptions();
            }
            else if (action.Body is UnaryExpression unary)
            {
                if (unary.Operand is MemberExpression unaryMember)
                {
                    return unaryMember.Member.CommandLineOptions();
                }
            }
            throw new NotImplementedException("Unsupported expression");
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
            if (args != null)
            {
                var func = action.Compile();
                returnValue = func(args);
            }
            else
            {
                throw new InvalidOperationException($"Missing argumentprovider for type {typeof(T).Name}");
            }
            var optionName = GetMetaData(action).ArgumentName;
            if (returnValue == null)
            {
                _log.Debug("No value provided for {optionName}", $"--{optionName}");
            }
            else
            {
                var censor = ArgumentsParser.CensoredParameters.Any(c => optionName!.Contains(c));
                if (returnValue is string returnString && string.IsNullOrWhiteSpace(returnString)) 
                {
                    _log.Debug("Parsed emtpy value for {optionName}", $"--{optionName}");
                } 
                else
                {
                    _log.Debug("Parsed value for {optionName}: {providedValue}", $"--{optionName}", censor ? "********" : returnValue);
                }
            }
            return returnValue;
        }
    }
}