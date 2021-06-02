using Fclp;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Configuration
{
    /// <summary>
    /// Default ArgumentsProvider that is brought to life by the 
    /// PluginService for each implementation of IArgumentsStandalone
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseArgumentsProvider<T> : IArgumentsProvider<T> where T : class, IArguments, new()
    {
        /// <summary>
        /// Command line partser internal
        /// </summary>
        private readonly FluentCommandLineParser<T> _parser;
        private readonly IArgumentsProvider<T> _typedThis;

        /// <summary>
        /// Log service
        /// </summary>
        public ILogService? Log { get; set; }

        public BaseArgumentsProvider()
        {
            _typedThis = this;
            _parser = new FluentCommandLineParser<T>
            {
                IsCaseSensitive = false
            };
            BaseArgumentsProvider<T>.Configure(_parser);
        }

        /// <summary>
        /// Configure the command line parser based on metadata in this type
        /// </summary>
        /// <param name="parser"></param>
        private static void Configure(FluentCommandLineParser<T> parser)
        {
            foreach (var (commandLineInfo, property) in typeof(T).CommandLineProperties())
            {
                var setupMethod = typeof(FluentCommandLineParser<T>).GetMethod(nameof(parser.Setup), new[] { typeof(PropertyInfo) });
                if (setupMethod == null)
                {
                    throw new InvalidOperationException();
                }
                var typedMethod = setupMethod.MakeGenericMethod(property.PropertyType);
                var result = typedMethod.Invoke(parser, new[] { property });

                var clob = typeof(ICommandLineOptionBuilderFluent<>).MakeGenericType(property.PropertyType);
                var @as = clob.GetMethod(nameof(ICommandLineOptionBuilderFluent<object>.As), new[] { typeof(string) });
                if (@as == null)
                {
                    throw new InvalidOperationException();
                }
                var asResult = @as.Invoke(result, new[] { (commandLineInfo.Name ?? property.Name).ToLower() });

                // Add description when available
                if (!string.IsNullOrWhiteSpace(commandLineInfo?.Description))
                {
                    var clo = typeof(ICommandLineOptionFluent<>).MakeGenericType(property.PropertyType);
                    var withDescription = clo.GetMethod(nameof(ICommandLineOptionFluent<object>.WithDescription), new[] { typeof(string) });
                    if (withDescription == null)
                    {
                        throw new InvalidOperationException();
                    }
                    withDescription.Invoke(asResult, new[] { commandLineInfo?.Description });
                }

                // Add default when available
                if (!string.IsNullOrWhiteSpace(commandLineInfo?.Default))
                {
                    var clo = typeof(ICommandLineOptionFluent<>).MakeGenericType(property.PropertyType);
                    var setDefault = clo.GetMethod(nameof(ICommandLineOptionFluent<object>.SetDefault), new[] { property.PropertyType });
                    if (setDefault == null)
                    {
                        throw new InvalidOperationException();
                    }
                    setDefault.Invoke(asResult, new[] { commandLineInfo?.Default });
                }
            }
        }

        /// <summary>
        /// Get the parsed result
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        T? IArgumentsProvider<T>.GetResult(string[] args)
        {
            var result = _parser.Parse(args);
            if (result.HasErrors)
            {
                Log?.Error(result.ErrorText);
                return null;
            }
            return _parser.Object;
        }
        object? IArgumentsProvider.GetResult(string[] args) => _typedThis.GetResult(args);

        /// <summary>
        /// Validate the arguments
        /// </summary>
        /// <param name="current"></param>
        /// <param name="main"></param>
        /// <returns></returns>
        bool IArgumentsProvider<T>.Validate(T current, MainArguments main)
        {
            if (main.Renew)
            {
                if (_typedThis.Active(current))
                {
                    Log?.Error($"Renewal {(string.IsNullOrEmpty(_typedThis.Group) ? "" : $"{_typedThis.Group} ")}parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                    return false;
                }
            }
            return true;
        }
        bool IArgumentsProvider.Validate(object current, MainArguments main) => _typedThis.Validate((T)current, main);

        /// <summary>
        /// Get list of unmatched arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        IEnumerable<string> IArgumentsProvider.GetExtraArguments(string[] args) => _parser.Parse(args).AdditionalOptionsFound.Select(x => x.Key);

        /// <summary>
        /// Test if the arguments are activated when they are
        /// not supposed to (e.g. in --renew mode).
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        bool IArgumentsProvider.Active(object current)
        {
            if (current is IArguments standalone)
            {
                return standalone.Active();
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Construct an emtpy instance of T to able to use its properties
        /// </summary>
        private T DefaultInstance
        {
            get
            {
                if (_defaultInstance == null)
                {
                    var type = typeof(T);
                    var constructor = type.GetConstructor(Array.Empty<Type>());
                    if (constructor == null)
                    {
                        throw new InvalidOperationException();
                    }
                    _defaultInstance = (T)constructor.Invoke(null);
                }
                return _defaultInstance;
            }
        }
        private T? _defaultInstance;

        /// <summary>
        /// List of all properties in this argument class
        /// </summary>
        IEnumerable<CommandLineAttribute> IArgumentsProvider.Configuration => typeof(T).CommandLineProperties().Select(cmd => cmd.Item1).OfType<CommandLineAttribute>();
        
        /// <summary>
        /// Name of the arguments group
        /// </summary>
        string IArgumentsGroup.Name => DefaultInstance.Name;

        /// <summary>
        /// Supergroup
        /// </summary>
        string IArgumentsGroup.Group => DefaultInstance.Group;

        /// <summary>
        /// Under which conditions is this block of arguments usable
        /// </summary>
        string? IArgumentsGroup.Condition => DefaultInstance.Condition;

        /// <summary>
        /// Is this a default plugin?
        /// </summary>
        bool IArgumentsGroup.Default => DefaultInstance.Default;
    }
}
