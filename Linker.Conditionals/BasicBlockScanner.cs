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

		public MethodBody Body {
			get;
		}

		public bool FoundConditionals {
			get; private set;
		}

		public BasicBlockList BlockList {
			get;
		}

		BasicBlockScanner (MartinContext context, MethodBody body)
		{
			Context = context;
			Body = body;

			BlockList = new BasicBlockList (context, body);
		}

		public static bool ThrowOnError;

		public static BasicBlockScanner Scan (MartinContext context, MethodBody body)
		{
			var scanner = new BasicBlockScanner (context, body);
			if (!scanner.Scan ())
				return null;
			return scanner;
		}

		public IReadOnlyCollection<BasicBlock> BasicBlocks => BlockList.Blocks;

		bool Scan ()
		{
			Context.LogMessage ($"SCAN: {Body.Method}");

			if (Body.ExceptionHandlers.Count > 0) {
				if (!ThrowOnError)
					return false;
				throw new NotSupportedException ($"We don't support exception handlers yet: {Body.Method.FullName}");
			}

			BasicBlock bb = null;

			for (int i = 0; i < Body.Instructions.Count; i++) {
				var instruction = Body.Instructions [i];

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
					var target = (Instruction)instruction.Operand;
					if (!BlockList.HasBlock (target)) {
						Context.LogMessage ($"    JUMP TARGET BB: {target}");
						BlockList.NewBlock (target);
						bb = null;
					}
					continue;
				case OperandType.InlineSwitch:
					if (!ThrowOnError)
						return false;
					throw new NotSupportedException ($"We don't support `switch` statements yet: {Body.Method.FullName}");
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
				if (genericInstance.ElementMethod == Context.IsWeakInstanceOf) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					HandleWeakInstanceOf (ref bb, ref index, conditionalType);
				}
			} else if (target == Context.IsFeatureSupported) {
				HandleIsFeatureSupported (ref bb, ref index);
			}
		}

		void HandleWeakInstanceOf (ref BasicBlock bb, ref int index, TypeDefinition type)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= Body.Instructions.Count)
				throw new NotSupportedException ();

			var argument = Body.Instructions [index - 1];

			Context.LogMessage ($"WEAK INSTANCE OF: {bb} {index} {type} - {argument}");

			BlockList.Dump ();

			if (CecilHelper.IsSimpleLoad (argument)) {
				if (bb.Instructions.Count > 2)
					BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 2);
				bb.Type = BasicBlock.BlockType.SimpleWeakInstanceOf;
			} else {
				Context.LogMessage ($"    COMPLICATED LOAD: {argument}");
				BlockList.SplitBlockAt (ref bb, bb.Instructions.Count - 1);
				bb.Type = BasicBlock.BlockType.WeakInstanceOf;
			}

			BlockList.Dump ();

			FoundConditionals = true;

			if (index + 1 >= Body.Instructions.Count)
				throw new NotSupportedException ();

			LookAheadAfterConditional (ref bb, ref index);
		}

		void HandleIsFeatureSupported (ref BasicBlock bb, ref int index)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= Body.Instructions.Count)
				throw new NotSupportedException ();

			var argument = Body.Instructions [index - 1];

			Context.LogMessage ($"IS FEATURE SUPPORTED: {bb} {index} - {argument}");

			BlockList.Dump ();

			var feature = CecilHelper.GetFeatureArgument (argument);
			var evaluated = Context.IsFeatureEnabled (feature);
			Context.LogMessage ($"IS FEATURE SUPPORTED #1: {bb} {index} - {feature} {evaluated}");

			FoundConditionals = true;

			bb.Type = BasicBlock.BlockType.IsFeatureSupported;

			LookAheadAfterConditional (ref bb, ref index);
		}

		void LookAheadAfterConditional (ref BasicBlock bb, ref int index)
		{
			if (index + 1 >= Body.Instructions.Count)
				throw new NotSupportedException ();

			var next = Body.Instructions [index + 1];
			if (next.OpCode.OperandType == OperandType.InlineBrTarget || next.OpCode.OperandType == OperandType.ShortInlineBrTarget) {
				bb.AddInstruction (next);
				index++;

				var target = (Instruction)next.Operand;
				if (!BlockList.HasBlock (target)) {
					Context.LogMessage ($"    JUMP TARGET BB: {target}");
					BlockList.NewBlock (target);
				}
			} else if (CecilHelper.IsStoreInstruction (next) || next.OpCode.Code == Code.Ret) {
				bb.AddInstruction (next);
				index++;
			}

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

		bool EvaluateWeakInstanceOf (Instruction instruction)
		{
			var target = ((GenericInstanceMethod)instruction.Operand).GenericArguments [0].Resolve ();
			return Context.Context.Annotations.IsMarked (target);
		}

		void RewriteWeakInstanceOf (BasicBlock block)
		{
			Context.LogMessage ($"REWRITE WEAK INSTANCE");

			BlockList.Dump ();

			bool evaluated;
			int nextIndex;
			switch (block.Type) {
			case BasicBlock.BlockType.WeakInstanceOf:
				evaluated = EvaluateWeakInstanceOf (block.Instructions [0]);
				nextIndex = 1;
				break;
			case BasicBlock.BlockType.SimpleWeakInstanceOf:
				evaluated = EvaluateWeakInstanceOf (block.Instructions [1]);
				nextIndex = 2;
				break;
			default:
				throw new NotSupportedException ();
			}

			Context.LogMessage ($"REWRITE WEAK INSTANCE OF: {evaluated} {block}");

			if (block.Instructions.Count == nextIndex + 1) {
				var next = block.Instructions [nextIndex];
				switch (next.OpCode.Code) {
				case Code.Br:
				case Code.Br_S:
					Context.LogMessage ($"  UNCONDITIONAL BRANCH");
					break;
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
					Context.LogMessage ($"  NO BRANCH");
					break;
				}
			}

			RewriteConditional (ref block, evaluated);

			BlockList.Dump ();
		}

		void RewriteIsFeatureSupported (BasicBlock block)
		{
			Context.LogMessage ($"REWRITE IS FEATURE SUPPORTED");

			BlockList.Dump ();

			var feature = CecilHelper.GetFeatureArgument (block.Instructions [0]);
			var evaluated = Context.IsFeatureEnabled (feature);

			Context.LogMessage ($"REWRITE IS FEATURE SUPPORTED #1: {feature} {evaluated}");

			if (block.Instructions.Count == 3) {
				var next = block.Instructions [2];
				switch (next.OpCode.Code) {
				case Code.Br:
				case Code.Br_S:
					Context.LogMessage ($"  UNCONDITIONAL BRANCH");
					break;
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
					Context.LogMessage ($"  NO BRANCH");
					break;
				}
			}

			RewriteConditional (ref block, evaluated);

			BlockList.Dump ();
		}

		void RewriteBranch (ref BasicBlock block, bool evaluated)
		{
			var target = (Instruction)block.LastInstruction.Operand;

			Context.LogMessage ($"  REWRITING BRANCH: {block} {evaluated} {target}");

			BlockList.Dump (block);

			var index = Body.Instructions.IndexOf (block.Instructions [0]);

			var newInstructions = new List<Instruction> ();
			var pop = Instruction.Create (OpCodes.Pop);
			var branch = Instruction.Create (OpCodes.Br, target);
			var type = BasicBlock.BlockType.Normal;

			switch (block.Type) {
			case BasicBlock.BlockType.SimpleWeakInstanceOf:
			case BasicBlock.BlockType.IsFeatureSupported:
				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);
				if (evaluated) {
					newInstructions.Add (branch);
					type = BasicBlock.BlockType.Branch;
				}
				break;
			case BasicBlock.BlockType.WeakInstanceOf:
				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);
				newInstructions.Add (pop);
				if (evaluated) {
					newInstructions.Add (branch);
					type = BasicBlock.BlockType.Branch;
				}
				break;
			default:
				throw new NotSupportedException ();
			}

			for (int i = 0; i < newInstructions.Count; i++)
				Body.Instructions.Insert (index + i, newInstructions [i]);
			if (newInstructions.Count == 0)
				BlockList.RemoveBlock (block);
			else {
				BlockList.ReplaceBlock (ref block, type, newInstructions.ToArray ());
			}
		}

		void RewriteConditional (ref BasicBlock block, bool evaluated)
		{
			Context.LogMessage ($"  REWRITING CONDITIONAL");

			var index = Body.Instructions.IndexOf (block.Instructions [0]);

			var newInstructions = new List<Instruction> ();
			var constant = Instruction.Create (evaluated ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
			var pop = Instruction.Create (OpCodes.Pop);

			switch (block.Type) {
			case BasicBlock.BlockType.SimpleWeakInstanceOf:
			case BasicBlock.BlockType.IsFeatureSupported:
				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);
				Body.Instructions.Insert (index, constant);
				newInstructions.Add (constant);
				newInstructions.AddRange (block.GetInstructions (2));
				break;
			case BasicBlock.BlockType.WeakInstanceOf:
				Body.Instructions.RemoveAt (index);
				Body.Instructions.Insert (index, pop);
				Body.Instructions.Insert (index + 1, constant);
				newInstructions.Add (pop);
				newInstructions.Add (constant);
				newInstructions.AddRange (block.GetInstructions (1));
				break;
			default:
				throw new NotSupportedException ();
			}

			BlockList.ReplaceBlock (ref block, BasicBlock.BlockType.Normal, newInstructions.ToArray ());
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

				switch (block.Type) {
				case BasicBlock.BlockType.Branch:
					markNextBlock = false;
					break;
				case BasicBlock.BlockType.Normal:
					markNextBlock = true;
					break;
				default:
					throw new NotSupportedException ();
				}

				foreach (var instruction in block.Instructions) {
					switch (instruction.OpCode.OperandType) {
					case OperandType.InlineBrTarget:
					case OperandType.ShortInlineBrTarget:
						var target = (Instruction)instruction.Operand;
						marked.Add (BlockList.GetBlock ((Instruction)instruction.Operand));
						break;
					}
				}
			}

			var removedDeadBlocks = false;
			for (int i = 0; i < BlockList.Count; i++) {
				if (marked.Contains (BlockList [i]))
					continue;

				Context.LogMessage ($"  DEAD BLOCK: {BlockList [i]}");

				foreach (var instruction in BlockList [i].Instructions)
					Body.Instructions.Remove (instruction);

				BlockList.RemoveBlock (BlockList [i]);

				removedDeadBlocks = true;
			}

			for (int i = 0; i < BlockList.Count - 1; i++) {
				if (BlockList [i].Type != BasicBlock.BlockType.Branch)
					continue;

				var lastInstruction = BlockList [i].LastInstruction;
				if (lastInstruction.OpCode.Code != Code.Br && lastInstruction.OpCode.Code != Code.Br_S)
					continue;
				if ((Instruction)lastInstruction.Operand != BlockList [i + 1].FirstInstruction)
					continue;

				Context.LogMessage ($"ELIMINATE DEAD JUMP: {lastInstruction}");

				Body.Instructions.Remove (lastInstruction);

				if (BlockList [i].Instructions.Count == 1)
					BlockList.RemoveBlock (BlockList [i--]);
				else {
					BlockList [i].RemoveInstruction (lastInstruction);
					BlockList [i].Type = BasicBlock.BlockType.Normal;
				}
			}

			Context.LogMessage ($"DONE ELIMINATING DEAD BLOCKS");

			return removedDeadBlocks;
		}
	}
}
