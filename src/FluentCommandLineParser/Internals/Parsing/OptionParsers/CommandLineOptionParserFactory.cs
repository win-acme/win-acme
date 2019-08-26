#region License
// CommandLineOptionParserFactory.cs
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

namespace Fclp.Internals.Parsing.OptionParsers
{
    /// <summary>
    /// 
    /// </summary>
    public class CommandLineOptionParserFactory : ICommandLineOptionParserFactory
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="CommandLineOptionParserFactory"/> class.
        /// </summary>
        public CommandLineOptionParserFactory()
        {
            this.Parsers = new Dictionary<Type, object>();
            this.AddOrReplace(new BoolCommandLineOptionParser());
            this.AddOrReplace(new Int32CommandLineOptionParser());
            this.AddOrReplace(new Int64CommandLineOptionParser());
            this.AddOrReplace(new StringCommandLineOptionParser());
            this.AddOrReplace(new DateTimeCommandLineOptionParser());
            this.AddOrReplace(new TimeSpanCommandLineOptionParser());
            this.AddOrReplace(new DoubleCommandLineOptionParser());
            this.AddOrReplace(new UriCommandLineOptionParser());
            this.AddOrReplace(new ListCommandLineOptionParser<string>(this));
            this.AddOrReplace(new ListCommandLineOptionParser<int>(this));
            this.AddOrReplace(new ListCommandLineOptionParser<long>(this));
            this.AddOrReplace(new ListCommandLineOptionParser<double>(this));
            this.AddOrReplace(new ListCommandLineOptionParser<DateTime>(this));
            this.AddOrReplace(new ListCommandLineOptionParser<TimeSpan>(this));
            this.AddOrReplace(new ListCommandLineOptionParser<bool>(this));
            this.AddOrReplace(new ListCommandLineOptionParser<Uri>(this));
            this.AddOrReplace(new NullableCommandLineOptionParser<bool>(this));
            this.AddOrReplace(new NullableCommandLineOptionParser<int>(this));
            this.AddOrReplace(new NullableCommandLineOptionParser<long>(this));
            this.AddOrReplace(new NullableCommandLineOptionParser<double>(this));
            this.AddOrReplace(new NullableCommandLineOptionParser<DateTime>(this));
            this.AddOrReplace(new NullableCommandLineOptionParser<TimeSpan>(this));
        }

        internal Dictionary<Type, object> Parsers { get; set; }

        /// <summary>
        /// Adds the specified <see cref="ICommandLineOptionParser{T}"/> to this factories list of supported parsers.
        /// If an existing parser has already been registered for the type then it will be replaced.
        /// </summary>
        /// <typeparam name="T">The type which the <see cref="ICommandLineOptionParser{T}"/> will be returned for.</typeparam>
        /// <param name="parser">The parser to return for the specified type.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="parser"/> is <c>null</c>.</exception>
        public void AddOrReplace<T>(ICommandLineOptionParser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException("parser");

            var parserType = typeof(T);

            // remove existing
            this.Parsers.Remove(parserType);

            this.Parsers.Add(parserType, parser);
        }

        /// <summary>
        /// Creates a <see cref="ICommandLineOptionParser{T}"/> to handle the specified type.
        /// </summary>
        /// <typeparam name="T">The type of parser to create.</typeparam>
        /// <returns>A <see cref="ICommandLineOptionParser{T}"/> suitable for the specified type.</returns>
        /// <exception cref="UnsupportedTypeException">If the specified type is not supported by this factory.</exception>
        public ICommandLineOptionParser<T> CreateParser<T>()
        {
            var type = typeof(T);

            if (!this.Parsers.ContainsKey(type))
            {
                if (!TryAddAsSpecialParser<T>(type))
                {
                    throw new UnsupportedTypeException();
                }
            }

            return (ICommandLineOptionParser<T>)this.Parsers[type];
        }

        /// <summary>
        /// Attempts to add a special case parser, such as Enum or List{TEnum} parser.
        /// </summary>
        /// <returns>True if a special parser was added for the type; otherwise false.</returns>
        private bool TryAddAsSpecialParser<T>(Type type)
        {
            if (type.IsEnum)
            {
                bool hasFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);

                Type enumParserType = hasFlags ?
                    typeof(EnumFlagCommandLineOptionParser<T>) :
                    typeof(EnumCommandLineOptionParser<T>);

                if (!this.Parsers.ContainsKey(enumParserType))
                {
                    if (hasFlags)
                    {
                        this.AddOrReplace(new EnumFlagCommandLineOptionParser<T>());
                    }
                    else
                    {
                        this.AddOrReplace(new EnumCommandLineOptionParser<T>());
                    }

                }
                return true;
            }

            if (type.IsGenericType)
            {
                var genericType = TryGetListGenericType(type);

                if (genericType != null)
                {
                    if (genericType.IsEnum || IsNullableEnum(genericType))
                    {
                        var enumListParserType = typeof(ListCommandLineOptionParser<>).MakeGenericType(genericType);
                        var parser = (ICommandLineOptionParser<T>)Activator.CreateInstance(enumListParserType, this);

                        if (!this.Parsers.ContainsKey(type))
                        {
                            this.AddOrReplace(parser);
                        }

                        return true;
                    }
                }
            }

            if (IsNullableEnum(type))
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                var nullableEnumParserType = typeof(NullableEnumCommandLineOptionParser<>).MakeGenericType(underlyingType);
                var parser = (ICommandLineOptionParser<T>)Activator.CreateInstance(nullableEnumParserType, this);

                if (!this.Parsers.ContainsKey(type))
                {
                    this.AddOrReplace(parser);
                }

                return true;
            }

            return false;
        }

        private static bool IsNullableEnum(Type t)
        {
            Type u = Nullable.GetUnderlyingType(t);
            return (u != null) && u.IsEnum;
        }

        /// <summary>
        /// Attemps to get the type of generic from a generic list.
        /// </summary>
        private static Type TryGetListGenericType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition()
                == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }

            return null;
        }
    }
}