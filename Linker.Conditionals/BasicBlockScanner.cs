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

		readonly Dictionary<Instruction, BasicBlock> _bb_by_instruction;
		readonly List<BasicBlock> _block_list;
		int _next_block_id;

		BasicBlockScanner (MartinContext context, MethodBody body)
		{
			Context = context;
			Body = body;

			_bb_by_instruction = new Dictionary<Instruction, BasicBlock> ();
			_block_list = new List<BasicBlock> ();
		}

		public static bool ThrowOnError;

		public static BasicBlockScanner Scan (MartinContext context, MethodBody body)
		{
			var scanner = new BasicBlockScanner (context, body);
			if (!scanner.Scan ())
				return null;
			return scanner;
		}

		public IReadOnlyCollection<BasicBlock> BasicBlocks => _bb_by_instruction.Values;

		BasicBlock NewBlock (Instruction instruction, BlockType type = BlockType.Normal)
		{
			var block = new BasicBlock (++_next_block_id, type, instruction);
			_bb_by_instruction.Add (instruction, block);
			_block_list.Add (block);
			return block;
		}

		void RemoveBlock (BasicBlock block)
		{
			_block_list.Remove (block);
			_bb_by_instruction.Remove (block.Instructions [0]);
		}

		void ReplaceBlock (ref BasicBlock block, BlockType type, params Instruction[] instructions)
		{
			if (instructions.Length < 1)
				throw new ArgumentOutOfRangeException ();

			var blockIndex = _block_list.IndexOf (block);

			block = new BasicBlock (++_next_block_id, type, instructions);
			_block_list [blockIndex] = block;
			_bb_by_instruction [instructions[0]] = block;
		}

		bool SplitBlockAt (ref BasicBlock block, int position)
		{
			if (block.Instructions.Count < position)
				throw new ArgumentOutOfRangeException ();
			if (block.Instructions.Count == position)
				return false;

			var blockIndex = _block_list.IndexOf (block);

			var previousInstructions = block.GetInstructions (0, position);
			var nextInstructions = block.GetInstructions (position);

			var previousBlock = new BasicBlock (++_next_block_id, BlockType.Normal, previousInstructions);
			_block_list [blockIndex] = previousBlock;
			_bb_by_instruction [previousInstructions [0]] = previousBlock;

			block = new BasicBlock (++_next_block_id, block.Type, nextInstructions);
			_block_list.Insert (blockIndex + 1, block);
			_bb_by_instruction.Add (nextInstructions [0], block);
			return true;
		}

		BasicBlock GetBlock (Instruction instruction)
		{
			return _bb_by_instruction [instruction];
		}

		bool Scan ()
		{
			Context.LogMessage ($"SCAN: {Body.Method}");

			if (Body.ExceptionHandlers.Count > 0) {
				if (!ThrowOnError)
					return false;
				throw new NotSupportedException ($"We don't support exception handlers yet: {Body.Method.FullName}");
			}

			BasicBlock bb = null;

			foreach (var instruction in Body.Instructions) {
				if (bb == null) {
					if (_bb_by_instruction.TryGetValue (instruction, out bb)) {
						Context.LogMessage ($"    KNOWN BB: {bb}");
					} else {
						bb = NewBlock (instruction);
						Context.LogMessage ($"    NEW BB: {bb}");
					}
	 			} else {
					bb.AddInstruction (instruction);
				}

				Context.LogMessage ($"        {instruction}");

				switch (instruction.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					if (bb.Type == BlockType.Normal)
						bb.Type = BlockType.Branch;
					var target = (Instruction)instruction.Operand;
					if (!_bb_by_instruction.ContainsKey (target)) {
						Context.LogMessage ($"    JUMP TARGET BB: {target}");
						NewBlock (target);
						bb = null;
					}
					break;
				case OperandType.InlineSwitch:
					if (!ThrowOnError)
						return false;
					throw new NotSupportedException ($"We don't support `switch` statements yet: {Body.Method.FullName}");
				case OperandType.InlineMethod:
					HandleCall (ref bb, instruction);
					break;
				}

				if (instruction.OpCode == OpCodes.Throw || instruction.OpCode == OpCodes.Rethrow) {
					Context.LogMessage ($"    THROW");
					bb.Type = BlockType.Branch;
					bb = null;
				} else if (instruction.OpCode == OpCodes.Ret) {
					Context.LogMessage ($"    RET");
					bb.Type = BlockType.Branch;
					bb = null;
				}
			}

			DumpBlocks ();

			ComputeOffsets ();

			DumpBlocks ();

			return true;
		}

		void DumpBlocks ()
		{
			Context.LogMessage ($"BLOCK DUMP");
			foreach (var block in _block_list) {
				Context.LogMessage ($"{block}:");
				foreach (var instruction in block.Instructions)
					Context.LogMessage ($"  {instruction}");
			}
		}

		void HandleCall (ref BasicBlock bb, Instruction instruction)
		{
			var target = (MethodReference)instruction.Operand;
			Context.LogMessage ($"    CALL: {target}");

			if (instruction.Operand is GenericInstanceMethod genericInstance) {
				if (genericInstance.ElementMethod == Context.IsWeakInstanceOf) {
					var conditionalType = genericInstance.GenericArguments[0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments[0]);
					HandleWeakInstanceOf (ref bb, instruction, conditionalType);
				}
			}
		}

		void HandleWeakInstanceOf (ref BasicBlock bb, Instruction instruction, TypeDefinition type)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();

			var argument = bb.Instructions [bb.Instructions.Count - 2];
			Context.LogMessage ($"    WEAK INSTANCE OF: {type} - {instruction} - {argument}");

			if (Context.Context.Annotations.IsMarked (type)) {
				Context.LogMessage ($"    IS MARKED!");
				return;
			}

			VerifyWeakInstanceOfArgument (argument);

			DumpBlocks ();

			SplitBlockAt (ref bb, bb.Instructions.Count - 2);

			bb.Type = BlockType.WeakInstanceOf;

			DumpBlocks ();

			FoundConditionals = true;
		}

		bool VerifyWeakInstanceOfArgument (Instruction argument)
		{
			if (argument.OpCode == OpCodes.Ldnull)
				return true;
			throw new NotSupportedException ($"Invalid opcode `{argument.OpCode}` used as weak instance target in `{Body.Method}`.");
		}

		public void RewriteConditionals ()
		{
			Context.LogMessage ($"REWRITE CONDITIONALS");

			DumpBlocks ();

			var foundConditionals = false;

			foreach (var block in _block_list.ToArray ()) {
				switch (block.Type) {
				case BlockType.Normal:
				case BlockType.Branch:
					continue;
				case BlockType.WeakInstanceOf:
					RewriteWeakInstanceOf (block);
					foundConditionals = true;
					break;
				default:
					throw new NotSupportedException ();
				}
			}

			if (!foundConditionals)
				return;

			ComputeOffsets ();

			DumpBlocks ();

			while (EliminateDeadBlocks ()) {  }

			DumpBlocks ();

			Context.LogMessage ($"DONE REWRITING CONDITIONALS");
		}

		void RewriteWeakInstanceOf (BasicBlock block)
		{
			var target = ((GenericInstanceMethod)block.Instructions [1].Operand).GenericArguments [0].Resolve ();
			var value = Context.Context.Annotations.IsMarked (target);

			var index = Body.Instructions.IndexOf (block.Instructions [0]);

			Context.LogMessage ($"REWRITE WEAK INSTANCE OF: {target} {value} {block}");

			var eliminateBranch = false;
			var rewriteBranch = false;
			var next = block.Instructions [2];
			if (next.OpCode == OpCodes.Br || next.OpCode == OpCodes.Br_S) {
				Context.LogMessage ($"  UNCONDITIONAL BRANCH");
			} else if (next.OpCode == OpCodes.Brfalse || next.OpCode == OpCodes.Brfalse_S) {
				Context.LogMessage ($"  BR FALSE");
				eliminateBranch = value;
				rewriteBranch = !value;
			} else if (next.OpCode == OpCodes.Brtrue || next.OpCode == OpCodes.Brtrue_S) {
				Context.LogMessage ($"  BR TRUE");
				eliminateBranch = !value;
				rewriteBranch = value;
			} else {
				Context.LogMessage ($"  NO BRANCH");
			}

			if (eliminateBranch) {
				Context.LogMessage ($"  ELIMINATING BRANCH");

				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);

				SplitBlockAt (ref block, 2);
			} else if (rewriteBranch) {
				Context.LogMessage ($"  REWRITE BRANCH");

				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);

				var branch = Instruction.Create (OpCodes.Br, (Instruction)next.Operand);
				Body.Instructions.Insert (index, branch);

				ReplaceBlock (ref block, BlockType.Branch, branch);
			} else {
				Context.LogMessage ($"  REWRITING CONDITIONAL");

				var constant = Instruction.Create (value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

				Body.Instructions.RemoveAt (index);
				Body.Instructions.RemoveAt (index);
				Body.Instructions.Insert (index, constant);

				var extraInstructions = block.GetInstructions (2);

				ReplaceBlock (ref block, BlockType.Normal, constant);
				block.AddInstructions (extraInstructions);
			}
		}

		bool EliminateDeadBlocks ()
		{
			Context.LogMessage ($"ELIMINATING DEAD BLOCKS");

			DumpBlocks ();

			var markNextBlock = true;
			var marked = new HashSet<BasicBlock> ();

			foreach (var block in _block_list) {
				if (markNextBlock)
					marked.Add (block);

				switch (block.Type) {
				case BlockType.Branch:
					markNextBlock = false;
					break;
				case BlockType.Normal:
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
						marked.Add (GetBlock ((Instruction)instruction.Operand));
						break;
					}
				}
			}

			var removedDeadBlocks = false;
			var allBlocks = _block_list.ToArray ();
			for (int i = 0; i < allBlocks.Length; i++) {
				if (marked.Contains (allBlocks [i]))
					continue;

				Context.LogMessage ($"  DEAD BLOCK: {allBlocks [i]}");

				RemoveBlock (allBlocks [i]);
				foreach (var instruction in allBlocks [i].Instructions)
					Body.Instructions.Remove (instruction);

				removedDeadBlocks = true;
			}

			Context.LogMessage ($"DONE ELIMINATING DEAD BLOCKS");

			return removedDeadBlocks;
		}

		void ComputeOffsets ()
		{
			var offset = 0;
			foreach (var instruction in Body.Instructions) {
				instruction.Offset = offset;
				offset += instruction.GetSize ();
			}

			foreach (var block in _block_list)
				block.ComputeOffsets ();

			_block_list.Sort ((first, second) => first.StartOffset.CompareTo (second.StartOffset));
		}

		public enum BlockType
		{
			Normal,
			Branch,
			WeakInstanceOf
		}

		public class BasicBlock
		{
			public int Index {
				get;
			}

			public BlockType Type {
				get; set;
			}

			public int StartOffset {
				get;
				private set;
			}

			public int EndOffset {
				get;
				private set;
			}

			public IReadOnlyList<Instruction> Instructions => _instructions;

			readonly List<Instruction> _instructions = new List<Instruction> ();

			public BasicBlock (int index, BlockType type, Instruction instruction)
			{
				Index = index;
				Type = type;
				StartOffset = instruction.Offset;
				EndOffset = instruction.Offset;

				AddInstruction (instruction);

			}

			public BasicBlock (int index, BlockType type, params Instruction[] instructions)
			{
				Index = index;
				Type = type;

				if (instructions.Length < 1)
					throw new ArgumentOutOfRangeException ();

				StartOffset = EndOffset = instructions [0].Offset;
				AddInstructions (instructions);
			}

			public void AddInstruction (Instruction instruction)
			{
				if (instruction.Offset < EndOffset)
					throw new ArgumentException ();

				_instructions.Add (instruction);
				EndOffset = instruction.Offset;
			}

			public void AddInstructions (params Instruction[] instructions)
			{
				foreach (var instruction in instructions)
					AddInstruction (instruction);
			}

			public Instruction[] GetInstructions (int offset, int count)
			{
				if (offset == _instructions.Count)
					return new Instruction [0];
				if (offset + count > _instructions.Count)
					throw new ArgumentOutOfRangeException ();

				var array = new Instruction [count];
				for (int i = 0; i < count; i++)
					array [i] = _instructions [offset + i];
				return array;
			}

			public Instruction[] GetInstructions (int offset)
			{
				return GetInstructions (offset, Instructions.Count - offset);
			}

			public void ComputeOffsets ()
			{
				StartOffset = _instructions [0].Offset;
				EndOffset = _instructions [_instructions.Count - 1].Offset;
			}

			public override string ToString ()
			{
				return $"[BB {Index}{((Type != BlockType.Normal ? $" ({Type})" : ""))}: 0x{StartOffset:x2} - 0x{EndOffset:x2}]";
			}
		}
	}
}
