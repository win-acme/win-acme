using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Configuration
{
    public class ArgumentsParser
    {
        private ILogService _log;
        private string[] _args;
        private List<IArgumentsProvider> _providers;

        public T GetArguments<T>() where T : new()
        {
            foreach (var provider in _providers) {
                if (provider is IArgumentsProvider<T>)
                {
                    return ((IArgumentsProvider<T>)provider).GetResult(_args);
                }
            }
            return default(T);
        }

        public ArgumentsParser(ILogService log, PluginService plugins, string[] args)
        {
            _log = log;
            _args = args;
            _providers = plugins.OptionProviders();
        }

        internal bool Validate()
        {
            // Test if the arguments can be resolved by any of the known providers
            var superset = _providers.Skip(1).SelectMany(x => x.Configuration);
            var result = _providers.First().GetParseResult(_args);
            foreach (var add in result.AdditionalOptionsFound)
            {
                var super = superset.FirstOrDefault(x => string.Equals(x.LongName, add.Key, StringComparison.CurrentCultureIgnoreCase));
                if (super == null)
                {
                    _log.Error("Unknown argument --{0}", add.Key);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Show command line arguments for the help function
        /// </summary>
        internal void ShowArguments()
        {
            foreach (var provider in _providers)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                Console.WriteLine(" --------------------------------");
                Console.WriteLine($" {provider.Name}");
                Console.WriteLine(" --------------------------------");
                Console.WriteLine();
                foreach (var x in provider.Configuration)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($" --{x.LongName}");
                    Console.ResetColor();
                    Console.WriteLine(":");
                    var step = 60;
                    var pos = 0;
                    var words = x.Description.Split(' ');
                    while (pos < words.Length)
                    {
                        var line = "";
                        while (pos < words.Length && line.Length + words[pos].Length + 1 < step)
                        {
                            line += " " + words[pos++];
                            if (line.EndsWith("]"))
                            {
                                break;
                            }
                        }
                        Console.SetCursorPosition(3, Console.CursorTop);
                        Console.WriteLine($" {line}");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}