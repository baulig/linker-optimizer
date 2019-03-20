//
// DeadCodeEliminator.cs
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
	public class DeadCodeEliminator
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public BasicBlockList BlockList => Scanner.BlockList;

		protected MethodDefinition Method => BlockList.Body.Method;

		public DeadCodeEliminator (BasicBlockScanner scanner)
		{
			Scanner = scanner;
		}

		public bool RemoveDeadBlocks ()
		{
			if (Scanner.DebugLevel > 0) {
				Scanner.DumpBlocks (1);
				Scanner.Context.Debug ();
			}

			var removedDeadBlocks = false;
			for (int i = 0; i < BlockList.Count; i++) {
				if (BlockList [i].Reachability == Reachability.Unknown)
					throw new MartinTestException ();
				if (BlockList [i].Reachability == Reachability.Unreachable)
					throw new MartinTestException ();
				if (BlockList [i].Reachability != Reachability.Dead)
					continue;

				Scanner.LogDebug (2, $"  DEAD BLOCK: {BlockList [i]}");
				Scanner.DumpBlock (2, BlockList [i]);

				if (Scanner.DebugLevel > 0)
					Scanner.Context.Debug ();

				removedDeadBlocks = true;
				DeleteBlock (ref i);
			}

			if (removedDeadBlocks) {
				BlockList.ComputeOffsets ();

				Scanner.DumpBlocks ();
			}

			return removedDeadBlocks;
		}

		bool DeleteBlock (ref int position)
		{
			var block = BlockList [position];

			if (block.Type == BasicBlockType.Normal) {
				BlockList.DeleteBlock (ref block);
				position--;
				return false;
			}

			if (block.Type != BasicBlockType.Try)
				throw new InvalidOperationException ();

			var index = BlockList.IndexOf (block);
			int end_index = index + 1;

			while (block.ExceptionHandlers.Count > 0) {
				var handler = block.ExceptionHandlers [0];
				var handler_index = BlockList.IndexOf (BlockList.GetBlock (handler.HandlerEnd));
				if (handler_index > end_index)
					end_index = handler_index;

				block.ExceptionHandlers.RemoveAt (0);
				BlockList.Body.ExceptionHandlers.Remove (handler);
			}

			Scanner.LogDebug (2, $"  DEAD EXCEPTION BLOCK: {block} {end_index}");

			while (end_index > index) {
				var current = BlockList [index];
				if (current.Reachability != Reachability.Dead)
					throw new MartinTestException ();

				Scanner.LogDebug (2, $"      DELETE: {current}");

				BlockList.DeleteBlock (ref current);
				end_index--;
			}

			position = -1;
			return true;
		}

		public bool RemoveDeadJumps ()
		{
			var removedDeadBlocks = false;

			for (int i = 0; i < BlockList.Count - 1; i++) {
				if (BlockList [i].BranchType != BranchType.Jump)
					continue;

				var lastInstruction = BlockList [i].LastInstruction;
				var nextInstruction = BlockList [i + 1].FirstInstruction;
				if (lastInstruction.OpCode.Code != Code.Br && lastInstruction.OpCode.Code != Code.Br_S)
					continue;
				if ((Instruction)lastInstruction.Operand != nextInstruction)
					continue;

				Scanner.LogDebug (2, $"ELIMINATE DEAD JUMP: {lastInstruction}");

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

				Scanner.DumpBlocks ();
			}

			return removedDeadBlocks;
		}

		public bool RemoveConstantJumps ()
		{
			var removedConstantJumps = false;

			if (Scanner.DebugLevel > 0)
				Scanner.Context.Debug ();

			for (int i = 0; i < BlockList.Count - 1; i++) {
				var block = BlockList [i];
				if (block.Count < 2)
					continue;

				if (block.BranchType == BranchType.False) {
					if (block.Instructions [block.Count - 2].OpCode.Code != Code.Ldc_I4_0)
						continue;
				} else if (block.BranchType == BranchType.True) {
					if (block.Instructions [block.Count - 2].OpCode.Code != Code.Ldc_I4_1)
						continue;
				} else {
					continue;
				}

				if (block.LastInstruction.OpCode.Code != Code.Brfalse && block.LastInstruction.OpCode.Code != Code.Brfalse_S)
					throw new MartinTestException ();

				var target = (Instruction)block.LastInstruction.Operand;

				Scanner.LogDebug (2, $"ELIMINATE CONSTANT JUMP: {block.LastInstruction} {target}");

				BlockList.RemoveInstructionAt (block, block.Count - 1);
				BlockList.ReplaceInstructionAt (ref block, block.Count - 1, Instruction.Create (OpCodes.Br, target));

				removedConstantJumps = true;

			}

			if (removedConstantJumps) {
				BlockList.ComputeOffsets ();

				Scanner.DumpBlocks ();
			}

			return removedConstantJumps;
		}

		public bool RemoveUnusedVariables ()
		{
			Scanner.LogDebug (1, $"REMOVE VARIABLES: {Method.Name}");

			var removed = false;
			var variables = new Dictionary<VariableDefinition, VariableEntry> ();

			for (int i = 0; i < Method.Body.Variables.Count; i++) {
				var variable = new VariableEntry (Method.Body.Variables [i], i);
				variables.Add (variable.Variable, variable);
			}

			foreach (var block in BlockList.Blocks) {
				Scanner.LogDebug (2, $"REMOVE VARIABLES #1: {block}");

				for (int i = 0; i < block.Instructions.Count; i++) {
					var instruction = block.Instructions [i];
					Scanner.LogDebug (2, $"    {CecilHelper.Format (instruction)}");

					var variable = CecilHelper.GetVariable (Method.Body, instruction);
					if (variable == null)
						continue;

					var entry = variables [variable];
					if (entry == null)
						throw new MartinTestException ();

					entry.Used = true;
					if (entry.Modified)
						continue;

					switch (instruction.OpCode.Code) {
					case Code.Ldloc_0:
					case Code.Ldloc_1:
					case Code.Ldloc_2:
					case Code.Ldloc_3:
					case Code.Ldloc:
					case Code.Ldloc_S:
						continue;

					case Code.Ldloca:
					case Code.Ldloca_S:
						entry.SetModified ();
						continue;

					case Code.Stloc:
					case Code.Stloc_0:
					case Code.Stloc_1:
					case Code.Stloc_2:
					case Code.Stloc_3:
					case Code.Stloc_S:
						break;

					default:
						throw new MartinTestException ();
					}

					if (i == 0 || entry.IsConstant) {
						entry.SetModified ();
						continue;
					}

					var load = block.Instructions [i - 1];
					switch (block.Instructions [i - 1].OpCode.Code) {
					case Code.Ldc_I4_0:
						entry.SetConstant (block, load, 0);
						break;
					case Code.Ldc_I4_1:
						entry.SetConstant (block, load, 1);
						break;
					default:
						entry.SetModified ();
						break;
					}
				}
			}

			Scanner.LogDebug (1, $"REMOVE VARIABLES #1");

			for (int i = Method.Body.Variables.Count - 1; i >= 0; i--) {
				var variable = variables [Method.Body.Variables [i]];
				Scanner.LogDebug (2, $"    VARIABLE #{i}: {variable}");
				if (!variable.Used) {
					Scanner.LogDebug (2, $"    --> REMOVE");
					RemoveVariable (variable);
					Method.Body.Variables.RemoveAt (i);
					removed = true;
					continue;
				}

				if (variable.IsConstant) {
					Scanner.LogDebug (2, $"    --> CONSTANT ({variable.Value}): {variable.Instruction}");
					Scanner.DumpBlock (2, variable.Block);
					var position = variable.Block.IndexOf (variable.Instruction);
					var block = variable.Block;
					BlockList.RemoveInstructionAt (ref block, position + 1);
					BlockList.RemoveInstructionAt (ref block, position);
					RemoveVariable (variable);
					Method.Body.Variables.RemoveAt (i);
					removed = true;
					continue;
				}
			}

			Scanner.LogDebug (1, $"REMOVE VARIABLES #2: {removed}");

			if (removed) {
				BlockList.ComputeOffsets ();

				Scanner.DumpBlocks ();
			}

			return removed;
		}

		void RemoveVariable (VariableEntry variable)
		{
			Scanner.DumpBlocks ();

			for (int i = 0; i < BlockList.Count; i++) {
				var block = BlockList [i];
				for (int j = 0; j < block.Instructions.Count; j++) {
					var instruction = block.Instructions [j];
					Scanner.LogDebug (2, $"    {CecilHelper.Format (instruction)}");

					switch (instruction.OpCode.Code) {
					case Code.Ldloc_0:
						if (variable.Index == 0 && variable.IsConstant)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_0:
						break;
					case Code.Ldloc_1:
						if (variable.Index < 1)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_0));
						else if (variable.Index == 1)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_1:
						if (variable.Index < 1)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_0));
						break;
					case Code.Ldloc_2:
						if (variable.Index < 2)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_1));
						else if (variable.Index == 2)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_2:
						if (variable.Index < 2)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_1));
						break;
					case Code.Ldloc_3:
						if (variable.Index < 3)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_2));
						else if (variable.Index == 3)
							BlockList.ReplaceInstructionAt (ref block, j, CecilHelper.CreateConstantLoad (variable.Value));
						break;
					case Code.Stloc_3:
						if (variable.Index < 3)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_2));
						break;
					}
				}
			}
		}

		class VariableEntry
		{
			public VariableDefinition Variable {
				get;
			}

			public int Index {
				get;
			}

			public bool Used {
				get; set;
			}

			public bool Modified {
				get;
				private set;
			}

			public bool IsConstant => Block != null;

			public BasicBlock Block {
				get;
				private set;
			}

			public Instruction Instruction {
				get;
				private set;
			}

			public int Value {
				get;
				private set;
			}

			public VariableEntry (VariableDefinition variable, int index)
			{
				Variable = variable;
				Index = index;
			}

			public void SetModified ()
			{
				Modified = true;
				Block = null;
				Instruction = null;
				Value = 0;
			}

			public void SetConstant (BasicBlock block, Instruction instruction, int value)
			{
				if (Modified)
					throw new InvalidOperationException ();
				Block = block;
				Instruction = instruction;
				Value = value;
			}

			public override string ToString ()
			{
				return $"[{Variable}: used={Used}, modified={Modified}, constant={IsConstant}]";
			}
		}
	}
}
