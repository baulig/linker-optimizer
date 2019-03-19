//
// FlowAnalysis.cs
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
	public class FlowAnalysis
	{
		public BasicBlockScanner Scanner {
			get;
		}

		public BasicBlockList BlockList => Scanner.BlockList;

		public FlowAnalysis (BasicBlockScanner scanner)
		{
			Scanner = scanner;
		}

		protected MethodDefinition Method => BlockList.Body.Method;

		protected AssemblyDefinition Assembly => Method.DeclaringType.Module.Assembly;

		Dictionary<BasicBlock, BlockEntry> _entry_by_block;
		List<BlockEntry> _block_list;

		void UpdateStatus (ref Reachability current, Reachability reachability)
		{
			switch (reachability) {
			case Reachability.Unreachable:
			case Reachability.Dead:
				current = reachability;
				break;
			case Reachability.Conditional:
				if (current == Reachability.Normal)
					current = reachability;
				break;
			case Reachability.Exception:
				current = Reachability.Conditional;
				break;
			}
		}

		Reachability And (Reachability first, Reachability second)
		{
			if (first == Reachability.Dead || second == Reachability.Dead)
				return Reachability.Dead;
			if (first == Reachability.Unreachable || second == Reachability.Unreachable)
				return Reachability.Unreachable;
			if (first == Reachability.Conditional || second == Reachability.Conditional)
				return Reachability.Conditional;
			if (first == Reachability.Exception || second == Reachability.Exception)
				throw new MartinTestException ();
			return Reachability.Normal;
		}

		void MarkBlock (BlockEntry entry, Reachability reachability, Instruction target)
		{
			var block = BlockList.GetBlock (target);
			if (block == entry.Block)
				return;
			if (_entry_by_block.TryGetValue (block, out var targetEntry))
				targetEntry.AddOrigin (entry.Block, reachability);
			else {
				targetEntry = new BlockEntry (block, reachability);
				targetEntry.AddOrigin (entry.Block, reachability);
				_entry_by_block.Add (block, targetEntry);
				_block_list.Add (targetEntry);
			}
		}

		void MarkExceptionHandler (Instruction instruction)
		{
			var block = BlockList.GetBlock (instruction);
			var entry = new BlockEntry (block, Reachability.Exception);
			_entry_by_block.Add (block, entry);
			_block_list.Add (entry);
		}

		public void Analyze ()
		{
			_entry_by_block = new Dictionary<BasicBlock, BlockEntry> ();
			_block_list = new List<BlockEntry> ();

			foreach (var handler in Method.Body.ExceptionHandlers) {
				if (handler.HandlerStart != null)
					MarkExceptionHandler (handler.HandlerStart);
			}

			Scanner.DumpBlocks ();

			Scanner.LogDebug (1, $"ANALYZE: {Method.Name}");

			var reachability = Reachability.Normal;
			Origin current = null;

			foreach (var block in BlockList.Blocks) {
				Scanner.LogDebug (2, $"ANALYZE #1: {block} {reachability}");

				if (!_entry_by_block.TryGetValue (block, out var entry)) {
					entry = new BlockEntry (block, reachability);
					_entry_by_block.Add (block, entry);
					_block_list.Add (entry);
				}

				if (current != null) {
					entry.Origins.Add (current);
					current = null;
				}

				Scanner.LogDebug (2, $"ANALYZE #2: {entry} {reachability}");
				Scanner.DumpBlock (2, block);

				switch (block.BranchType) {
				case BranchType.None:
					current = new Origin (block, Reachability.Normal);
					break;
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
					MarkBlock (entry, Reachability.Conditional, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Conditional);
					current = new Origin (block, Reachability.Conditional);
					break;
				case BranchType.Exit:
				case BranchType.Return:
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Jump:
					MarkBlock (entry, Reachability.Normal, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Switch:
					foreach (var label in (Instruction [])block.LastInstruction.Operand)
						MarkBlock (entry, Reachability.Conditional, label);
					UpdateStatus (ref reachability, Reachability.Conditional);
					current = new Origin (block, Reachability.Conditional);
					break;
				}
			}

			_block_list.Sort ((first, second) => first.Block.Index.CompareTo (second.Block.Index));

			DumpBlockList ();

			Scanner.LogDebug (1, "ANALYZE #3");

			while (ResolveOrigins ()) {
				Scanner.LogDebug (1, $"ANALYZE #3 -> AGAIN");
			}

			Scanner.LogDebug (1, $"ANALYZE #4");

			DumpBlockList ();

			Scanner.DumpBlocks ();

			Scanner.LogDebug (1, $"FLOW ANALYSIS COMPLETE");

			return;
		}

		void DumpBlockList ()
		{
			Scanner.LogDebug (2, $"BLOCK LIST: {Method.Name}");
			for (int i = 0; i < _block_list.Count; i++) {
				Scanner.LogDebug (2, $"  #{i}: {_block_list [i]}");
				foreach (var origin in _block_list [i].Origins)
					Scanner.LogDebug (2, $"        {origin}");
			}
		}

		bool ResolveOrigins ()
		{
			bool foundUnreachable = false;
			for (int i = 0; i < _block_list.Count; i++) {
				var entry = _block_list [i];
				Scanner.LogDebug (3, $"    {i} {entry}");
				bool foundOrigin = false;

				for (int j = 0; j < entry.Origins.Count; j++) {
					var origin = entry.Origins [j];
					var originEntry = _entry_by_block [origin.Block];
					var effectiveOrigin = And (originEntry.Reachability, origin.Reachability);
					Scanner.LogDebug (3, $"        ORIGIN: {origin} - {originEntry} - {effectiveOrigin}");
					if (originEntry.Reachability == Reachability.Dead) {
						entry.Origins.RemoveAt (j--);
						continue;
					}

					foundOrigin = true;
					switch (entry.Reachability) {
					case Reachability.Dead:
						throw new MartinTestException ();
					case Reachability.Unreachable:
						if (entry.Reachability != effectiveOrigin)
							Scanner.LogDebug (3, $"        -> EFFECTIVE ORIGIN {effectiveOrigin}");
						entry.Reachability = effectiveOrigin;
						break;
					case Reachability.Conditional:
						if (effectiveOrigin == Reachability.Normal) {
							Scanner.LogDebug (3, $"        -> NORMAL");
							entry.Reachability = Reachability.Normal;
						}
						break;
					}
				}

				if (entry.Reachability == Reachability.Unreachable) {
					if (foundOrigin || entry.Origins.Count == 0) {
						entry.Reachability = Reachability.Dead;
						Scanner.LogDebug (3, $"        -> MARKING DEAD");
						MarkDead (entry);
					} else
						foundUnreachable = true;
				}
			}

			return foundUnreachable;
		}

		void MarkDead (BlockEntry entry)
		{
			entry.Reachability = Reachability.Dead;
			if (entry.Block.Type == BasicBlockType.Normal)
				return;

			Scanner.LogDebug (2, $"    MARK DEAD: {entry.Block}");

			if (entry.Block.Type != BasicBlockType.Try)
				throw new MartinTestException ();

			var index = BlockList.IndexOf (entry.Block);
			int end_index = index + 1;

			foreach (var handler in entry.Block.ExceptionHandlers) {
				var handler_block = BlockList.GetBlock (handler.HandlerEnd);
				var handler_index = BlockList.IndexOf (handler_block);
				if (handler_index > end_index)
					end_index = handler_index;
			}

			Scanner.LogDebug (2, $"    MARK DEAD TRY: {index} {end_index}");

			for (int i = index; i < end_index; i++) {
				var delete = _entry_by_block [BlockList [i]];
				Scanner.LogDebug (2, $"    MARK DEAD TRY #1: {i} {BlockList[i]}: {delete}");
				delete.Reachability = Reachability.Dead;
			}
		}

		public bool RemoveDeadBlocks ()
		{
			var removedDeadBlocks = false;
			for (int i = 0; i < _block_list.Count; i++) {
				if (_block_list [i].Reachability == Reachability.Unreachable)
					throw new MartinTestException ();
				if (_block_list [i].Reachability != Reachability.Dead)
					continue;

				Scanner.LogDebug (2, $"  DEAD BLOCK: {_block_list [i]}");

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
			var block = _block_list [position].Block;

			if (block.Type == BasicBlockType.Normal) {
				_block_list.RemoveAt (position--);
				_entry_by_block.Remove (block);
				BlockList.DeleteBlock (ref block);
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
				var entry = _entry_by_block [BlockList [index]];
				if (entry.Reachability != Reachability.Dead)
					throw new MartinTestException ();

				Scanner.LogDebug (2, $"      DELETE: {current} {entry}");

				_entry_by_block.Remove (current);
				_block_list.Remove (entry);

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

				Scanner.DumpBlocks ();
			}

			return removedDeadBlocks;
		}

		public bool RemoveUnusedVariables ()
		{
			Scanner.LogDebug (1, $"REMOVE VARIABLES: {Method.Name}");

			var removed = false;
			var marked = new HashSet<VariableDefinition> ();
			var variables = Method.Body.Variables;

			foreach (var instruction in Method.Body.Instructions) {
				Scanner.LogDebug (2, $"    INSTRUCTION: {instruction.OpCode.OperandType} {CecilHelper.Format (instruction)}");

				switch (instruction.OpCode.Code) {
				case Code.Ldloc_0:
				case Code.Stloc_0:
					marked.Add (variables [0]);
					break;
				case Code.Ldloc_1:
				case Code.Stloc_1:
					marked.Add (variables [1]);
					break;
				case Code.Ldloc_2:
				case Code.Stloc_2:
					marked.Add (variables [2]);
					break;
				case Code.Ldloc_3:
				case Code.Stloc_3:
					marked.Add (variables [3]);
					break;
				case Code.Ldloc:
				case Code.Ldloc_S:
				case Code.Ldloca:
				case Code.Ldloca_S:
				case Code.Stloc_S:
				case Code.Stloc:
					var variable = ((VariableReference)instruction.Operand).Resolve ();
					Scanner.LogDebug (2, $"    VARIABLE: {variable} {instruction}");
					if (variable == null)
						continue;
					marked.Add (variable);
					break;
				}
			}

			Scanner.LogDebug (1, $"REMOVE VARIABLES #1");

			for (int i = Method.Body.Variables.Count - 1; i >= 0; i--) {
				if (marked.Contains (Method.Body.Variables [i]))
					continue;
				Scanner.LogDebug (2, $"    REMOVE: {Method.Body.Variables[i]}");
				RemoveVariable (i);
				Method.Body.Variables.RemoveAt (i);
				removed = true;
			}

			Scanner.LogDebug (1, $"REMOVE VARIABLES #2: {removed}");

			if (removed) {
				BlockList.ComputeOffsets ();

				Scanner.DumpBlocks ();
			}

			return removed;
		}

		void RemoveVariable (int index)
		{
			Scanner.DumpBlocks ();

			for (int i = 0; i < BlockList.Count; i++) {
				var block = BlockList [i];
				for (int j = 0; j < block.Instructions.Count; j++) {
					var instruction = block.Instructions [j];
					Scanner.LogDebug (2, $"    INSTRUCTION: {instruction.OpCode.OperandType} {CecilHelper.Format (instruction)}");

					switch (instruction.OpCode.Code) {
					case Code.Ldloc_0:
					case Code.Stloc_0:
						break;
					case Code.Ldloc_1:
						if (index < 1)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_0));
						break;
					case Code.Stloc_1:
						if (index < 1)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_0));
						break;
					case Code.Ldloc_2:
						if (index < 2)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_1));
						break;
					case Code.Stloc_2:
						if (index < 2)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_1));
						break;
					case Code.Ldloc_3:
						if (index < 3)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Ldloc_2));
						break;
					case Code.Stloc_3:
						if (index < 3)
							BlockList.ReplaceInstructionAt (ref block, j, Instruction.Create (OpCodes.Stloc_2));
						break;
					}
				}
			}
		}

		enum Reachability
		{
			Normal,
			Unreachable,
			Conditional,
			Exception,
			Dead
		}

		class Origin
		{
			public BasicBlock Block {
				get;
			}

			public Reachability Reachability {
				get;
			}

			public Origin (BasicBlock block, Reachability reachability)
			{
				Block = block;
				Reachability = reachability;
			}

			public override string ToString ()
			{
				return $"[{Block}: {Reachability}]";
			}
		}

		class BlockEntry
		{
			public BasicBlock Block {
				get;
			}

			public Reachability Reachability {
				get; set;
			}

			public List<Origin> Origins {
				get;
			}

			public void AddOrigin (BasicBlock block, Reachability reachability)
			{
				Origins.Add (new Origin (block, reachability));
			}

			public BlockEntry (BasicBlock block, Reachability reachability)
			{
				Block = block;
				Reachability = reachability;
				Origins = new List<Origin> ();
			}

			public override string ToString ()
			{
				return $"[{Reachability}: {Block}]";
			}
		}
	}
}
