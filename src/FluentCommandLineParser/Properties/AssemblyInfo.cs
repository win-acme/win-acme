#region License
// AssemblyInfo.cs
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Fluent Command Line Parser")]
[assembly: AssemblyDescription("A simple, strongly typed .NET C# command line parser library using a fluent easy to use interface.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Fluent Command Line Parser")]
[assembly: AssemblyCopyright("Copyright © Simon Williams 2012 - 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(true)]
// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b563a988-ae88-4bcc-b6ab-e167f941a167")]

// Allows us to unit test the 'internals'
//[assembly: InternalsVisibleTo("FluentCommandLineParser.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100ad2e88658ff62cac7d7ececd3ac22c6d39ee4e19a09d61acae936f9497df38fa3db020d2b607c8176cd754c7e3a8cdc10559bedcbaaeed76e277f0d009b39bab687261567a1f2da2c3d63b913822ee944664e29bcb85d6b49b87c7d6ee44647ec5252379ed5e4c09d787f6753cf2fdf4a1c1890eedc655738d466bb6f3b91396")]
[assembly: InternalsVisibleTo("FluentCommandLineParser.Tests")]
// !! DO NOT CHANGE - VERSIONS ARE HANDLED AUTOMATICALLY FROM THE CONTINUOUS INTEGRATION SERVER!!
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
