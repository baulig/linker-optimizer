//
// BasicBlockList.cs
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
using Mono.Cecil.Cil;

namespace Mono.Linker.Conditionals
{
	public class BasicBlockList
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public MethodBody Body {
			get;
		}

		readonly Dictionary<Instruction, BasicBlock> _bb_by_instruction;
		readonly List<BasicBlock> _block_list;
		int _next_block_id;

		public IReadOnlyList<BasicBlock> Blocks => _block_list;

		public BasicBlockList (BasicBlockScanner scanner, MethodBody body)
		{
			Scanner = scanner;
			Body = body;

			_bb_by_instruction = new Dictionary<Instruction, BasicBlock> ();
			_block_list = new List<BasicBlock> ();
		}

		public BasicBlock NewBlock (Instruction instruction)
		{
			var block = new BasicBlock (++_next_block_id, instruction);
			_bb_by_instruction.Add (instruction, block);
			_block_list.Add (block);
			return block;
		}

		void ReplaceBlock (ref BasicBlock block, IList<Instruction> instructions)
		{
			if (instructions.Count < 1)
				throw new ArgumentOutOfRangeException ();

			var blockIndex = _block_list.IndexOf (block);
			var oldInstruction = block.Instructions [0];
			oldInstruction.Offset = -1;

			_bb_by_instruction.Remove (oldInstruction);

			block = new BasicBlock (++_next_block_id, instructions);
			_block_list [blockIndex] = block;
			_bb_by_instruction.Add (instructions [0], block);

			AdjustJumpTargets (oldInstruction, instructions [0]);
		}

		void AdjustJumpTargets (Instruction oldTarget, Instruction newTarget)
		{
			foreach (var instruction in Body.Instructions) {
				if (instruction.OpCode.OperandType == OperandType.InlineSwitch) {
					var labels = (Instruction [])instruction.Operand;
					for (int i = 0; i < labels.Length; i++) {
						if (labels [i] != oldTarget)
							continue;
						labels [i] = newTarget ?? throw CannotRemoveTarget;
					}
					continue;
				}
				if (instruction.OpCode.OperandType != OperandType.InlineBrTarget &&
				    instruction.OpCode.OperandType != OperandType.ShortInlineBrTarget)
					continue;
				if (instruction.Operand != oldTarget)
					continue;
				instruction.Operand = newTarget ?? throw CannotRemoveTarget;
			}

			foreach (var handler in Body.ExceptionHandlers) {
				if (handler.TryStart == oldTarget)
					handler.TryStart = newTarget ?? throw CannotRemoveTarget;
				if (handler.TryEnd == oldTarget)
					handler.TryEnd = newTarget ?? throw CannotRemoveTarget;
				if (handler.HandlerStart == oldTarget)
					handler.HandlerStart = newTarget ?? throw CannotRemoveTarget;
				if (handler.HandlerEnd == oldTarget)
					handler.HandlerEnd = newTarget ?? throw CannotRemoveTarget;
				if (handler.FilterStart == oldTarget)
					handler.FilterStart = newTarget ?? throw CannotRemoveTarget;
			}
		}

		public void Initialize ()
		{
			_bb_by_instruction.Clear ();
			_block_list.Clear ();
			_next_block_id = 0;

			foreach (var handler in Body.ExceptionHandlers) {
				if (handler.TryStart != null)
					EnsureExceptionBlock (BasicBlockType.Try, handler.TryStart, handler);
				if (handler.HandlerStart != null)
					EnsureExceptionBlock (BasicBlockType.Catch, handler.HandlerStart, handler);
				if (handler.HandlerEnd != null)
					EnsureExceptionBlock (BasicBlockType.Normal, handler.HandlerEnd, handler);
				if (handler.FilterStart != null)
					EnsureExceptionBlock (BasicBlockType.Filter, handler.FilterStart, handler);
			}

			foreach (var instruction in Body.Instructions) {
				switch (instruction.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					EnsureBlock (BasicBlockType.Normal, instruction, (Instruction)instruction.Operand);
					break;
				case OperandType.InlineSwitch:
					foreach (var label in (Instruction [])instruction.Operand)
						EnsureBlock (BasicBlockType.Normal, instruction, label);
					break;
				}
			}
		}

		void EnsureExceptionBlock (BasicBlockType type, Instruction target, ExceptionHandler handler)
		{
			if (!_bb_by_instruction.TryGetValue (target, out var block)) {
				block = new BasicBlock (++_next_block_id, type, target);
				_bb_by_instruction.Add (target, block);
				_block_list.Add (block);
			}
			if (block.Type == BasicBlockType.Normal)
				block.Type = type;
			else if (block.Type != type)
				throw new MartinTestException ();
			block.ExceptionHandlers.Add (handler);
			block.AddJumpOrigin (new JumpOrigin (block, handler));
		}

		internal void EnsureBlock (BasicBlockType type, Instruction origin, Instruction target)
		{
			if (!_bb_by_instruction.TryGetValue (target, out var block)) {
				block = new BasicBlock (++_next_block_id, type, target);
				_bb_by_instruction.Add (target, block);
				_block_list.Add (block);
			}
			block.AddJumpOrigin (new JumpOrigin (block, origin));
		}

