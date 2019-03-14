//
// MonoLinkerSupportStep.cs
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
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Conditionals
{
	public class MonoLinkerSupportStep : BaseStep
	{
		static void Debug (string message)
		{
			Console.Error.WriteLine (message);
		}

		bool IsEnabled (AssemblyNameDefinition name) => name.Name == "mscorlib" || name.Name == "martin-test";

		bool IsEnabled (TypeDefinition type) => type.Namespace == "Martin.LinkerTest";

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (!IsEnabled (assembly.Name))
				return;

			Debug ($"MONO LINKER SUPPORT: {assembly}");

			foreach (TypeDefinition type in assembly.MainModule.Types) {
				if (IsEnabled (type))
					ProcessType (type);
			}
		}

		void ProcessType (TypeDefinition type)
		{
			Debug ($"MONO LINKER SUPPORT: {type}");

			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					ProcessType (nested);
			}

			foreach (var method in type.Methods)
				ProcessMethod (method);
		}

		void ProcessMethod (MethodDefinition method)
		{
			Debug ($"MONO LINKER SUPPORT: {method}");

			var scanner = new BasicBlockScanner (method.Body);
			scanner.Scan ();
		}
	}
}
