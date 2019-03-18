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

		protected MethodDefinition Method => BlockList.Body.Method;

		protected AssemblyDefinition Assembly => Method.DeclaringType.Module.Assembly;

		protected LinkerConditional (BasicBlockList blocks)
		{
			BlockList = blocks;
		}

		public abstract void RewriteConditional (ref BasicBlock block);

		protected void RewriteConditional (ref BasicBlock block, int stackDepth, bool condition)
		{
			/*
			 * The conditional call can be replaced with a constant.
			 */

			switch (block.BranchType) {
			case BranchType.False:
				RewriteBranch (ref block, stackDepth, !condition);
				break;
			case BranchType.True:
				RewriteBranch (ref block, stackDepth, condition);
				break;
			case BranchType.None:
				RewriteAsConstant (ref block, stackDepth, condition);
				break;
			case BranchType.Return:
				RewriteReturn (ref block, stackDepth, condition);
				break;
			default:
				throw new MartinTestException ();
			}
		}

		void RewriteReturn (ref BasicBlock block, int stackDepth, bool condition)
		{
			if (block.LastInstruction.OpCode.Code != Code.Ret)
				throw new NotSupportedException ();

			// Rewrite as constant, then put back the return
			var constant = Instruction.Create (condition ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			ReplaceWithInstruction (ref block, stackDepth, constant);
			BlockList.InsertInstructionAt (ref block, block.Count, Instruction.Create (OpCodes.Ret));
		}

		void RewriteBranch (ref BasicBlock block, int stackDepth, bool condition)
		{
			/*
			 * If the instruction immediately following the conditional call is a
			 * conditional branch, then we can resolve the conditional and do not
			 * need to load the boolean conditional value onto the stack.
			 */

			if (condition) {
				/*
				 * Replace with direct jump.  Not that ReplaceWithInstruction() will take
				 * care of popping extra values off the stack if needed.
				 */
				var branch = Instruction.Create (OpCodes.Br, (Instruction)block.LastInstruction.Operand);
				ReplaceWithInstruction (ref block, stackDepth, branch);
			} else if (stackDepth > 0) {
				/*
				 * The condition is false, but there are still values on the stack that
				 * we need to pop.
				 */
				ReplaceWithInstruction (ref block, stackDepth, null);
			} else {
				/*
				 * The condition is false and there are no additional values on the stack.
				 * We can just simply delete the entire block.
				 */
				BlockList.DeleteBlock (ref block);
			}
		}

		void RewriteAsConstant (ref BasicBlock block, int stackDepth, bool condition)
		{
			/*
			 * Replace the entire block with a constant.
			 */
			var constant = Instruction.Create (condition ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			ReplaceWithInstruction (ref block, stackDepth, constant);
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
			if (stackDepth == 0 && instruction == null) {
				// Delete the entire block.
				BlockList.DeleteBlock (ref block);
				return;
			}

			// Remove everything except the first instruction.
			while (block.Count > 1)
				BlockList.RemoveInstructionAt (block, 1);

			if (stackDepth == 0) {
				BlockList.ReplaceInstructionAt (ref block, 0, instruction);
				return;
			}

			BlockList.ReplaceInstructionAt (ref block, 0, Instruction.Create (OpCodes.Pop));
			for (int i = 1; i < stackDepth; i++)
				BlockList.InsertInstructionAt (ref block, i, Instruction.Create (OpCodes.Pop));

			if (instruction != null)
				BlockList.InsertInstructionAt (ref block, stackDepth, instruction);
		}

		public static bool Scan (BasicBlockList blocks, ref BasicBlock bb, ref int index, Instruction instruction)
		{
			var target = (MethodReference)instruction.Operand;
			blocks.Context.LogMessage ($"    CALL: {target}");

			if (instruction.Operand is GenericInstanceMethod genericInstance) {
				if (genericInstance.ElementMethod == blocks.Context.IsWeakInstanceOfMethod) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					IsWeakInstanceOfConditional.Create (blocks, ref bb, ref index, conditionalType);
					return true;
				} else if (genericInstance.ElementMethod == blocks.Context.AsWeakInstanceOfMethod) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					AsWeakInstanceOfConditional.Create (blocks, ref bb, ref index, conditionalType);
					return true;
				} else if (genericInstance.ElementMethod == blocks.Context.IsTypeAvailableMethod) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					IsTypeAvailableConditional.Create (blocks, ref bb, ref index, conditionalType);
					return true;
				}
			} else if (target == blocks.Context.IsFeatureSupportedMethod) {
				IsFeatureSupportedConditional.Create (blocks, ref bb, ref index);
				return true;
			} else if (target == blocks.Context.IsTypeNameAvailableMethod) {
				IsTypeAvailableConditional.Create (blocks, ref bb, ref index);
				return true;
			}

			return false;
		}

		protected static void LookAheadAfterConditional (BasicBlockList blocks, ref BasicBlock bb, ref int index)
		{
			if (index + 1 >= blocks.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * Look ahead at the instruction immediately following the call to the
			 * conditional support method (`IsWeakInstanceOf<T>()` or `IsFeatureSupported()`).
			 *
			 * If it's a branch, then we add it to the current block.  Since the conditional
			 * method leaves a `bool` value on the stack, the following instruction can never
			 * be an unconditional branch.
			 *
			 * At the end of this method, the current basic block will always look like this:
			 *
			 *   - (optional) simple load
			 *   - conditional call
			 *   - (optional) conditional branch.
			 *
			 * We will also close out the current block and start a new one after this.
			 */

			var next = blocks.Body.Instructions [index + 1];
			var type = CecilHelper.GetBranchType (next);

			switch (type) {
			case BranchType.None:
				bb = null;
				break;
			case BranchType.False:
			case BranchType.True:
			case BranchType.Return:
				bb.AddInstruction (next);
				index++;
				bb = null;
				break;
			default:
				throw new MartinTestException ($"UNKNOWN BRANCH TYPE: {type} {next.OpCode}");
			}
		}

	}
}
