//
// BasicBlockScanner.cs
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
	public class BasicBlockScanner
	{
		public MartinContext Context {
			get;
		}

		public MethodDefinition Method {
			get;
		}

		public bool FoundConditionals {
			get; private set;
		}

		public BasicBlockList BlockList {
			get;
		}

		BasicBlockScanner (MartinContext context, MethodDefinition method)
		{
			Context = context;
			Method = method;

			BlockList = new BasicBlockList (context, method.Body);
		}

		public static bool ThrowOnError;

		public static BasicBlockScanner Scan (MartinContext context, MethodDefinition method)
		{
			var scanner = new BasicBlockScanner (context, method);
			if (!scanner.Scan ())
				return null;
			return scanner;
		}

		public IReadOnlyCollection<BasicBlock> BasicBlocks => BlockList.Blocks;

		void CloseBlock (ref BasicBlock block, Instruction instruction, Instruction target)
		{
			CloseBlock (ref block, CecilHelper.GetBranchType (instruction), target);
		}

		void CloseBlock (ref BasicBlock block, BranchType branch, Instruction target)
		{
			if (block != null) {
				block.BranchType = branch;
				if (block.Type == BasicBlock.BlockType.Normal)
					block.Type = branch == BranchType.Switch ? BasicBlock.BlockType.Switch : BasicBlock.BlockType.Branch;
				block = null;
			}
			if (!BlockList.HasBlock (target))
				BlockList.NewBlock (target);
		}

		bool Scan ()
		{
			Context.LogMessage ($"SCAN: {Method}");

			BasicBlock bb = null;

			for (int i = 0; i < Method.Body.Instructions.Count; i++) {
				var instruction = Method.Body.Instructions [i];

				if (bb == null) {
					if (BlockList.TryGetBlock (instruction, out bb)) {
						Context.LogMessage ($"    KNOWN BB: {bb}");
					} else {
						bb = BlockList.NewBlock (instruction);
						Context.LogMessage ($"    NEW BB: {bb}");
					}
				} else if (BlockList.TryGetBlock (instruction, out var newBB)) {
					bb = newBB;
					Context.LogMessage ($"    KNOWN BB: {bb}");
				} else {
					bb.AddInstruction (instruction);
				}

				switch (instruction.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					if (bb.Type == BasicBlock.BlockType.Normal)
						bb.Type = BasicBlock.BlockType.Branch;
					CloseBlock (ref bb, instruction, (Instruction)instruction.Operand);
					continue;
				case OperandType.InlineSwitch:
					if (bb.Type == BasicBlock.BlockType.Normal)
						bb.Type = BasicBlock.BlockType.Switch;
					foreach (var label in (Instruction[])instruction.Operand) {
						CloseBlock (ref bb, BranchType.Switch, label);
					}
					continue;
				case OperandType.InlineMethod:
					HandleCall (ref bb, ref i, instruction);
					continue;
				}

				switch (instruction.OpCode.Code) {
				case Code.Throw:
				case Code.Rethrow:
					Context.LogMessage ($"    THROW");
					bb.Type = BasicBlock.BlockType.Branch;
					bb = null;
					break;
				case Code.Ret:
					Context.LogMessage ($"    RET");
					bb.Type = BasicBlock.BlockType.Branch;
					bb = null;
					break;
				}
			}

			BlockList.ComputeOffsets ();

			BlockList.Dump ();

			return true;
		}

		void HandleCall (ref BasicBlock bb, ref int index, Instruction instruction)
		{
			var target = (MethodReference)instruction.Operand;
			Context.LogMessage ($"    CALL: {target}");

			if (instruction.Operand is GenericInstanceMethod genericInstance) {
				if (genericInstance.ElementMethod == Context.IsWeakInstanceOfMethod) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					HandleWeakInstanceOf (ref bb, ref index, conditionalType);
				}
			} else if (target == Context.IsFeatureSupportedMethod) {
				HandleIsFeatureSupported (ref bb, ref index);
			} else if (target == Context.MarkFeatureMethod) {
				HandleMarkFeature (ref bb, ref index);
			}
		}

		void HandleWeakInstanceOf (ref BasicBlock bb, ref int index, TypeDefinition type)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= Method.Body.Instructions.Count)
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

			var argument = Method.Body.Instructions [index - 1];

			Context.LogMessage ($"WEAK INSTANCE OF: {bb} {index} {type} - {argument}");

			if (CecilHelper.IsSimpleLoad (argument)) {
				if (bb.Instructions.Count > 2)
					BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);
				bb.Type = BasicBlock.BlockType.SimpleWeakInstanceOf;
			} else {
				BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 1);
				bb.Type = BasicBlock.BlockType.WeakInstanceOf;
			}

			/*
			 * Once we get here, the current block only contains the (optional) simple load
			 * and the conditional call itself.
			 */

			FoundConditionals = true;

			if (index + 1 >= Method.Body.Instructions.Count)
				throw new NotSupportedException ();

			LookAheadAfterConditional (ref bb, ref index);
		}

		void HandleIsFeatureSupported (ref BasicBlock bb, ref int index)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= Method.Body.Instructions.Count)
				throw new NotSupportedException ();

			/*
			 * `bool MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature feature)`
			 *
			 */

			if (bb.Instructions.Count > 2)
				BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);
			bb.Type = BasicBlock.BlockType.IsFeatureSupported;

			FoundConditionals = true;

			LookAheadAfterConditional (ref bb, ref index);
		}

		void HandleMarkFeature (ref BasicBlock bb, ref int index)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= Method.Body.Instructions.Count)
				throw new NotSupportedException ();

			var feature = CecilHelper.GetFeatureArgument (bb.Instructions [bb.Count - 2]);
			Context.SetFeatureEnabled (feature, true);

			Context.LogMessage ($"MARK FEATURE: {feature}");

			/*
			 * `void MonoLinkerSupport.MarkFeature (MonoLinkerFeature feature)`
			 *
			 */

			if (bb.Instructions.Count > 2) {
				/*
				 * If we are in the middle of a basic block, then we can simply remove
				 * the two instructions.
				 */
				BlockList.RemoveInstructionAt (bb, bb.Count - 1);
				BlockList.RemoveInstructionAt (bb, bb.Count - 1);
				index -= 2;
				return;
			} else {
				/*
				 * We are at the beginning of a basic block.  Since somebody might jump
				 * to us, we replace the call with a `nop`.
				 */
				BlockList.RemoveInstructionAt (bb, bb.Count - 1);
				BlockList.ReplaceInstructionAt (ref bb, bb.Count - 1, Instruction.Create (OpCodes.Nop));
				index--;
				return;
			}
		}

		void LookAheadAfterConditional (ref BasicBlock bb, ref int index)
		{
			if (index + 1 >= Method.Body.Instructions.Count)
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

			var next = Method.Body.Instructions [index + 1];
			if (next.OpCode.OperandType == OperandType.InlineBrTarget || next.OpCode.OperandType == OperandType.ShortInlineBrTarget) {
				bb.AddInstruction (next);
				index++;

				var target = (Instruction)next.Operand;
				if (!BlockList.HasBlock (target)) {
					Context.LogMessage ($"    JUMP TARGET BB: {target}");
					BlockList.NewBlock (target);
				}
			}

			// Always start a new basic block after this.
			bb = null;
		}

		public void RewriteConditionals ()
		{
			Context.LogMessage ($"REWRITE CONDITIONALS");

			BlockList.Dump ();

			var foundConditionals = false;

			foreach (var block in BlockList.Blocks.ToArray ()) {
				switch (block.Type) {
				case BasicBlock.BlockType.Normal:
				case BasicBlock.BlockType.Branch:
				case BasicBlock.BlockType.Switch:
					continue;
				case BasicBlock.BlockType.WeakInstanceOf:
				case BasicBlock.BlockType.SimpleWeakInstanceOf:
					RewriteWeakInstanceOf (block);
					foundConditionals = true;
					break;
				case BasicBlock.BlockType.IsFeatureSupported:
					RewriteIsFeatureSupported (block);
					foundConditionals = true;
					break;
				default:
					throw new NotSupportedException ();
				}
			}

			if (!foundConditionals)
				return;

			BlockList.ComputeOffsets ();

			BlockList.Dump ();

			while (EliminateDeadBlocks ()) { }

			BlockList.Dump ();

			Context.LogMessage ($"DONE REWRITING CONDITIONALS");
		}

		void RewriteWeakInstanceOf (BasicBlock block)
		{
			Context.LogMessage ($"REWRITE WEAK INSTANCE");

			BlockList.Dump ();

			bool evaluated;
			int nextIndex;
			TypeDefinition type;
			switch (block.Type) {
			case BasicBlock.BlockType.WeakInstanceOf:
				/*
				 * The argument came from a complicated expression, so we started
				 * a new basic block with the call instruction; it's argument has
				 * already been loaded onto the stack.
				 */
				type = CecilHelper.GetWeakInstanceArgument (block.Instructions [0]);
				evaluated = Context.Annotations.IsMarked (type);
				nextIndex = 1;
				break;
			case BasicBlock.BlockType.SimpleWeakInstanceOf:
				/*
				 * The argument came from a simple load, so the block starts with
				 * the load instruction followed by the call.
				 */
				type = CecilHelper.GetWeakInstanceArgument (block.Instructions [1]);
				evaluated = Context.Annotations.IsMarked (type);
				nextIndex = 2;
				break;
			default:
				throw new NotSupportedException ();
			}

			Context.LogMessage ($"REWRITE WEAK INSTANCE OF: {block} {type} {evaluated}");

			if (evaluated)
				RewriteAsIsinst (ref block, type);
			else
				RewriteConditional (block, nextIndex, false);

			BlockList.ComputeOffsets ();

			BlockList.Dump ();

			Context.LogMessage ($"REWRITE WEAK INSTANCE DONE: {block} {evaluated}");
		}

		void RewriteIsFeatureSupported (BasicBlock block)
		{
			Context.LogMessage ($"REWRITE IS FEATURE SUPPORTED");

			BlockList.Dump ();

			var feature = CecilHelper.GetFeatureArgument (block.Instructions [0]);
			var evaluated = Context.IsFeatureEnabled (feature);

			Context.LogMessage ($"REWRITE IS FEATURE SUPPORTED #1: {feature} {evaluated}");

			RewriteConditional (block, 2, evaluated);

			BlockList.Dump ();
		}

		void RewriteConditional (BasicBlock block, int nextIndex, bool evaluated)
		{
			/*
			 * The conditional call will either be the last instruction in the block
			 * or it will be followed by a conditional branch (since the call returns a
			 * boolean, it cannot be an unconditional branch).
			 */

			if (block.Instructions.Count == nextIndex + 1) {
				var next = block.Instructions [nextIndex];
				switch (next.OpCode.Code) {
				case Code.Brfalse:
				case Code.Brfalse_S:
					Context.LogMessage ($"  BR FALSE");
					RewriteBranch (ref block, !evaluated);
					return;
				case Code.Brtrue:
				case Code.Brtrue_S:
					Context.LogMessage ($"  BR TRUE");
					RewriteBranch (ref block, evaluated);
					return;
				default:
					throw new NotSupportedException ($"Invalid instruction `{next}` after conditional call.");
				}
			}

			RewriteAsConstant (ref block, evaluated);
		}

		void RewriteBranch (ref BasicBlock block, bool evaluated)
		{
			var target = (Instruction)block.LastInstruction.Operand;

			Context.LogMessage ($"  REWRITING BRANCH: {block} {evaluated} {target}");

			BlockList.Dump (block);

			/*
			 * If the instruction immediately following the conditional call is a
			 * conditional branch, then we can resolve the conditional and do not
			 * need to load the boolean conditional value onto the stack.
			 */

			var pop = Instruction.Create (OpCodes.Pop);
			var branch = Instruction.Create (OpCodes.Br, target);

			switch (block.Type) {
			case BasicBlock.BlockType.SimpleWeakInstanceOf:
			case BasicBlock.BlockType.IsFeatureSupported:
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
					block.Type = BasicBlock.BlockType.Branch;
				} else {
					BlockList.DeleteBlock (block);
				}
				break;

			case BasicBlock.BlockType.WeakInstanceOf:
				/*
				 * The block contains the conditional call and the branch.  Since the
				 * call argument has already been pushed onto the stack, we need to
				 * insert a `pop` to discard it.
				 *
				 * Then we can resolve the conditional into either using an unconditional
				 * branch or no branch at all.
				 *
				 */
				BlockList.ReplaceInstructionAt (ref block, 0, pop);
				BlockList.RemoveInstructionAt (block, 1);
				if (evaluated) {
					BlockList.InsertInstructionAt (ref block, 1, branch);
					block.Type = BasicBlock.BlockType.Branch;
				} else {
					block.Type = BasicBlock.BlockType.Normal;
				}
				break;

			default:
				throw new NotSupportedException ();
			}
		}

		void RewriteAsConstant (ref BasicBlock block, bool evaluated)
		{
			Context.LogMessage ($"  REWRITING AS CONSTANT");

			/*
			 * The instruction following the conditional call was not a branch.
			 * In this case, the conditional call is always the last instruction
			 * in the block.
			 */

			var constant = Instruction.Create (evaluated ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			var pop = Instruction.Create (OpCodes.Pop);

			switch (block.Type) {
			case BasicBlock.BlockType.SimpleWeakInstanceOf:
			case BasicBlock.BlockType.IsFeatureSupported:
				/*
				 * The block contains a simple load and the conditional call.
				 * Replace both with the constant load.
				 */
				if (block.Instructions.Count != 2)
					throw new NotSupportedException ();
				BlockList.ReplaceInstructionAt (ref block, 0, constant);
				BlockList.RemoveInstructionAt (block, 1);
				block.Type = BasicBlock.BlockType.Normal;
				break;

			case BasicBlock.BlockType.WeakInstanceOf:
				/*
				 * The block only contains the conditional call, but it's argument
				 * has already been pushed onto the stack, so we need to insert a
				 * `pop` to discard it.
				 */
				if (block.Instructions.Count != 1)
					throw new NotSupportedException ();
				BlockList.ReplaceInstructionAt (ref block, 0, pop);
				BlockList.InsertInstructionAt (ref block, 1, constant);
				block.Type = BasicBlock.BlockType.Normal;
				break;

			default:
				throw new NotSupportedException ();
			}
		}

		void RewriteAsIsinst (ref BasicBlock block, TypeDefinition type)
		{
			Context.LogMessage ($"  REWRITING AS ISINST: {type}");

			/*
			 * The feature is available, so we replace the conditional call with `isinst`.
			 */

			int index;
			switch (block.Type) {
			case BasicBlock.BlockType.SimpleWeakInstanceOf:
				/*
				 * The block contains a simple load, the conditional call
				 * and an optional branch.
				 */
				index = 1;
				break;
			case BasicBlock.BlockType.WeakInstanceOf:
				/*
				 * The block contains the conditional call (with the argument already
				 * on the stack) and an optional branch.
				 *
				 * Since we cannot replace the a basic block's first instruction,
				 * we need to replace the entire block.
				 */
				index = 0;
				break;
			default:
				throw new NotSupportedException ();
			}

			BasicBlock.BlockType blockType;
			/*
			 * The call instruction is optionally followed by a branch.
			 */
			if (block.Count == index + 1) {
				blockType = BasicBlock.BlockType.Normal;
			} else if (block.Count == index + 2) {
				if (!CecilHelper.IsConditionalBranch (block.Instructions [index + 1]))
					throw new NotSupportedException ();
				blockType = BasicBlock.BlockType.Branch;
			} else {
				throw new NotSupportedException ();
			}

			/*
			 * If we're followed by a branch (which will always be a conditional
			 * branch due to the value on the stack), then we can simply use `isinst`.
			 *
			 */

			Context.LogMessage ($"  REWRITING AS ISINST #1: {Method.Name} {type} {index} {blockType}");

			BlockList.ReplaceInstructionAt (ref block, index, Instruction.Create (OpCodes.Isinst, type));

			if (blockType == BasicBlock.BlockType.Normal) {
				// Convert it into a bool.
				BlockList.InsertInstructionAt (ref block, index + 1, Instruction.Create (OpCodes.Ldnull));
				BlockList.InsertInstructionAt (ref block, index + 2, Instruction.Create (OpCodes.Cgt_Un));
			}

			block.Type = blockType;
		}

		bool EliminateDeadBlocks ()
		{
			Context.LogMessage ($"ELIMINATING DEAD BLOCKS");

			BlockList.Dump ();

			var markNextBlock = true;
			var marked = new HashSet<BasicBlock> ();

			foreach (var block in BlockList.Blocks) {
				if (markNextBlock)
					marked.Add (block);

				if (block.Type == BasicBlock.BlockType.Normal) {
					markNextBlock = true;
					continue;
				}

				if (block.Type == BasicBlock.BlockType.Switch) {
					foreach (var label in (Instruction[])block.LastInstruction.Operand) {
						marked.Add (BlockList.GetBlock (label));
					}
					markNextBlock = true;
					continue;
				}

				if (block.Type != BasicBlock.BlockType.Branch)
					throw new NotSupportedException ();

				var branch = block.LastInstruction;
				switch (branch.OpCode.Code) {
				case Code.Br:
				case Code.Br_S:
					markNextBlock = false;
					break;
				case Code.Ret:
				case Code.Throw:
				case Code.Rethrow:
					markNextBlock = false;
					continue;
				default:
					markNextBlock = true;
					break;
				}

				marked.Add (BlockList.GetBlock ((Instruction)branch.Operand));
			}

			var removedDeadBlocks = false;
			for (int i = 0; i < BlockList.Count; i++) {
				if (marked.Contains (BlockList [i]))
					continue;

				Context.LogMessage ($"  DEAD BLOCK: {BlockList [i]}");

				BlockList.DeleteBlock (BlockList [i]);

				removedDeadBlocks = true;
				--i;
			}

			if (removedDeadBlocks) {
				BlockList.ComputeOffsets ();

				BlockList.Dump ();
			}

			for (int i = 0; i < BlockList.Count - 1; i++) {
				if (BlockList [i].Type != BasicBlock.BlockType.Branch)
					continue;

				var lastInstruction = BlockList [i].LastInstruction;
				var nextInstruction = BlockList [i + 1].FirstInstruction;
				if (lastInstruction.OpCode.Code != Code.Br && lastInstruction.OpCode.Code != Code.Br_S)
					continue;
				if ((Instruction)lastInstruction.Operand != nextInstruction)
					continue;

				Context.LogMessage ($"ELIMINATE DEAD JUMP: {lastInstruction}");

				BlockList.AdjustJumpTargets (lastInstruction, nextInstruction);

				removedDeadBlocks = true;

				if (BlockList [i].Count == 1)
					BlockList.DeleteBlock (BlockList [i--]);
				else {
					BlockList.RemoveInstruction (BlockList [i], lastInstruction);
					BlockList [i].Type = BasicBlock.BlockType.Normal;
				}
			}

			if (removedDeadBlocks) {
				BlockList.ComputeOffsets ();

				BlockList.Dump ();
			}

			Context.LogMessage ($"DONE ELIMINATING DEAD BLOCKS");

			return removedDeadBlocks;
		}
	}
}
