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

		readonly Instruction [] Instructions;
		readonly Dictionary<int, Instruction> _ins_by_offset;
		readonly Dictionary<int, BasicBlock> _bb_by_offset;
		int _next_block_id;

		BasicBlockScanner (MartinContext context, MethodBody body)
		{
			Context = context;
			Body = body;

			Instructions = body.Instructions.ToArray ();
			_ins_by_offset = new Dictionary<int, Instruction> ();
			_bb_by_offset = new Dictionary<int, BasicBlock> ();
		}

		public static bool ThrowOnError;

		public static BasicBlockScanner Scan (MartinContext context, MethodBody body)
		{
			var scanner = new BasicBlockScanner (context, body);
			if (!scanner.Scan ())
				return null;
			return scanner;
		}

		public IReadOnlyCollection<BasicBlock> BasicBlocks => _bb_by_offset.Values;

		BasicBlock NewBlock (Instruction instruction, BlockType type = BlockType.Normal)
		{
			var block = new BasicBlock (++_next_block_id, type, instruction);
			_bb_by_offset.Add (instruction.Offset, block);
			return block;
		}

		void RemoveBlock (BasicBlock block)
		{
			_bb_by_offset.Remove (block.StartOffset);
		}

		bool Scan ()
		{
			Context.LogMessage ($"SCAN: {Body.Method} {Instructions.Length}");

			if (Body.ExceptionHandlers.Count > 0) {
				if (!ThrowOnError)
					return false;
				throw new NotSupportedException ($"We don't support exception handlers yet: {Body.Method.FullName}");
			}

			BasicBlock bb = null;

			foreach (var instruction in Instructions) {
				_ins_by_offset.Add (instruction.Offset, instruction);
				if (bb == null) {
					if (_bb_by_offset.TryGetValue (instruction.Offset, out bb)) {
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
					var target = (Instruction)instruction.Operand;
					if (!_bb_by_offset.ContainsKey (target.Offset)) {
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

				if (instruction.OpCode == OpCodes.Throw) {
					Context.LogMessage ($"    THROW");
					bb = null;
				}
			}

			DumpBlocks ();

			return true;
		}

		void DumpBlocks ()
		{
			foreach (var block in _bb_by_offset.Values) {
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

			RemoveBlock (bb);

			if (bb.Instructions.Count > 2) {
				var previous = NewBlock (bb.Instructions [0]);
				for (int i = 1; i < bb.Instructions.Count - 2; i++) {
					previous.AddInstruction (bb.Instructions [i]);
				}
			}

			bb = NewBlock (argument, BlockType.WeakInstanceOf);
		    	bb.AddInstruction (instruction);

			bb.ContainsConditionals = true;
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
			foreach (var block in _bb_by_offset.Values.ToArray ()) {
				switch (block.Type) {
				case BlockType.Normal:
					continue;
				case BlockType.WeakInstanceOf:
					RewriteWeakInstanceOf (block);
					break;
				default:
					throw new NotSupportedException ();
				}
			}
		}

		void RewriteWeakInstanceOf (BasicBlock block)
		{
			var target = ((GenericInstanceMethod)block.Instructions [1].Operand).GenericArguments [0].Resolve ();
			var value = Context.Context.Annotations.IsMarked (target);

			Context.LogMessage ($"REWRITE WEAK INSTANCE OF: {target} {value} {block}");

			var constant = Instruction.Create (OpCodes.Ldc_I4_0);
			constant.Offset = -1;

			var index = Body.Instructions.IndexOf (block.Instructions [0]);
			Body.Instructions.Remove (block.Instructions [0]);
			Body.Instructions.Remove (block.Instructions [1]);
			Body.Instructions.Insert (index, constant);

			RemoveBlock (block);
			var rewritten = NewBlock (constant);

			for (int i = 2; i < block.Instructions.Count; i++) {
				rewritten.AddInstruction (block.Instructions [i]);
			}
		}

		public enum BlockType
		{
			Normal,
			WeakInstanceOf
		}

		public class BasicBlock
		{
			public int Index {
				get;
			}

			public BlockType Type {
				get;
			}

			public int StartOffset {
				get;
			}

			public int EndOffset {
				get;
				private set;
			}

			public bool ContainsConditionals {
				get; set;
			}

			public IReadOnlyList<Instruction> Instructions => instructions;

			readonly List<Instruction> instructions;

			public BasicBlock (int index, BlockType type, Instruction instruction)
			{
				Index = index;
				Type = type;
				StartOffset = instruction.Offset;
				EndOffset = instruction.Offset;

				instructions = new List<Instruction> ();
				AddInstruction (instruction);

			}

			public void AddInstruction (Instruction instruction)
			{
				if (instruction.Offset < EndOffset)
					throw new ArgumentException ();

				instructions.Add (instruction);
				EndOffset = instruction.Offset;
			}

			public override string ToString ()
			{
				return $"[BB {Index}{((Type != BlockType.Normal ? $" ({Type})" : ""))}: 0x{StartOffset:x2} - 0x{EndOffset:x2}{((ContainsConditionals ? " CONDITIONAL" : ""))}]";
			}
		}
	}
}
