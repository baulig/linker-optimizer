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

		public bool NoConditionalRedefinition {
			get; set;
		}

		public IList<string> DebugTypes => _debug_types;

		public IList<string> DebugMethods => _debug_methods;

		readonly List<string> _debug_types = new List<string> ();
		readonly List<string> _debug_methods = new List<string> ();

		public MartinOptions ()
		{
			NoConditionalRedefinition = true;
		}

		public bool EnableDebugging (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return EnableDebugging (type.DeclaringType);

			if (type.Namespace == "Martin.LinkerTest")
				return true;
			if (type.Module.Assembly.Name.Name.ToLowerInvariant ().Contains ("martin"))
				return true;

			return _debug_types.Any (t => type.FullName.Contains (t));
		}

		public bool EnableDebugging (MethodDefinition method)
		{
			if (EnableDebugging (method.DeclaringType))
				return true;
			if (method.Name == "Main")
				return true;
			if (method.FullName.Contains ("Martin"))
				return true;

			return _debug_methods.Any (m => method.FullName.Contains (m));
		}
	}
}
