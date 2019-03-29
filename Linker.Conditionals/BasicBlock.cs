﻿//
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
			get; set;
		}

		public BranchType BranchType {
			get;
			private set;
		}

		public LinkerConditional LinkerConditional {
			get; set;
		}

		public int Count => _instructions.Count;

		public IReadOnlyList<Instruction> Instructions => _instructions;

		public Instruction FirstInstruction => _instructions [0];

		public Instruction LastInstruction => _instructions [_instructions.Count - 1];

		readonly List<Instruction> _instructions = new List<Instruction> ();

		public BasicBlock (int index, Instruction instruction)
		{
			Index = index;
			BranchType = BranchType.None;

			AddInstruction (instruction);
		}

		public BasicBlock (int index, IList<Instruction> instructions)
		{
			Index = index;
			BranchType = BranchType.None;

			if (instructions.Count < 1)
				throw new ArgumentOutOfRangeException ();

			AddInstructions (instructions);
		}

		void Update ()
		{
			BranchType = CecilHelper.GetBranchType (LastInstruction);
		}

		public void AddInstruction (Instruction instruction)
		{
			if (BranchType != BranchType.None)
				throw new NotSupportedException ();

			_instructions.Add (instruction);
			Update ();
		}

		public void AddInstructions (IList<Instruction> instructions)
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
			if (position == _instructions.Count)
				Update ();
		}

		public void InsertAt (int position, Instruction instruction)
		{
			if (position == 0)
				throw new ArgumentOutOfRangeException (nameof (position), "Cannot replace first instruction in basic block.");
			_instructions.Insert (position, instruction);
			if (position == _instructions.Count - 1)
				Update ();
		}

		public int IndexOf (Instruction instruction)
		{
			return _instructions.IndexOf (instruction);
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

		public override string ToString ()
		{
			return $"[BB {Index} ({BranchType}): {FirstInstruction.OpCode.Code}]";
		}
	}
}
