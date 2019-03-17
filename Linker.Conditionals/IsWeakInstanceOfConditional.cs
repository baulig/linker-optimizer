//
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

		public IsWeakInstanceOfConditional (BasicBlockList blocks, TypeDefinition type, bool hasLoad)
			: base (blocks)
		{
			InstanceType = type;
			HasLoadInstruction = hasLoad;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			Context.LogMessage ($"REWRITE CONDITIONAL: {this} {BlockList.Body.Method.Name} {block}");

			var evaluated = Context.Annotations.IsMarked (InstanceType);

			if (!evaluated)
				RewriteConditional (ref block, HasLoadInstruction ? 0 : 1, false);
			else
				RewriteAsIsInst (ref block);
		}

		void RewriteAsIsInst (ref BasicBlock block)
		{
			Context.LogMessage ($"REWRITE AS ISINST: {block.Count} {block}");

			var index = HasLoadInstruction ? 1 : 0;
			var branchType = block.BranchType;

			/*
			 * The block consists of the following:
			 *
			 * - optional simple load instruction
			 * - conditional call
			 * - optional branch instruction
			 *
			 */

			BlockList.ReplaceInstructionAt (ref block, index++, Instruction.Create (OpCodes.Isinst, InstanceType));

			block.BranchType = branchType;

			switch (block.BranchType) {
			case BranchType.False:
			case BranchType.True:
				block.Type = BasicBlockType.Branch;
				break;
			case BranchType.Return:
				// Convert it into a bool.
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				block.Type = BasicBlockType.Branch;
				break;
			case BranchType.None:
				// Convert it into a bool.
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				block.Type = BasicBlockType.Normal;
				break;
			default:
				throw new MartinTestException ();

			}
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {InstanceType.Name} {HasLoadInstruction}]";
		}
	}
}
