#region License
// ParsedOption.cs
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
namespace Fclp.Internals.Parsing
{
	/// <summary>
	/// Contains information about a single parsed option and any value.
	/// </summary>
	public class ParsedOption
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="ParsedOption"/> class.
		/// </summary>
		/// <param name="key">The command line option key.</param>
		/// <param name="value">The value matched with the key.</param>
		public ParsedOption(string key, string value)
		{
			Key = key;
			Value = value;
		}

		/// <summary>
		/// Initialises a new instance of the <see cref="ParsedOption"/> class.
		/// </summary>
		public ParsedOption()
		{
		}

		/// <summary>
		/// Gets the raw key representing this option.
		/// </summary>
		public string RawKey { get; set; }

		/// <summary>
		/// Gets or sets the command line option key.
		/// </summary>
		public string Key { get; set; }

		/// <summary>
		/// Gets or sets the first value matched with the key.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Gets or sets all the values matched with this key.
		/// </summary>
		public string[] Values { get; set; }

		/// <summary>
		/// Gets or sets the additional values matched with this key.
		/// </summary>
		public string[] AdditionalValues { get; set; }

		/// <summary>
		/// Gets or sets the prefix for the key e.g. -, / or --.
		/// </summary>
		public string Prefix { get; set; }

		/// <summary>
		/// Gets or sets any suffix for the key e.g. boolean arguments with +, -.
		/// </summary>
		public string Suffix { get; set; }

        internal ICommandLineOption SetupCommand { get; set; }

        internal int SetupOrder { get; set; }

        internal int Order { get; set; }

        /// <summary>
        /// Gets whether this parsed option has a value set.
        /// </summary>
        public bool HasValue
		{
			get { return string.IsNullOrEmpty(Value) == false; }
		}

		/// <summary>
		/// Gets whether this parsed options has a suffix.
		/// </summary>
		public bool HasSuffix
		{
			get { return string.IsNullOrEmpty(Suffix) == false; }
		}

		/// <summary>
		/// Determines whether two specified <see cref="ParsedOption"/> objects have the same values.
		/// </summary>
		/// <param name="other">The other <see cref="ParsedOption"/> to compare.</param>
		/// <returns>true if they are equal; otherwise false.</returns>
		protected bool Equals(ParsedOption other)
		{
			return string.Equals(Key, other.Key) && string.Equals(Value, other.Value);
		}

		/// <summary>
		/// Determines whether this <see cref="ParsedOption"/> is equal to the specified <see cref="System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare.</param>
		/// <returns>true if they are equal; otherwise false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((ParsedOption) obj);
		}

		/// <summary>
		/// Returns the hashcode for this instance.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return ((Key != null ? Key.GetHashCode() : 0)*397) ^ (Value != null ? Value.GetHashCode() : 0);
			}
		}

		/// <summary>
		/// Creates a clone of this option.
		/// </summary>
		/// <returns></returns>
		public ParsedOption Clone()
		{
			return new ParsedOption
			{
				Key = Key,
				Prefix = Prefix,
				Suffix = Suffix,
				Value = Value,
				AdditionalValues = AdditionalValues,
				RawKey = RawKey,
				Values = Values
			};
		}
	}
}