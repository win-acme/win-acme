using Fclp;
using Fclp.Internals;
using PKISharp.WACS.Services;
using System.Collections.Generic;

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
        public abstract void Configure(FluentCommandLineParser<T> parser);
        bool IArgumentsProvider.Active(object current)
        {
            if (current is T typed)
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
                if (IsActive(current))
                {
                    Log?.Error($"Renewal {(string.IsNullOrEmpty(Group)?"":$"{Group} ")}parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                    return false;
                }
            }
            return true;
        }

        bool IArgumentsProvider.Validate(object current, MainArguments main) => Validate((T)current, main);

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