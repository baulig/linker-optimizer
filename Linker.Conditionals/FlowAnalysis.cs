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

		public void Analyze ()
		{
			if (Method.Body.HasExceptionHandlers)
				return;

			_reachability_status = new Dictionary<BasicBlock, Reachability> ();
			var reachability = Reachability.Normal;

			BlockList.Dump ();

			Context.LogMessage ($"ANALYZE: {Method.Name}");

			foreach (var block in BlockList.Blocks) {
				CheckCurrentBlock (ref reachability, block);

				Context.LogMessage ($"ANALYZE: {block} {_reachability_status.ContainsKey (block)} {reachability}");
				BlockList.Dump (block);

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
	}
}
