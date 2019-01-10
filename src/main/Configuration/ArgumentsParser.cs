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
            var providers = plugins.OptionProviders();
            var main = providers.OfType<IArgumentsProvider<MainArguments>>().First();
            _providers = new List<IArgumentsProvider>();
            _providers.Add(main);
            _providers.AddRange(providers.Except(new[] { main }));
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
        /// Show command line arguments for the help function
        /// </summary>
        internal void ShowArguments()
        {
            Console.WriteLine();
            foreach (var providerGroup in _providers.GroupBy(p => p.Group).OrderBy(g => g.Key))
            {
                if (!string.IsNullOrEmpty(providerGroup.Key))
                {
                    Console.WriteLine($" {providerGroup.Key}");
                    Console.WriteLine(" -----------------------------------");
                    Console.WriteLine();
                }

                foreach (var provider in providerGroup)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"   {provider.Name}");
                    Console.ResetColor();
                    if (!string.IsNullOrEmpty(provider.Condition))
                    {
                        Console.WriteLine($"   [{provider.Condition}]");
                    }
                    Console.WriteLine("   -----------------------------------");
                    Console.WriteLine();
                    foreach (var x in provider.Configuration)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"   --{x.LongName}");
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
                    Console.WriteLine();
                }
            }
        }
    }
}