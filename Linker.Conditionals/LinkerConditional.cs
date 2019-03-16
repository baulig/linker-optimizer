//
// LinkerConditional.cs
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
	public abstract class LinkerConditional
	{
		public BasicBlockList BlockList {
			get;
		}

		protected MartinContext Context => BlockList.Context;

		protected LinkerConditional (BasicBlockList blocks)
		{
			BlockList = blocks;
		}

		public abstract void RewriteConditional (ref BasicBlock block);

		protected void RewriteConditional (ref BasicBlock block, int stackDepth, bool evaluated)
		{
			/*
			 * The conditional call will either be the last instruction in the block
			 * or it will be followed by a conditional branch (since the call returns a
			 * boolean, it cannot be an unconditional branch).
			 */

			switch (block.BranchType) {
			case BranchType.FeatureFalse:
				RewriteBranch (ref block, stackDepth, !evaluated);
				break;
			case BranchType.FeatureTrue:
				RewriteBranch (ref block, stackDepth, evaluated);
				break;
			case BranchType.Feature:
				RewriteAsConstant (ref block, stackDepth, evaluated);
				break;
			case BranchType.FeatureReturn:
				RewriteReturn (ref block, stackDepth, evaluated);
				break;
			default:
				throw new MartinTestException ();
			}
		}

		void RewriteReturn (ref BasicBlock block, int stackDepth, bool evaluated)
		{
			if (false && block.Count != stackDepth + 3)
				throw new MartinTestException ();
			if (block.LastInstruction.OpCode.Code != Code.Ret)
				throw new MartinTestException ();

			// Rewrite as constant, then put back the return
			var constant = Instruction.Create (evaluated ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			ReplaceWithInstruction (ref block, stackDepth, constant);
			BlockList.InsertInstructionAt (ref block, block.Count, Instruction.Create (OpCodes.Ret));

			block.Type = BasicBlockType.Branch;
			block.BranchType = BranchType.Return;
		}

		void RewriteBranch (ref BasicBlock block, int stackDepth, bool condition)
		{
			if (false && block.Count != stackDepth + 3)
				throw new MartinTestException ();

			var target = (Instruction)block.LastInstruction.Operand;

			Context.LogMessage ($"  REWRITING BRANCH: {block} {stackDepth} {condition} {target}");

			/*
			 * If the instruction immediately following the conditional call is a
			 * conditional branch, then we can resolve the conditional and do not
			 * need to load the boolean conditional value onto the stack.
			 */

			var branch = Instruction.Create (OpCodes.Br, target);

			/*
			 * The block contains a simple load, the conditional call and the branch.
			 *
			 * If the branch opcode was a conditional branch and it's condition
			 * evaluated to false, then we can just simply remove the entire block.
			 *
			 * Otherwise, we will replace the entire block with an unconditional
			 * branch to the target.
			 *
			 */

			if (condition) {
				ReplaceWithInstruction (ref block, stackDepth, branch);

				block.Type = BasicBlockType.Branch;
				block.BranchType = BranchType.Jump;
			} else if (stackDepth > 0) {
				// Remove everything except the first instruction.
				for (int i = 1; i < block.Count; i++)
					BlockList.RemoveInstructionAt (block, 1);

				BlockList.ReplaceInstructionAt (ref block, 0, Instruction.Create (OpCodes.Pop));
				for (int i = 1; i < stackDepth; i++)
					BlockList.InsertInstructionAt (ref block, i, Instruction.Create (OpCodes.Pop));

				block.Type = BasicBlockType.Normal;
				block.BranchType = BranchType.None;
			} else {
				BlockList.DeleteBlock (block);
			}
		}

		void RewriteAsConstant (ref BasicBlock block, int stackDepth, bool evaluated)
		{
			var constant = Instruction.Create (evaluated ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			ReplaceWithInstruction (ref block, stackDepth, constant);

			block.Type = BasicBlockType.Normal;
			block.BranchType = BranchType.None;
		}

		void ReplaceWithInstruction (ref BasicBlock block, int stackDepth, Instruction instruction)
		{
			// Remove everything except the first instruction.
			for (int i = 1; i < block.Count; i++)
				BlockList.RemoveInstructionAt (block, 1);

			if (stackDepth == 0) {
				BlockList.ReplaceInstructionAt (ref block, 0, instruction);
				return;
			}

			BlockList.ReplaceInstructionAt (ref block, 0, Instruction.Create (OpCodes.Pop));
			for (int i = 1; i < stackDepth; i++)
				BlockList.InsertInstructionAt (ref block, i, Instruction.Create (OpCodes.Pop));

			BlockList.InsertInstructionAt (ref block, stackDepth, instruction);
		}
	}
}
