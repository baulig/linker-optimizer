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

			var reachability = Reachability.Normal;
			Origin current = null;

			Scanner.DumpBlocks ();

			Scanner.LogDebug (1, $"ANALYZE: {Method.Name}");

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

			Scanner.LogDebug (1, $"ANALYZE #3: {Method.Name}");

			_block_list.Sort ((first, second) => first.Block.Index.CompareTo (second.Block.Index));

			while (ResolveOrigins ()) {
				Scanner.LogDebug (1, $"ANALYZE #3 -> AGAIN");
			}

			Scanner.LogDebug (1, $"ANALYZE #4");

			for (int i = 0; i < _block_list.Count; i++) {
				Scanner.LogDebug (1, $"    {i} {_block_list [i]}");
			}

			Scanner.DumpBlocks ();

			Scanner.LogDebug (1, $"FLOW ANALYSIS COMPLETE");

			return;
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
						entry.Reachability = effectiveOrigin;
						break;
					case Reachability.Conditional:
						if (effectiveOrigin == Reachability.Normal)
							entry.Reachability = Reachability.Normal;
						break;
					}
				}

				if (entry.Reachability == Reachability.Unreachable) {
					if (foundOrigin || entry.Origins.Count == 0)
						entry.Reachability = Reachability.Dead;
					else
						foundUnreachable = true;
				}
			}

			return foundUnreachable;
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

				var block = _block_list [i].Block;
				BlockList.DeleteBlock (ref block);

				removedDeadBlocks = true;
			}

			if (removedDeadBlocks) {
				BlockList.ComputeOffsets ();

				BlockList.Dump ();
			}

			return removedDeadBlocks;
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

				BlockList.Dump ();
			}

			return removedDeadBlocks;
		}

		public bool RemoveUnusedVariables ()
		{
			var removed = false;
			var marked = new HashSet<VariableDefinition> ();
			var variables = Method.Body.Variables;

			Scanner.LogDebug (1, $"REMOVE VARIABLES: {Method.Name}");

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

			for (int i = 0; i < Method.Body.Variables.Count; i++) {
				if (marked.Contains (Method.Body.Variables [i]))
					continue;
				Scanner.LogDebug (2, $"    REMOVE: {Method.Body.Variables[i]}");
				Method.Body.Variables.RemoveAt (i);
				removed = true;
				i--;
			}

			Scanner.LogDebug (1, $"REMOVE VARIABLES #2: {removed}");

			return removed;
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
