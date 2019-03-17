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
		Dictionary<BasicBlock, BlockEntry> _block_list;

		void MarkBlock (Reachability reachability, Instruction instruction)
		{
			var block = BlockList.GetBlock (instruction);
			MarkBlock (reachability, block);
		}

		void MarkBlock (Reachability reachability, BasicBlock block)
		{
			if (!_reachability_status.TryGetValue (block, out var status)) {
				_reachability_status.Add (block, reachability);
				return;
			}
		}

		void CheckCurrentBlock (ref Reachability current, BasicBlock block)
		{
			if (!_reachability_status.TryGetValue (block, out var status)) {
				_reachability_status.Add (block, current);
				return;
			}

			// UpdateStatus (ref current, status);
			switch (current) {
			case Reachability.Unreachable:
			case Reachability.Dead:
				current = status;
				break;
			case Reachability.Conditional:
				if (status == Reachability.Normal)
					current = status;
				break;
			}
		}

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

		void MarkBlock (BlockEntry entry, Reachability reachability, Instruction target)
		{
			var block = BlockList.GetBlock (target);
			if (_block_list.TryGetValue (block, out var targetEntry))
				targetEntry.AddOrigin (entry.Block, reachability);
			else {
				targetEntry = new BlockEntry (block, reachability);
				_block_list.Add (block, targetEntry);
			}
		}

		public void Analyze ()
		{
			if (Method.Body.HasExceptionHandlers)
				return;

			_reachability_status = new Dictionary<BasicBlock, Reachability> ();
			_block_list = new Dictionary<BasicBlock, BlockEntry> ();
			var reachability = Reachability.Normal;

			BlockList.Dump ();

			Context.LogMessage ($"ANALYZE: {Method.Name}");

			foreach (var block in BlockList.Blocks) {
				Context.LogMessage ($"ANALYZE #1: {block} {reachability}");

				if (!_block_list.TryGetValue (block, out var entry)) {
					entry = new BlockEntry (block, reachability);
					_block_list.Add (block, entry);
				}

				Context.LogMessage ($"ANALYZE #2: {entry} {reachability}");
				BlockList.Dump (block);

				switch (block.BranchType) {
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
					MarkBlock (entry, Reachability.Conditional, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Conditional);
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
					break;
				}
			}

			Context.LogMessage ($"ANALYZE #3: {Method.Name}");

			var entries = _block_list.Values.ToArray ();
			for (int i = 0; i < entries.Length; i++) {
				Context.LogMessage ($"    {i} {entries[i]}");

				foreach (var origin in entries[i].Origins) {
					var originEntry = _block_list [origin.Block];
					Context.LogMessage ($"        ORIGIN: {origin} - {originEntry}");
					if (originEntry.Reachability == Reachability.Dead)
						continue;
					if (entries [i].Reachability == Reachability.Unreachable)
						entries [i].Reachability = originEntry.Reachability;
				}

				if (entries [i].Reachability == Reachability.Unreachable)
					entries [i].Reachability = Reachability.Dead;
			}

			Context.LogMessage ($"ANALYZE #4");

			entries = _block_list.Values.ToArray ();
			for (int i = 0; i < entries.Length; i++) {
				Context.LogMessage ($"    {i} {entries [i]}");

			}

			Context.LogMessage ($"ANALYZE #5");

			BlockList.Dump ();

			return;

			foreach (var block in BlockList.Blocks) {
				Context.LogMessage ($"ANALYZE #2: {block} {_reachability_status.ContainsKey (block)} {reachability}");
				BlockList.Dump (block);

				if (_reachability_status.TryGetValue (block, out var status)) {
					Context.LogMessage ($"ANALYZE #3: {status}");
				}

//				CheckCurrentBlock (ref reachability, block);

				switch (block.BranchType) {
				case BranchType.None:
					break;
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
					// MarkBlock (Reachability.Conditional, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Conditional);
					break;
				case BranchType.Exit:
				case BranchType.Return:
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Jump:
					// MarkBlock (Reachability.Normal, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Switch:
					// foreach (var label in (Instruction [])block.LastInstruction.Operand)
					//	MarkBlock (Reachability.Conditional, label);
					UpdateStatus (ref reachability, Reachability.Conditional);
					break;
				}
			}

			Context.LogMessage ($"FLOW ANALYSIS COMPLETE");

			return;

			foreach (var block in BlockList.Blocks) {
				MarkBlock (Reachability.Normal, block);

				switch (block.BranchType) {
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
					MarkBlock (Reachability.Conditional, (Instruction)block.LastInstruction.Operand);
					break;
				case BranchType.Jump:
					MarkBlock (Reachability.Normal, (Instruction)block.LastInstruction.Operand);
					break;
				case BranchType.Switch:
					foreach (var label in (Instruction [])block.LastInstruction.Operand)
						MarkBlock (Reachability.Conditional, label);
					break;
				}
			}

			foreach (var block in BlockList.Blocks) {
				MarkBlock (Reachability.Normal, block);

				switch (block.BranchType) {
				case BranchType.None:
					break;
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
					MarkBlock (Reachability.Conditional, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Conditional);
					break;
				case BranchType.Exit:
				case BranchType.Return:
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Jump:
					MarkBlock (Reachability.Normal, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Switch:
					foreach (var label in (Instruction [])block.LastInstruction.Operand)
						MarkBlock (Reachability.Conditional, label);
					UpdateStatus (ref reachability, Reachability.Conditional);
					break;
				}
			}

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
