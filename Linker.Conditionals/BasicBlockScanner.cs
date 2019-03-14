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
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Conditionals
{
	public class BasicBlockScanner
	{
		public MethodBody Body {
			get;
		}

		public BasicBlockScanner (MethodBody body)
		{
			Body = body;
		}

		public static bool ThrowOnError;

		static void Debug (string message)
		{
			Console.Error.WriteLine (message);
		}

		public void Scan ()
		{
			Scan (Body.Instructions.ToArray ());
		}

		bool Scan (Instruction [] instructions)
		{
			Debug ($"SCAN: {Body} {instructions.Length}");

			if (Body.ExceptionHandlers.Count > 0) {
				if (!ThrowOnError)
					return false;
				throw new NotSupportedException ($"We don't support exception handlers yet: {Body.Method.FullName}");
			}

			BasicBlock bb = null;
			var bb_by_offset = new Dictionary<int, BasicBlock> ();

			for (int i = 0; i < instructions.Length; i++) {
				if (bb == null) {
					if (bb_by_offset.TryGetValue (instructions [i].Offset, out bb)) {
						Debug ($"    KNOWN BB: {bb}");
					} else {
						bb = new BasicBlock (bb_by_offset.Count, instructions [i]);
						bb_by_offset.Add (instructions[i].Offset, bb);
						Debug ($"    NEW BB: {bb}");
					}
	 			}

				Debug ($"        {instructions [i]}");

				switch (instructions[i].OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					var target = (Instruction)instructions [i].Operand;
					if (!bb_by_offset.ContainsKey (target.Offset)) {
						var target_bb = new BasicBlock (bb_by_offset.Count, target);
						Debug ($"    JUMP TARGET BB: {target}");
						bb_by_offset.Add (target.Offset, target_bb);
						bb = null;
					}
					break;
				case OperandType.InlineSwitch:
					if (!ThrowOnError)
						return false;
					throw new NotSupportedException ($"We don't support `switch` statements yet: {Body.Method.FullName}");
				}

				if (instructions [i].OpCode == OpCodes.Throw) {
					Debug ($"    THROW");
					bb = null;
				}
			}

			return true;
		}

		class BasicBlock
		{
			public int Index {
				get;
			}

			public int Offset => Instruction.Offset;

			public Instruction Instruction {
				get;
			}

			public BasicBlock (int index, Instruction instruction)
			{
				Index = index;
				Instruction = instruction;
			}

			public override string ToString ()
			{
				return $"[BB {Index} (0x{Offset:x2}): {Instruction}]";
			}
		}
	}
}
