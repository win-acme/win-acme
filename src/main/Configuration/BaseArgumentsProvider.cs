using Fclp;
using Fclp.Internals;
using PKISharp.WACS.Services.Interfaces;
using System.Collections.Generic;

namespace PKISharp.WACS.Configuration
{
    public abstract class BaseArgumentsProvider<T> : IArgumentsProvider<T> where T : new()
    {
        private FluentCommandLineParser<T> _parser;
        private ICommandLineParserResult _result;

        public BaseArgumentsProvider()
        {
            _parser = new FluentCommandLineParser<T>();
            _parser.IsCaseSensitive = false;
            Configure(_parser);
        }

        public abstract string Name { get; }
        public abstract void Configure(FluentCommandLineParser<T> parser);

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