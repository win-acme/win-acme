using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Configuration
{
    public class ArgumentsParser
    {
        private readonly ILogService _log;
        private readonly string[] _args;
        private readonly List<IArgumentsProvider> _providers;

        public T GetArguments<T>() where T : new()
        {
            foreach (var provider in _providers)
            {
                if (provider is IArgumentsProvider<T>)
                {
                    return ((IArgumentsProvider<T>)provider).GetResult(_args);
                }
            }
            return default;
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
            var superset = _providers.SelectMany(x => x.Configuration);
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

            // Run indivual result validations
            var main = GetArguments<MainArguments>();
            var mainProvider = _providers.OfType<IArgumentsProvider<MainArguments>>().First();
            if (mainProvider.Validate(_log, main, main))
            {
                // Validate the others
                var others = _providers.Except(new[] { mainProvider });
                foreach (var other in others)
                {
                    var opt = other.GetResult(_args);
                    if (!other.Validate(_log, opt, main))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Test if any arguments are active, so that we can warn users
        /// that these arguments have no effect on renewals.
        /// </summary>
        /// <returns></returns>
        public bool Active()
        {
            var mainProvider = _providers.OfType<IArgumentsProvider<MainArguments>>().First();
            var others = _providers.Except(new[] { mainProvider });
            foreach (var other in others)
            {
                var opt = other.GetResult(_args);
                if (other.Active(opt))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Show current command line
        /// </summary>
        internal void ShowCommandLine() => _log.Verbose($"Arguments: {string.Join(" ", _args)}");

        /// <summary>
        /// Show command line arguments for the help function
        /// </summary>
        internal void ShowArguments()
        {
            Console.WriteLine();
            foreach (var providerGroup in _providers.GroupBy(p => p.Group).OrderBy(g => g.Key))
            {
                if (!string.IsNullOrEmpty(providerGroup.Key))
                {
                    Console.WriteLine($"# {providerGroup.Key}");
                    Console.WriteLine();
                }

                foreach (var provider in providerGroup)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"## {provider.Name}");
                    Console.ResetColor();
                    if (!string.IsNullOrEmpty(provider.Condition))
                    {
                        Console.Write($"``` [{provider.Condition}] ```");
                        if (provider.Default)
                        {
                            Console.WriteLine(" (default)");
                        }
                        else
                        {
                            Console.WriteLine();
                        }
                    }
                    Console.WriteLine("```");
                    foreach (var x in provider.Configuration)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"   --{x.LongName}");
                        Console.WriteLine();
                        Console.ResetColor();
                        var step = 60;
                        var pos = 0;
                        var words = x.Description.Split(' ');
                        while (pos < words.Length)
                        {
                            var line = "";
                            while (pos < words.Length && line.Length + words[pos].Length + 1 < step)
                            {
                                line += " " + words[pos++];
                            }
                            if (!Console.IsOutputRedirected)
                            {
                                Console.SetCursorPosition(3, Console.CursorTop);
                            }
                            Console.WriteLine($" {line}");
                        }
                        Console.WriteLine();
                    }
                    Console.WriteLine("```");
                }
            }
        }
    }
}