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
		public MartinContext Context {
			get;
		}

		public MethodBody Body {
			get;
		}

		readonly Dictionary<Instruction, BasicBlock> _bb_by_instruction;
		readonly List<BasicBlock> _block_list;
		int _next_block_id;

		public IReadOnlyList<BasicBlock> Blocks => _block_list;

		public BasicBlockList (MartinContext context, MethodBody body)
		{
			Context = context;
			Body = body;

			_bb_by_instruction = new Dictionary<Instruction, BasicBlock> ();
			_block_list = new List<BasicBlock> ();
		}

		public BasicBlock NewBlock (Instruction instruction, BasicBlock.BlockType type = BasicBlock.BlockType.Normal)
		{
			var block = new BasicBlock (++_next_block_id, type, instruction);
			_bb_by_instruction.Add (instruction, block);
			_block_list.Add (block);
			return block;
		}

		public void RemoveBlock (BasicBlock block)
		{
			_block_list.Remove (block);
			_bb_by_instruction.Remove (block.Instructions [0]);
		}

		public void ReplaceBlock (ref BasicBlock block, BasicBlock.BlockType type, IList<Instruction> instructions)
		{
			if (instructions.Count < 1)
				throw new ArgumentOutOfRangeException ();

			var blockIndex = _block_list.IndexOf (block);
			_bb_by_instruction.Remove (block.Instructions [0]);

			block = new BasicBlock (++_next_block_id, type, instructions);
			_block_list [blockIndex] = block;
			_bb_by_instruction.Add (instructions [0], block);
		}

		public bool SplitBlockAt (ref BasicBlock block, int position)
		{
			if (block.Instructions.Count < position)
				throw new ArgumentOutOfRangeException ();
			if (block.Instructions.Count == position)
				return false;

			var blockIndex = _block_list.IndexOf (block);

			var previousInstructions = block.GetInstructions (0, position);
			var nextInstructions = block.GetInstructions (position);

			var previousBlock = new BasicBlock (++_next_block_id, BasicBlock.BlockType.Normal, previousInstructions);
			_block_list [blockIndex] = previousBlock;
			_bb_by_instruction [previousInstructions [0]] = previousBlock;

			block = new BasicBlock (++_next_block_id, block.Type, nextInstructions);
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

		public void ComputeOffsets ()
		{
			var offset = 0;
			foreach (var instruction in Body.Instructions) {
				instruction.Offset = offset;
				offset += instruction.GetSize ();
			}

			_block_list.Sort ((first, second) => first.FirstInstruction.Offset.CompareTo (second.FirstInstruction.Offset));
		}


		public void Dump ()
		{
			Context.LogMessage ($"BLOCK DUMP ({Body.Method})");
			foreach (var block in _block_list) {
				Dump (block);
			}
		}

		public void Dump (BasicBlock block)
		{
			Context.LogMessage ($"{block}:");
			foreach (var instruction in block.Instructions) {
				if (instruction.OpCode.Code == Code.Ldstr)
					Context.LogMessage ($"  {instruction.OpCode}");
				else
					Context.LogMessage ($"  {instruction}");
			}
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
		}

		public void InsertInstructionAt (ref BasicBlock block, int position, Instruction instruction)
		{
			if (position < 0 || position > block.Count)
				throw new ArgumentOutOfRangeException (nameof (position));

			if (position == block.Count) {
				// Appending to the end.
				var index = Body.Instructions.IndexOf (block.LastInstruction);
				Body.Instructions.Insert (index + 1, instruction);
				block.AddInstruction (instruction);
				return;
			} else if (position > 0) {
				var index = Body.Instructions.IndexOf (block.Instructions [position]);
				Body.Instructions.Insert (index, instruction);
				block.InsertAt (position, instruction);
				return;
			}

			/*
			 * Our logic assumes that the first instruction in a basic block will never change
			 * (because basic blocks are referenced by their first instruction).
			 */

			var instructions = block.Instructions.ToList ();
			instructions.Insert (0, instruction);
			ReplaceBlock (ref block, block.Type, instructions);
		}

		public void ReplaceInstructionAt (ref BasicBlock block, int position, Instruction instruction)
		{
			var index = Body.Instructions.IndexOf (block.Instructions [position]);
			Body.Instructions [index] = instruction;

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

			ReplaceBlock (ref block, block.Type, instructions);
		}
	}
}
