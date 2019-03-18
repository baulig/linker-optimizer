//
// IsTypeAvailableConditional.cs
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

namespace Mono.Linker.Conditionals
{
	public class IsTypeAvailableConditional : LinkerConditional
	{
		public TypeDefinition InstanceType {
			get;
		}

		IsTypeAvailableConditional (BasicBlockList blocks, TypeDefinition type)
			: base (blocks)
		{
			InstanceType = type;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			var evaluated = Context.Annotations.IsMarked (InstanceType);
			Context.LogMessage ($"REWRITE FEATURE CONDITIONAL: {InstanceType} {evaluated}");

			RewriteConditional (ref block, 0, evaluated);
		}

		public static IsTypeAvailableConditional Create (BasicBlockList blocks, ref BasicBlock bb, ref int index, TypeDefinition type)
		{
			if (index + 1 >= blocks.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * `bool MonoLinkerSupport.IsTypeAvailable<T> ()`
			 *
			 */

			if (bb.Instructions.Count > 1)
				blocks.SplitBlockAt (ref bb, bb.Instructions.Count - 1);

			var instance = new IsTypeAvailableConditional (blocks, type);
			bb.LinkerConditional = instance;

			LookAheadAfterConditional (blocks, ref bb, ref index);

			return instance;
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {InstanceType}]";
		}

	}
}
