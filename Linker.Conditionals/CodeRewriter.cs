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

		public MethodBody Body => Scanner.Body;

		public CodeRewriter (BasicBlockScanner scanner)
		{
			Scanner = scanner;
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
		public void ReplaceWithInstruction (ref BasicBlock block, int stackDepth, Instruction instruction)
		{
			if (stackDepth == 0 && instruction == null) {
				// Delete the entire block.
				BlockList.DeleteBlock (ref block);
				return;
			}

			// Remove everything except the first instruction.
			while (block.Count > 1)
				RemoveInstructionAt (block, 1);

			if (stackDepth == 0) {
				ReplaceInstructionAt (ref block, 0, instruction);
				return;
			}

			ReplaceInstructionAt (ref block, 0, Instruction.Create (OpCodes.Pop));
			for (int i = 1; i < stackDepth; i++)
				BlockList.InsertInstructionAt (ref block, i, Instruction.Create (OpCodes.Pop));

			if (instruction != null)
				BlockList.InsertInstructionAt (ref block, stackDepth, instruction);
		}

		public void RemoveInstructionAt (BasicBlock block, int position)
		{
			if (block.Count < 2)
				throw new InvalidOperationException ("Basic block must have at least one instruction in it.");
			if (position == 0)
				throw new ArgumentOutOfRangeException (nameof (position), "Cannot replace first instruction in basic block.");

			var instruction = block.Instructions [position];
			Body.Instructions.Remove (instruction);
			block.RemoveInstructionAt (position);
			instruction.Offset = -1;

			// Only the last instruction in a basic block can be a branch.
			if (position == block.Count)
				CheckRemoveJumpOrigin (instruction);
		}

		public void ReplaceInstructionAt (ref BasicBlock block, int position, Instruction instruction)
		{
			var old = block.Instructions [position];
			var index = Body.Instructions.IndexOf (block.Instructions [position]);
			Body.Instructions [index] = instruction;
			instruction.Offset = -1;

			if (position > 0) {
				block.RemoveInstructionAt (position);
				block.InsertAt (position, instruction);
				if (position == block.Count - 1)
					CheckRemoveJumpOrigin (old);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToArray ();
			instructions [position] = instruction;

			BlockList.ReplaceBlock (ref block, instructions);
		}

		void CheckRemoveJumpOrigin (Instruction instruction)
		{
			var type = CecilHelper.GetBranchType (instruction);
			switch (type) {
			case BranchType.None:
			case BranchType.Return:
			case BranchType.Exit:
			case BranchType.EndFinally:
				return;
			case BranchType.Jump:
			case BranchType.True:
			case BranchType.False:
				RemoveJumpOrigin (instruction);
				break;
			default:
				throw new MartinTestException ();
			}
		}

		void RemoveJumpOrigin (Instruction instruction)
		{
			var target = BlockList.GetBlock ((Instruction)instruction.Operand);
			target.RemoveJumpOrigin (instruction);
		}
	}
}
