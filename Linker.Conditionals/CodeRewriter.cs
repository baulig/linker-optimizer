//
// CodeRewriter.cs
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
using Mono.Cecil.Cil;

namespace Mono.Linker.Conditionals
{
	public class CodeRewriter
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public BasicBlockList BlockList => Scanner.BlockList;

		public MethodDefinition Method => Scanner.Method;

		public MethodBody Body => Scanner.Body;

		public CodeRewriter (BasicBlockScanner scanner)
		{
			Scanner = scanner;
		}

		/*
		 * The block @block contains a linker conditional that resolves into the
		 * boolean constant @condition.  There are @stackDepth extra values on the
		 * stack and the block ends with a branch instruction.
		 *
		 * If @condition is true, then we replace the branch with a direct jump.
		 *
		 * If @condition is false, then we remove the branch.
		 *
		 * In either case, we need to make sure to pop the extra @stackDepth values
		 * off the stack.
		 *
		 */

		public void ReplaceWithBranch (ref BasicBlock block, int stackDepth, bool condition)
		{
			if (!CecilHelper.IsBranch (block.BranchType))
				throw new NotSupportedException ($"{nameof (ReplaceWithBranch)} used on non-branch block.");

			Instruction branch = null;
			if (condition)
				branch = Instruction.Create (OpCodes.Br, (Instruction)block.LastInstruction.Operand);

			ReplaceWithInstruction (ref block, stackDepth, branch);
		}

		/*
		 * Replace block @block with a boolean constant @constant, optionally popping
		 * @stackDepth extra values off the stack.
		 */
		public void ReplaceWithConstant (ref BasicBlock block, int stackDepth, bool constant)
		{
			var instruction = Instruction.Create (constant ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

			switch (block.BranchType) {
			case BranchType.None:
				ReplaceWithInstruction (ref block, stackDepth, instruction);
				break;
			case BranchType.Return:
				// Rewrite as constant, then put back the return
				Scanner.Rewriter.ReplaceWithInstruction (ref block, stackDepth, instruction);
				BlockList.InsertInstructionAt (ref block, block.Count, Instruction.Create (OpCodes.Ret));
				break;
			default:
				throw new NotSupportedException ($"{nameof (ReplaceWithConstant)} called on unsupported block type `{block.BranchType}`.");
			}
		}

		/*
		 * Replace block @block with an `isinst`, but keep @index instructions at the beginning
		 * of the block and the (optional) branch at the end.
		 *
		 * The block is expected to contain the following:
		 * 
		 * - optional simple load instruction
		 * - conditional call
		 * - optional branch instruction
		 *
		 */
		public void ReplaceWithIsInst (ref BasicBlock block, int index, TypeDefinition type)
		{
			if (index < 0 || index >= block.Count)
				throw new ArgumentOutOfRangeException (nameof (index));

			if (block.Instructions [index].OpCode.Code != Code.Call)
				DebugHelpers.AssertFailUnexpected (Method, block, block.Instructions [index]);

			/*
			 * The block consists of the following:
			 *
			 * - optional simple load instruction
			 * - conditional call
			 * - optional branch instruction
			 *
			 */

			var reference = Method.DeclaringType.Module.ImportReference (type);

			BlockList.ReplaceInstructionAt (ref block, index++, Instruction.Create (OpCodes.Isinst, reference));

			switch (block.BranchType) {
			case BranchType.False:
			case BranchType.True:
				DebugHelpers.Assert (index == block.Count - 1);
				break;
			case BranchType.None:
				// Convert it into a bool.
				DebugHelpers.Assert (index == block.Count);
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				break;
			case BranchType.Return:
				// Convert it into a bool.
				DebugHelpers.Assert (index == block.Count - 1);
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index++, Instruction.Create (OpCodes.Cgt_Un));
				break;
			default:
				throw DebugHelpers.AssertFailUnexpected (Method, block, block.BranchType);
			}
		}


		/*
		 * Replace the entire block with the following:
		 *
		 * - pop @stackDepth values from the stack
		 * - (optional) instruction @instruction
		 *
		 * The block will be deleted if this would result in an empty block.
		 *
		 */
		void ReplaceWithInstruction (ref BasicBlock block, int stackDepth, Instruction instruction)
		{
			Scanner.LogDebug (1, $"REPLACE INSTRUCTION: {block} {stackDepth} {instruction}");
			Scanner.DumpBlock (1, block);

			// Remove everything except the first instruction.
			while (block.Count > 1)
				BlockList.RemoveInstructionAt (ref block, 1);

			if (stackDepth == 0) {
				if (instruction == null)
					BlockList.RemoveInstructionAt (ref block, 0);
				else
					BlockList.ReplaceInstructionAt (ref block, 0, instruction);
				return;
			}

			BlockList.ReplaceInstructionAt (ref block, 0, Instruction.Create (OpCodes.Pop));
			for (int i = 1; i < stackDepth; i++)
				BlockList.InsertInstructionAt (ref block, i, Instruction.Create (OpCodes.Pop));

			if (instruction != null)
				BlockList.InsertInstructionAt (ref block, stackDepth, instruction);
		}
	}
}
