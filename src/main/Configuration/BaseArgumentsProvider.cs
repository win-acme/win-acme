using Fclp;
using Fclp.Internals;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;
using System.Collections.Generic;

namespace PKISharp.WACS.Configuration
{
    public abstract class BaseArgumentsProvider<T> : IArgumentsProvider<T> where T : new()
    {
        private FluentCommandLineParser<T> _parser;

        public BaseArgumentsProvider()
        {
            _parser = new FluentCommandLineParser<T>();
            _parser.IsCaseSensitive = false;
            Configure(_parser);
        }

        public abstract string Name { get; }
        public abstract string Group { get; }
        public abstract string Condition { get; }
        public virtual bool Default => false;
        public abstract void Configure(FluentCommandLineParser<T> parser);
        public abstract bool Active(T current);
        bool IArgumentsProvider.Active(object current) => Active((T)current);

        public virtual bool Validate(ILogService log, T current, MainArguments main)
        {
            var active = Active(current);
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

        public ICommandLineParserResult GetParseResult(string[] args)
        {
            return _parser.Parse(args);
        }

        public T GetResult(string[] args)
        {
            _parser.Parse(args);
            return _parser.Object;
        }
        object IArgumentsProvider.GetResult(string[] args) => GetResult(args);

    }
}