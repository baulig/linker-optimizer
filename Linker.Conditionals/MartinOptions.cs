//
// MartinOptions.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Conditionals
{
	public class MartinOptions
	{
		public bool ScanAllModules {
			get; set;
		}

		public bool AnalyzeAll {
			get; set;
		}

		public bool Preprocess {
			get; set;
		}

		public bool NoConditionalRedefinition {
			get; set;
		}

		public IList<string> DebugTypes {
			get;
		}

		public IList<string> DebugMethods {
			get;
		}

		public IList<string> FailOnTypes {
			get;
		}

		public IList<string> FailOnMethods {
			get;
		}

		public MartinOptions ()
		{
			NoConditionalRedefinition = true;
			DebugTypes = new List<string> ();
			DebugMethods = new List<string> ();
			FailOnTypes = new List<string> ();
			FailOnMethods = new List<string> ();
		}

		bool DontDebugThis (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return DontDebugThis (type.DeclaringType);

			switch (type.FullName) {
			case "Martin.LinkerTest.TestHelpers":
			case "Martin.LinkerTest.AssertionException":
				return true;
			default:
				return false;
			}
		}

		public bool EnableDebugging (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return EnableDebugging (type.DeclaringType);

			if (DontDebugThis (type))
				return false;

			if (type.Namespace == "Martin.LinkerTest")
				return true;
			if (type.Module.Assembly.Name.Name.ToLowerInvariant ().Contains ("martin"))
				return true;

			return DebugTypes.Any (t => type.FullName.Contains (t));
		}

		public bool EnableDebugging (MethodDefinition method)
		{
			if (DontDebugThis (method.DeclaringType))
				return false;
			if (EnableDebugging (method.DeclaringType))
				return true;
			if (method.Name == "Main")
				return true;
			if (method.FullName.Contains ("Martin"))
				return true;

			return DebugMethods.Any (m => method.FullName.Contains (m));
		}

		public bool FailOnType (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return FailOnType (type.DeclaringType);

			return FailOnTypes.Any (t => type.FullName.Contains (t));
		}

		public bool FailOnMethod (MethodDefinition method)
		{
			if (FailOnType (method.DeclaringType))
				return true;

			return FailOnMethods.Any (m => method.FullName.Contains (m));
		}

		public void CheckFailList (MartinContext context, TypeDefinition type, string original = null)
		{
			if (type.DeclaringType != null) {
				CheckFailList (context, type.DeclaringType, original ?? type.FullName);
				return;
			}

			var fail = FailOnTypes.FirstOrDefault (t => type.FullName.Contains (t));
			if (fail == null)
				return;

			var original_message = original != null ? $" while parsing `{original}`" : string.Empty;
			var message = $"Found type `{type.FullName}`{original_message}, which matches fail-list entry `{fail}.";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message);
			context.Context.Tracer.Dump ();
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			throw new NotSupportedException (message);
		}

		public void CheckFailList (MartinContext context, MethodDefinition method)
		{
			CheckFailList (context, method.DeclaringType, method.FullName);

			var fail = FailOnMethods.FirstOrDefault (m => method.FullName.Contains (m));
			if (fail == null)
				return;

			var message = $"Found method `{method.FullName}`, which matches fail-list entry `{fail}.";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message);
			context.Context.Tracer.Dump ();
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			throw new NotSupportedException (message);
		}
	}
}