		Exception CannotRemoveTarget => throw new NotSupportedException ("Attempted to remove a basic block that's being jumped to.");

		public bool SplitBlockAt (ref BasicBlock block, int position)
		{
			if (block.Instructions.Count < position)
				throw new ArgumentOutOfRangeException ();
			if (block.Instructions.Count == position)
				return false;

			var blockIndex = _block_list.IndexOf (block);

			var previousInstructions = block.GetInstructions (0, position);
			var nextInstructions = block.GetInstructions (position);

			var previousBlock = new BasicBlock (++_next_block_id, previousInstructions);
			_block_list [blockIndex] = previousBlock;
			_bb_by_instruction [previousInstructions [0]] = previousBlock;

			block = new BasicBlock (++_next_block_id, nextInstructions);
			_block_list.Insert (blockIndex + 1, block);
			_bb_by_instruction.Add (nextInstructions [0], block);
			return true;
		}

		public int Count => _block_list.Count;

		public BasicBlock this [int index] => _block_list [index];

		public BasicBlock GetBlock (Instruction instruction) => _bb_by_instruction [instruction];

		public bool TryGetBlock (Instruction instruction, out BasicBlock block)
		{
			return _bb_by_instruction.TryGetValue (instruction, out block);
		}

		public bool HasBlock (Instruction instruction) => _bb_by_instruction.ContainsKey (instruction);

		public int IndexOf (BasicBlock block) => _block_list.IndexOf (block);

		public void ComputeOffsets ()
		{
			var offset = 0;
			foreach (var instruction in Body.Instructions) {
				instruction.Offset = offset;
				offset += instruction.GetSize ();
			}

			_block_list.Sort ((first, second) => first.FirstInstruction.Offset.CompareTo (second.FirstInstruction.Offset));

			for (int i = 0; i < _block_list.Count; i++)
				_block_list [i].Index = i;
		}

		internal void ClearFlowInformation ()
		{
			foreach (var block in _block_list)
				block.ClearFlowInformation ();
		}

		public void Dump ()
		{
			Scanner.Context.LogMessage (MessageImportance.Low, $"BLOCK DUMP ({Body.Method})");
			foreach (var block in _block_list) {
				Dump (block);
			}
		}

		public void Dump (BasicBlock block)
		{
			Scanner.Context.LogMessage (MessageImportance.Low, $"{block}:");
			Scanner.LogDebug (0, "  ", null, block.JumpOrigins);
			Scanner.LogDebug (0, "  ", null, block.Instructions);
		}

		public void RemoveInstruction (BasicBlock block, Instruction instruction)
		{
			var position = block.IndexOf (instruction);
			RemoveInstructionAt (block, position);
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
		}

		public void RemoveInstructionAt (ref BasicBlock block, int position)
		{
			if (block.Count < 2)
				throw new InvalidOperationException ("Basic block must have at least one instruction in it.");

			var instruction = block.Instructions [position];
			instruction.Offset = -1;

			Body.Instructions.Remove (instruction);

			if (position > 0) {
				block.RemoveInstructionAt (position);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToList ();
			instructions.RemoveAt (0);
			ReplaceBlock (ref block, instructions);
		}

		public void InsertInstructionAt (ref BasicBlock block, int position, Instruction instruction)
		{
			if (position < 0 || position > block.Count)
				throw new ArgumentOutOfRangeException (nameof (position));

			int index;
			if (position == block.Count) {
				// Appending to the end.
				index = Body.Instructions.IndexOf (block.LastInstruction);
				Body.Instructions.Insert (index + 1, instruction);
				block.AddInstruction (instruction);
				return;
			}

			index = Body.Instructions.IndexOf (block.Instructions [position]);
			Body.Instructions.Insert (index, instruction);

			if (position > 0) {
				block.InsertAt (position, instruction);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToList ();
			instructions.Insert (0, instruction);
			ReplaceBlock (ref block, instructions);
		}

		public void ReplaceInstructionAt (ref BasicBlock block, int position, Instruction instruction)
		{
			var index = Body.Instructions.IndexOf (block.Instructions [position]);
			Body.Instructions [index] = instruction;
			instruction.Offset = -1;

			if (position > 0) {
				block.RemoveInstructionAt (position);
				block.InsertAt (position, instruction);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToArray ();
			instructions [position] = instruction;

			ReplaceBlock (ref block, instructions);
		}

		public void DeleteBlock (ref BasicBlock block)
		{
			var firstInstruction = block.FirstInstruction;
			var startIndex = Body.Instructions.IndexOf (firstInstruction);
			for (int i = 0; i < block.Count; i++) {
				Body.Instructions [startIndex].Offset = -1;
				Body.Instructions.RemoveAt (startIndex);
			}
			var blockIndex = _block_list.IndexOf (block);
			var nextInstruction = blockIndex + 1 < Count ? _block_list [blockIndex + 1].FirstInstruction : null;
			_block_list.RemoveAt (blockIndex);
			_bb_by_instruction.Remove (firstInstruction);
			AdjustJumpTargets (firstInstruction, nextInstruction);
			block = null;
		}
	}
}
