//
// BasicBlock.cs
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
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Mono.Linker.Conditionals
{
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

		public int Count => _instructions.Count;

		public IReadOnlyList<Instruction> Instructions => _instructions;

		public Instruction FirstInstruction => _instructions [0];

		public Instruction LastInstruction => _instructions [_instructions.Count - 1];

		readonly List<Instruction> _instructions = new List<Instruction> ();

		public BasicBlock (int index, BlockType type, Instruction instruction)
		{
			Index = index;
			Type = type;
			StartOffset = instruction.Offset;
			EndOffset = instruction.Offset;

			AddInstruction (instruction);

		}

		public BasicBlock (int index, BlockType type, params Instruction [] instructions)
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

		public void AddInstructions (params Instruction [] instructions)
		{
			foreach (var instruction in instructions)
				AddInstruction (instruction);
		}

		public void RemoveInstruction (Instruction instruction)
		{
			var index = _instructions.IndexOf (instruction);
			RemoveInstructionAt (index);
		}

		public void RemoveInstructionAt (int position)
		{
			if (_instructions.Count < 2)
				throw new InvalidOperationException ();
			if (position == 0)
				throw new ArgumentOutOfRangeException (nameof (position), "Cannot replace first instruction in basic block.");
			_instructions.RemoveAt (position);
			ComputeOffsets ();
		}

		public void InsertAfter (Instruction position, Instruction instruction)
		{
			var index = _instructions.IndexOf (position);
			_instructions.Insert (index, instruction);
			ComputeOffsets ();
		}

		public void InsertAt (int position, Instruction instruction)
		{
			if (position == 0)
				throw new ArgumentOutOfRangeException (nameof (position), "Cannot replace first instruction in basic block.");
			_instructions.Insert (position, instruction);
		}

		public Instruction [] GetInstructions (int offset, int count)
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

		public Instruction [] GetInstructions (int offset)
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

		public enum BlockType
		{
			Normal,
			Branch,
			SimpleWeakInstanceOf,
			WeakInstanceOf,
			IsFeatureSupported
		}
	}
}
