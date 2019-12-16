using Fclp;
using Fclp.Internals;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Configuration
{
    public abstract class BaseArgumentsProvider<T> : IArgumentsProvider<T> where T : class, new()
    {
        private readonly FluentCommandLineParser<T> _parser;

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
        bool IArgumentsProvider.Active(object current) => IsActive(current);

        private bool IsActive(object current)
        {
            foreach (var prop in current.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(bool) && (bool)prop.GetValue(current) == true)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)prop.GetValue(current)))
                {
                    return true;
                }
                if (prop.PropertyType == typeof(int) && (int)prop.GetValue(current) > 0)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(int?) && (int?)prop.GetValue(current) != null)
                {
                    return true;
                }
                if (prop.PropertyType == typeof(long) && (long)prop.GetValue(current) > 0)
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

        public virtual bool Validate(ILogService log, T current, MainArguments main)
        {
            var active = IsActive(current);
            if (main.Renew && active)
            {
                log.Error($"{Group} parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                return false;
            }
            else
            {
                return true;
            }
        }

        bool IArgumentsProvider.Validate(ILogService log, object current, MainArguments main) => Validate(log, (T)current, main);

        public IEnumerable<ICommandLineOption> Configuration => _parser.Options;

        public ICommandLineParserResult GetParseResult(string[] args) => _parser.Parse(args);

        public T GetResult(string[] args)
        {
            _parser.Parse(args);
            return _parser.Object;
        }
        object IArgumentsProvider.GetResult(string[] args) => GetResult(args);

    }
}