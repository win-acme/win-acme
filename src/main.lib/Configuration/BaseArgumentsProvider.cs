using Fclp;
using Fclp.Internals;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Configuration
{
    public abstract class BaseArgumentsProvider<T> : IArgumentsProvider<T> where T : class, new()
    {
        private readonly FluentCommandLineParser<T> _parser;
        public ILogService? Log { get; set; }

        public BaseArgumentsProvider()
        {
            _parser = new FluentCommandLineParser<T>
            {
                IsCaseSensitive = false
            };
            Configure(_parser);
        }

        public abstract string Name { get; }
        public abstract string Group { get; }
        public virtual string? Condition { get; }
        public virtual bool Default => false;
        public virtual void Configure(FluentCommandLineParser<T> parser)
        {
            var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            foreach (var property in allProperties)
            {
                var declaringType = property.GetSetMethod()?.GetBaseDefinition().DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }
                var isLocal = declaringType == typeof(T) || declaringType.IsAbstract;
                if (!isLocal)
                {
                    continue;
                }
                var commandLineInfo = property.CommandLineOptions();
                var setupMethod = typeof(FluentCommandLineParser<T>).GetMethod(nameof(parser.Setup),  new[] { typeof(PropertyInfo) });
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
                var asResult = @as.Invoke(result, new[] { commandLineInfo?.Name ?? property.Name.ToLower() });
                
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

        bool IArgumentsProvider.Active(object current)
        {
            if (current is IArgumentsStandalone standalone)
            {
                return standalone.Active();
            }
            else if (current is T typed)
            {
                return IsActive(typed);
            }
            else
            {
                return false;
            }
        }

        protected virtual bool IsActive(T current)
        {
            foreach (var prop in current.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(bool) && (bool)(prop.GetValue(current) ?? false) == true)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)(prop.GetValue(current) ?? string.Empty)))
                {
                    return true;
                }
                if (prop.PropertyType == typeof(int) && (int)(prop.GetValue(current) ?? 0) > 0)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(int?) && (int?)prop.GetValue(current) != null)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(long) && (long)(prop.GetValue(current) ?? 0) > 0)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(long?) && (long?)prop.GetValue(current) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool Validate(T current, MainArguments main)
        {
            if (main.Renew)
            {
                if (((IArgumentsProvider<T>)this).Active(current))
                {
                    Log?.Error($"Renewal {(string.IsNullOrEmpty(Group)?"":$"{Group} ")}parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                    return false;
                }
            }
            return true;
        }

        bool IArgumentsProvider.Validate(object current, MainArguments main)
        {
            if (current is IArgumentsStandalone standalone)
            {
                return standalone.Validate(main, Log);
            }
            else if (current is T typed)
            {
                return Validate(typed, main);
            }
            else
            {
                return false;
            }
        }


        public IEnumerable<ICommandLineOption> Configuration => _parser.Options;

        public ICommandLineParserResult GetParseResult(string[] args) => _parser.Parse(args);

        public T? GetResult(string[] args)
        {
            var result = _parser.Parse(args);
            if (result.HasErrors)
            {
                Log?.Error(result.ErrorText);
                return null;
            }
            return _parser.Object;
        }
        object? IArgumentsProvider.GetResult(string[] args) => GetResult(args);

    }
}