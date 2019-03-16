//
// IsFeatureSupportedConditional.cs
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
	public class IsFeatureSupportedConditional : LinkerConditional
	{
		public int Feature {
			get;
		}

		public IsFeatureSupportedConditional (BasicBlockList blocks, int feature)
			: base (blocks)
		{
			Feature = feature;
		}

		public override void RewriteConditional (ref BasicBlock block)
		{
			var evaluated = Context.IsFeatureEnabled (Feature);
			Context.LogMessage ($"REWRITE FEATURE CONDITIONAL: {Feature} {evaluated}");

			RewriteConditional (ref block, 0, evaluated);
		}

		void RewriteBranch (BasicBlock block, bool condition, bool evaluated)
		{
			var target = (Instruction)block.LastInstruction.Operand;

			Context.LogMessage ($"  REWRITING BRANCH: {block} {condition} {evaluated} {target}");

			BlockList.Dump (block);

			/*
			 * If the instruction immediately following the conditional call is a
			 * conditional branch, then we can resolve the conditional and do not
			 * need to load the boolean conditional value onto the stack.
			 */

			var pop = Instruction.Create (OpCodes.Pop);
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
			if (evaluated) {
				BlockList.ReplaceInstructionAt (ref block, 0, branch);
				BlockList.RemoveInstructionAt (block, 1);
				BlockList.RemoveInstructionAt (block, 1);
				block.Type = BasicBlockType.Branch;
			} else {
				BlockList.DeleteBlock (block);
			}
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name}: {Feature}]";
		}
	}
}
