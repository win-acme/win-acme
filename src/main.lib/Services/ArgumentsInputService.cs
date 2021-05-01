using PKISharp.WACS.Configuration;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace PKISharp.WACS.Services
{
    public class ArgumentsInputService
    {
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly IArgumentsService _arguments;
        private readonly SecretServiceManager _secretServiceManager;

        public ArgumentsInputService(
            ILogService log,
            IInputService input,
            IArgumentsService arguments,
            SecretServiceManager secrets)
        {
            _log = log;
            _input = input;
            _arguments = arguments;
            _secretServiceManager = secrets;
        }

        /// <summary>
        /// Interactive
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="providedValue"></param>
        /// <param name="what"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public P? GetArgument<T, P>(
            Expression<Func<T, P?>> action,
            bool required = false)
            where T : class, new()
        {
            var ret = default(P);
            var args = _arguments.GetArguments<T>();
            string? optionName;
            if (args != null)
            {
                if (action.Body is MemberExpression expression)
                {
                    // TODO: process FluentCommandLineParser metadata
                    optionName = expression.Member.Name.ToLower();
                    var func = action.Compile();
                    ret = func(args);
                }
                else
                {
                    throw new NotImplementedException("Unsupported expression");
                }
            }
            else
            {
                throw new InvalidOperationException("Missing/invalid arguments");
            }

            if (ret == null)
            {
                if (required)
                {
                    _log.Error("Missing value for --{optionName}", optionName);
                    throw new Exception($"Missing value for --{optionName}");
                }
                else
                {
                    _log.Debug("No value provided for --{optionName}", optionName);
                }
            }
            else
            {
                //var censor = ArgumentsParser.CensoredParameters.Any(c => optionName!.Contains(c));
                //_log.Debug("Using value for --{optionName}: {providedValue}",
                //    optionName,
                //    censor ? new string('*', ret.ToString()?.Length ?? 1) : ret);
            }

            return ret;
        }
    }
}


