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

		void CloseBlock (ref BasicBlock block, BranchType branch, Instruction target)
		{
			if (!BlockList.HasBlock (target))
				BlockList.NewBlock (target);
			block = null;
		}

		void MarkTargetBlock (Dictionary<Instruction, BasicBlock> dict, Instruction target)
		{
			if (BlockList.HasBlock (target))
				return;
			if (!dict.TryGetValue (target, out var targetBlock)) {
				BlockList.NewBlock (target);
				return;
			}

			var index = targetBlock.IndexOf (target);
			Context.LogMessage ($"EXISTING TARGET: {targetBlock} {index} {target}");

			BlockList.SplitBlockAt (ref targetBlock, index);
		}

		bool Scan ()
		{
			Context.LogMessage ($"SCAN: {Method}");

			BasicBlock bb = null;
			var block_by_ins = new Dictionary<Instruction, BasicBlock> ();

			for (int i = 0; i < Method.Body.Instructions.Count; i++) {
				var instruction = Method.Body.Instructions [i];

				if (bb == null) {
					if (BlockList.TryGetBlock (instruction, out bb)) {
						Context.LogMessage ($"  KNOWN BB: {bb}");
					} else {
						bb = BlockList.NewBlock (instruction);
						Context.LogMessage ($"  NEW BB: {bb}");
					}
				} else if (BlockList.TryGetBlock (instruction, out var newBB)) {
					if (bb.BranchType != BranchType.None)
						throw new MartinTestException ();
					Context.LogMessage ($"  KNOWN BB: {bb} -> {newBB}");
					bb = newBB;
				} else {
					bb.AddInstruction (instruction);
				}

				block_by_ins.Add (instruction, bb);

				if (instruction.OpCode.OperandType == OperandType.InlineMethod) {
					Context.LogMessage ($"    CALL: {CecilHelper.Format (instruction)}");
					HandleCall (ref bb, ref i, instruction);
					continue;
				}

				var type = CecilHelper.GetBranchType (instruction);
				Context.LogMessage ($"    INS: {CecilHelper.Format (instruction)} {type}");
				switch (type) {
				case BranchType.None:
					break;
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
				case BranchType.Jump:
					MarkTargetBlock (block_by_ins, (Instruction)instruction.Operand);
					bb = null;
					break;

				case BranchType.Switch:
					foreach (var label in (Instruction [])instruction.Operand)
						MarkTargetBlock (block_by_ins, label);
					bb = null;
					break;

				case BranchType.Exit:
				case BranchType.Return:
					bb = null;
					break;

				default:
					throw new NotSupportedException ();
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

			bool hasLoad;
			TypeDefinition instanceType;
			if (CecilHelper.IsSimpleLoad (argument)) {
				if (bb.Instructions.Count > 2)
					BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);
				instanceType = CecilHelper.GetWeakInstanceArgument (bb.Instructions [1]);
				hasLoad = true;
			} else {
				BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 1);
				instanceType = CecilHelper.GetWeakInstanceArgument (bb.Instructions [0]);
				hasLoad = false;
			}

			bb.LinkerConditional = new IsWeakInstanceOfConditional (BlockList, instanceType, hasLoad);

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

			var feature = CecilHelper.GetFeatureArgument (bb.FirstInstruction);
			bb.LinkerConditional = new IsFeatureSupportedConditional (BlockList, feature);

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
			var type = CecilHelper.GetBranchType (next);

			if (type == BranchType.None) {
				bb = null;
				return;
			}

			bb.AddInstruction (next);
			index++;

			switch (type) {
			case BranchType.False:
			case BranchType.True:
				CloseBlock (ref bb, type, (Instruction)next.Operand);
				break;
			case BranchType.Return:
				bb = null;
				break;
			default:
				throw new MartinTestException ($"UNKNOWN BRANCH TYPE: {type} {next.OpCode}");
			}
		}

		public void RewriteConditionals ()
		{
			Context.LogMessage ($"REWRITE CONDITIONALS");

			BlockList.Dump ();

			var foundConditionals = false;

			foreach (var block in BlockList.Blocks.ToArray ()) {
				if (block.LinkerConditional != null) {
					RewriteLinkerConditional (block);
					block.LinkerConditional = null;
					foundConditionals = true;
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

		void RewriteLinkerConditional (BasicBlock block)
		{
			Context.LogMessage ($"REWRITE LINKER CONDITIONAL: {block.LinkerConditional}");

			BlockList.Dump ();

			block.LinkerConditional.RewriteConditional (ref block);

			BlockList.ComputeOffsets ();

			BlockList.Dump ();

			Context.LogMessage ($"DONE REWRITING LINKER CONDITIONAL");
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

				switch (block.BranchType) {
				case BranchType.None:
					markNextBlock = true;
					continue;
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
					marked.Add (BlockList.GetBlock ((Instruction)block.LastInstruction.Operand));
					markNextBlock = true;
					break;
				case BranchType.Exit:
				case BranchType.Return:
					markNextBlock = false;
					break;
				case BranchType.Jump:
					marked.Add (BlockList.GetBlock ((Instruction)block.LastInstruction.Operand));
					markNextBlock = false;
					break;
				case BranchType.Switch:
					foreach (var label in (Instruction [])block.LastInstruction.Operand)
						marked.Add (BlockList.GetBlock (label));
					markNextBlock = true;
					break;
				}
			}

			foreach (var handler in Method.Body.ExceptionHandlers) {
				if (handler.TryStart != null)
					marked.Add (BlockList.GetBlock (handler.TryStart));
				if (handler.TryEnd != null)
					marked.Add (BlockList.GetBlock (handler.TryEnd));
				if (handler.HandlerStart != null)
					marked.Add (BlockList.GetBlock (handler.HandlerStart));
				if (handler.HandlerEnd != null)
					marked.Add (BlockList.GetBlock (handler.HandlerEnd));
				if (handler.FilterStart != null)
					marked.Add (BlockList.GetBlock (handler.FilterStart));
			}


			var removedDeadBlocks = false;
			for (int i = 0; i < BlockList.Count; i++) {
				if (marked.Contains (BlockList [i]))
					continue;

				Context.LogMessage ($"  DEAD BLOCK: {BlockList [i]}");

				var block = BlockList [i];
				BlockList.DeleteBlock (ref block);

				removedDeadBlocks = true;
				--i;
			}

			if (removedDeadBlocks) {
				BlockList.ComputeOffsets ();

				BlockList.Dump ();
			}

			for (int i = 0; i < BlockList.Count - 1; i++) {
				if (BlockList [i].BranchType != BranchType.Jump)
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

				if (BlockList [i].Count == 1) {
					var block = BlockList [i--];
					BlockList.DeleteBlock (ref block);
				} else {
					BlockList.RemoveInstruction (BlockList [i], lastInstruction);
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
