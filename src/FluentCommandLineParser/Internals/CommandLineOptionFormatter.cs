#region License
// CommandLineOptionFormatter.cs
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
using System.Globalization;
using System.Linq;
using System.Text;
using Fclp.Internals.Extensions;

namespace Fclp.Internals
{
	/// <summary>
	/// Simple default formatter used to display command line options to the user.
	/// </summary>
	public class CommandLineOptionFormatter : ICommandLineOptionFormatter
	{
		#region Constructors

		/// <summary>
		/// Initialises a new instance of the <see cref="CommandLineOptionFormatter"/> class.
		/// </summary>
		public CommandLineOptionFormatter()
		{
			this.ValueText = "Value";
			this.DescriptionText = "Description";
			this.NoOptionsText = "No options have been setup";
		}

		#endregion

		/// <summary>
		/// The text format used in this formatter.
		/// </summary>
		public const string TextFormat = "\t{0}\t\t{1}\n";

		/// <summary>
		/// If true, outputs a header line above the option list. If false, the header is omitted. Default is true.
		/// </summary>
		private bool ShowHeader
		{
			get { return Header != null; }
		}

		/// <summary>
		/// Gets or sets the header to display before the printed options.
		/// </summary>
		public string Header { get; set; }

		/// <summary>
		/// Gets or sets the text to use as <c>Value</c> header. This should be localised for the end user.
		/// </summary>
		public string ValueText { get; set; }

		/// <summary>
		/// Gets or sets the text to use as the <c>Description</c> header. This should be localised for the end user.
		/// </summary>
		public string DescriptionText { get; set; }

		/// <summary>
		/// Gets or sets the text to use when there are no options. This should be localised for the end user.
		/// </summary>
		public string NoOptionsText { get; set; }

		/// <summary>
		/// Formats the list of <see cref="ICommandLineOption"/> to be displayed to the user.
		/// </summary>
		/// <param name="options">The list of <see cref="ICommandLineOption"/> to format.</param>
		/// <returns>A <see cref="System.String"/> representing the format</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="options"/> is <c>null</c>.</exception>
		public string Format(IEnumerable<ICommandLineOption> options)
		{
			if (options == null) throw new ArgumentNullException("options");

			var list = options.ToList();

			if (!list.Any()) return this.NoOptionsText;

			var sb = new StringBuilder();
			sb.AppendLine();

			// add headers first
			if (ShowHeader)
			{
				sb.AppendLine(Header);
				sb.AppendLine();
			}

			var ordered = (from option in list
						  orderby option.ShortName.IsNullOrWhiteSpace() == false descending , option.ShortName
						  select option).ToList();

			foreach (var cmdOption in ordered)
				sb.AppendFormat(CultureInfo.CurrentUICulture, TextFormat, FormatValue(cmdOption), cmdOption.Description);

			return sb.ToString();
		}

		/// <summary>
		/// Formats the short and long names into one <see cref="System.String"/>.
		/// </summary>
		static string FormatValue(ICommandLineOption cmdOption)
		{
			if (cmdOption.ShortName.IsNullOrWhiteSpace())
			{
				return cmdOption.LongName;
			}
			
			if (cmdOption.LongName.IsNullOrWhiteSpace())
			{
				return cmdOption.ShortName;
			}

			return cmdOption.ShortName + ":" + cmdOption.LongName;
		}
		// string = [-|/]f[:|=| ]|[-|/|--]filename[:|=| ] value 
	}
}
