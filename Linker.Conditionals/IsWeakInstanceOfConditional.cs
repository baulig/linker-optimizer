﻿//
// IsWeakInstanceOfConditional.cs
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

namespace Mono.Linker.Conditionals
{
	public class IsWeakInstanceOfConditional : LinkerConditional
	{
		public TypeDefinition InstanceType {
			get;
		}

		public bool HasLoadInstruction {
			get;
		}

		IsWeakInstanceOfConditional (BasicBlockList blocks, TypeDefinition type, bool hasLoad)
			: base (blocks)
		{
			InstanceType = type;
			HasLoadInstruction = hasLoad;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			Context.LogMessage ($"REWRITE CONDITIONAL: {this} {BlockList.Body.Method.Name} {block}");

			var evaluated = Context.Annotations.IsMarked (InstanceType);
			Context.MarkConditionalType (InstanceType);

			if (!evaluated)
				RewriteConditional (ref block, HasLoadInstruction ? 0 : 1, false);
			else
				RewriteAsIsInst (ref block);
		}

		void RewriteAsIsInst (ref BasicBlock block)
		{
			Context.LogMessage ($"REWRITE AS ISINST: {block.Count} {block}");

			var index = HasLoadInstruction ? 1 : 0;

			/*
			 * The block consists of the following:
			 *
			 * - optional simple load instruction
			 * - conditional call
			 * - optional branch instruction
			 *
			 */

			var reference = Assembly.MainModule.ImportReference (InstanceType);

			BlockList.ReplaceInstructionAt (ref block, index++, Instruction.Create (OpCodes.Isinst, reference));

			switch (block.BranchType) {
			case BranchType.False:
			case BranchType.True:
				break;
			case BranchType.None:
			case BranchType.Return:
				// Convert it into a bool.
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				break;
			default:
				throw new MartinTestException ();

			}
		}

		public static IsWeakInstanceOfConditional Create (BasicBlockList blocks, ref BasicBlock bb, ref int index, TypeDefinition type)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= blocks.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * `bool MonoLinkerSupport.IsWeakInstance<T> (object instance)`
			 *
			 * If the function argument is a simple load (like for instance `Ldarg_0`),
			 * then we can simply remove that load.  Otherwise, we need to insert a
			 * `Pop` to discard the value on the stack.
			 *
			 * In either case, we always start a new basic block for the conditional.
			 * Its first instruction will either be the simple load or the call itself.
			 */

			var argument = blocks.Body.Instructions [index - 1];

			blocks.Context.LogMessage ($"WEAK INSTANCE OF: {bb} {index} {type} - {argument}");

			bool hasLoad;
			TypeDefinition instanceType;
			if (CecilHelper.IsSimpleLoad (argument)) {
				if (bb.Instructions.Count > 2)
					blocks.SplitBlockAt (ref bb, bb.Instructions.Count - 2);
				instanceType = CecilHelper.GetWeakInstanceArgument (bb.Instructions [1]);
				hasLoad = true;
			} else {
				blocks.SplitBlockAt (ref bb, bb.Instructions.Count - 1);
				instanceType = CecilHelper.GetWeakInstanceArgument (bb.Instructions [0]);
				hasLoad = false;
			}

			var instance = new IsWeakInstanceOfConditional (blocks, instanceType, hasLoad);
			bb.LinkerConditional = instance;

			/*
			 * Once we get here, the current block only contains the (optional) simple load
			 * and the conditional call itself.
			 */

			if (index + 1 >= blocks.Body.Instructions.Count)
				throw new NotSupportedException ();

			LookAheadAfterConditional (blocks, ref bb, ref index);

			return instance;
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {InstanceType.Name} {HasLoadInstruction}]";
		}
	}
}
