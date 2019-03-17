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
		public BasicBlockList BlockList
		{
			get;
		}

		public FlowAnalysis (BasicBlockList blocks)
		{
			BlockList = blocks;
		}

		protected MartinContext Context => BlockList.Context;

		protected MethodDefinition Method => BlockList.Body.Method;

		protected AssemblyDefinition Assembly => Method.DeclaringType.Module.Assembly;

		Dictionary<BasicBlock, Reachability> _reachability_status;
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

		public void Analyze ()
		{
			if (Method.Body.HasExceptionHandlers)
				return;

			_entry_by_block = new Dictionary<BasicBlock, BlockEntry> ();
			_block_list = new List<BlockEntry> ();

			var reachability = Reachability.Normal;
			Origin current = null;

			BlockList.Dump ();

			Context.LogMessage ($"ANALYZE: {Method.Name}");

			foreach (var block in BlockList.Blocks) {
				Context.LogMessage ($"ANALYZE #1: {block} {reachability}");

				if (!_entry_by_block.TryGetValue (block, out var entry)) {
					entry = new BlockEntry (block, reachability);
					_entry_by_block.Add (block, entry);
					_block_list.Add (entry);
				}

				if (current != null) {
					entry.Origins.Add (current);
					current = null;
				}

				Context.LogMessage ($"ANALYZE #2: {entry} {reachability}");
				BlockList.Dump (block);

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

			Context.LogMessage ($"ANALYZE #3: {Method.Name}");

			_block_list.Sort ((first, second) => first.Block.Index.CompareTo (second.Block.Index));

			for (int i = 0; i < _block_list.Count; i++) {
				var entry = _block_list [i];
				Context.LogMessage ($"    {i} {entry}");
				bool foundOrigin = false;

				foreach (var origin in entry.Origins) {
					var originEntry = _entry_by_block [origin.Block];
					var effectiveOrigin = And (originEntry.Reachability, origin.Reachability);
					Context.LogMessage ($"        ORIGIN: {origin} - {originEntry} - {effectiveOrigin}");
					if (originEntry.Reachability == Reachability.Dead)
						continue;
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

				if (!foundOrigin && entry.Reachability == Reachability.Unreachable)
					entry.Reachability = Reachability.Dead;
			}

			Context.LogMessage ($"ANALYZE #4");

			for (int i = 0; i < _block_list.Count; i++) {
				Context.LogMessage ($"    {i} {_block_list [i]}");

			}

			BlockList.Dump ();

			Context.LogMessage ($"FLOW ANALYSIS COMPLETE");

			return;
		}

		public bool RemoveDeadBlocks ()
		{
			if (Method.Body.HasExceptionHandlers)
				return false;

			var removedDeadBlocks = false;
			for (int i = 0; i < _block_list.Count; i++) {
				if (_block_list [i].Reachability != Reachability.Dead)
					continue;

				Context.LogMessage ($"  DEAD BLOCK: {_block_list [i]}");

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

				Context.LogMessage ($"ELIMINATE DEAD JUMP: {lastInstruction}");

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

		enum Reachability
		{
			Normal,
			Unreachable,
			Conditional,
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
