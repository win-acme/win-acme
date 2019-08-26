#region License
// CommandLineOptionGrouper.cs
// Copyright (c) 2013, Simon Williams
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provide
// d that the following conditions are met:
// 
// Redistributions of source code must retain the above copyright notice, this list of conditions and the
// following disclaimer.
// 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
// the following disclaimer in the documentation and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED 
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Fclp.Internals.Extensions;

namespace Fclp.Internals.Parsing
{
    /// <summary>
    /// Organises arguments into group defined by their associated Option.
    /// </summary>
    public class CommandLineOptionGrouper
    {
        private readonly SpecialCharacters _specialCharacters;
        private string[] _args;
        private int _currentOptionLookupIndex;
        private int[] _foundOptionLookup;
        private int _currentOptionIndex;
        private readonly List<string> _orphanArgs;
        private bool _parseCommands = false;

        /// <summary>
        /// Initialises a new instance of <see cref="CommandLineOptionGrouper"/>.
        /// </summary>
	    public CommandLineOptionGrouper(SpecialCharacters specialCharacters)
        {
            _specialCharacters = specialCharacters;
            _orphanArgs = new List<string>();
        }

        /// <summary>
        /// Groups the specified arguments by the associated Option.
        /// </summary>
        public string[][] GroupArgumentsByOption(string[] args, bool parseCommands)
        {
            if (args.IsNullOrEmpty()) return new string[0][];

            _parseCommands = parseCommands;

            _args = args;

            _currentOptionIndex = -1;
            _currentOptionLookupIndex = -1;

            var options = new List<string[]>();

            var first = _args.First();

            if (IsEndOfOptionsKey(first))
            {
                options.Add(CreateGroupForCurrent());
            }
            else
            {
                if (parseCommands && IsACmd(first))
                {
                    if (ContainsAtLeastOneOption(args))
                    {
                        options.Add(new[] {first});

                        FindOptionIndexes();

                        while (MoveToNextOption())
                        {
                            options.Add(CreateGroupForCurrent());
                        }
                    }
                    else
                    {
                        options.Add(CreateGroupForCurrent());
                    }
                }
                else
                {
                    FindOptionIndexes();

                    while (MoveToNextOption())
                    {
                        options.Add(CreateGroupForCurrent());
                    }
                }
            }

            if (_orphanArgs.Any())
            {
                if (options.Count > 0)
                {
                    options.Insert(1, _orphanArgs.ToArray());
                }
                else
                {
                    options.Add(_orphanArgs.ToArray());
                }
            }

            return options.ToArray();
        }

        private bool ContainsAtLeastOneOption(string[] args)
        {
            return args.Any(IsAKey);
        }

        private string[] CreateGroupForCurrent()
        {
            var optionEndIndex = LookupTheNextOptionIndex();

            optionEndIndex = optionEndIndex != -1
				? optionEndIndex - 1
				: _args.Length - 1;

			var length = optionEndIndex - (_currentOptionIndex - 1);

            return _args.Skip(_currentOptionIndex)
                        .Take(length)
                        .ToArray();
        }

        private void FindOptionIndexes()
        {
            var indexes = new List<int>();
            var insideQuote = false;
            for (int index = 0; index < _args.Length; index++)
            {
                string currentArg = _args[index];

                if (IsEndOfOptionsKey(currentArg)) break;
                if(_parseCommands && index == 0 && IsACmd(currentArg)) continue;
                if (insideQuote || !IsAKey(currentArg))
                {
                    if (indexes.Count == 0)
                    {
                        _orphanArgs.Add(currentArg);
                    }
                    if (!insideQuote && currentArg.StartsWith("\"") && !currentArg.EndsWith("\""))
                    {
                        insideQuote = true;
                    }
                    else if (insideQuote && currentArg.EndsWith("\""))
                    {
                        insideQuote = false;
                    }
                    continue;
                };
                insideQuote = false;
                indexes.Add(index);
            }

            _foundOptionLookup = indexes.ToArray();
        }

        private bool MoveToNextOption()
        {
            var nextIndex = LookupTheNextOptionIndex();
            if (nextIndex == -1) return false;

            _currentOptionLookupIndex += 1;
            _currentOptionIndex = nextIndex;

            return true;
        }

        private int LookupTheNextOptionIndex()
        {
            return _foundOptionLookup.ElementAtOrDefault(_currentOptionLookupIndex + 1, -1);
        }

        /// <summary>
        /// Gets whether the specified <see cref="System.String"/> is a Option key.
        /// </summary>
        /// <param name="arg">The <see cref="System.String"/> to examine.</param>
        /// <returns><c>true</c> if <paramref name="arg"/> is a Option key; otherwise <c>false</c>.</returns>
        private bool IsAKey(string arg)
        {
            return arg != null && _specialCharacters.OptionPrefix.Any(arg.StartsWith);
        }

        private bool IsACmd(string arg)
        {
            return arg != null && _specialCharacters.OptionPrefix.Any(arg.StartsWith) == false;
        }

        /// <summary>
        /// Determines whether the specified string indicates the end of parsed options.
        /// </summary>
        private bool IsEndOfOptionsKey(string arg)
        {
            return string.Equals(arg, _specialCharacters.EndOfOptionsKey, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}