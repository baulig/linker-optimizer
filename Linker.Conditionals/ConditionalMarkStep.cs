//
// ConditionalMarkStep.cs
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
using Mono.Cecil.Cil;
using Mono.Linker.Steps;
using System.Collections.Generic;
using Mono.Collections.Generic;

namespace Mono.Linker.Conditionals
{
	public class ConditionalMarkStep : MarkStep
	{
		public MartinContext MartinContext => _context.MartinContext;

		Queue<MethodDefinition> _conditional_methods;
		Dictionary<MethodDefinition, BasicBlockScanner> _block_scanner_by_method;

		public ConditionalMarkStep ()
		{
			_conditional_methods = new Queue<MethodDefinition> ();
			_block_scanner_by_method = new Dictionary<MethodDefinition, BasicBlockScanner> ();
		}

		protected override void DoAdditionalProcessing ()
		{
			if (_methods.Count > 0)
				return;

			while (_conditional_methods.Count > 0) {
				var conditional = _conditional_methods.Dequeue ();
				MartinContext.LogDebug ($"  CONDITIONAL METHOD: {conditional}");
				var scanner = _block_scanner_by_method [conditional];
				scanner.RewriteConditionals ();
				base.MarkMethodBody (conditional.Body);
			}
		}

		protected override void MarkMethodBody (MethodBody body)
		{
			if (!MartinContext.IsEnabled (body.Method)) {
				base.MarkMethodBody (body);
				return;
			}

			MartinContext.LogMessage ($"MARK BODY: {body.Method}");

			var scanner = BasicBlockScanner.Scan (MartinContext, body.Method);
			if (scanner == null) {
				MartinContext.LogMessage (MessageImportance.High, $"BB SCAN FAILED: {body.Method}");
				base.MarkMethodBody (body);
				return;
			}

			if (!scanner.FoundConditionals) {
				base.MarkMethodBody (body);
				return;
			}

			MartinContext.LogMessage ($"MARK BODY - CONDITIONAL: {body.Method}");

			_conditional_methods.Enqueue (body.Method);
			_block_scanner_by_method.Add (body.Method, scanner);
		}

		protected override TypeDefinition MarkType (TypeReference reference)
		{
			var type = reference?.Resolve ();
			if (type != null && type.FullName.Contains ("Foo") && !Annotations.IsProcessed (type))
				MartinContext.LogMessage ($"FOO MARKED!");
			return base.MarkType (reference);
		}
	}
}
