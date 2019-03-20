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

		Reachability Or (Reachability first, Reachability second)
		{
			if (first == Reachability.Exception || second == Reachability.Exception)
				throw new MartinTestException ();
			if (first == Reachability.Normal || second == Reachability.Normal)
				return Reachability.Normal;
			if (first == Reachability.Conditional || second == Reachability.Conditional)
				return Reachability.Conditional;
			return Reachability.Unreachable;
		}

		void MarkBlock (BasicBlock current, Reachability reachability, Instruction target)
		{
			var block = BlockList.GetBlock (target);
			if (block == current)
				return;
			block.FlowOrigins.Add (new Origin (current, reachability));
			if (block.Reachability == Reachability.Unknown)
				block.Reachability = reachability;
		}

		void MarkExceptionHandler (Instruction instruction)
		{
			var block = BlockList.GetBlock (instruction);
			block.Reachability = Reachability.Exception;
		}

		public void Analyze ()
		{
			BlockList.ClearFlowInformation ();

			foreach (var handler in Method.Body.ExceptionHandlers) {
				if (handler.HandlerStart != null)
					MarkExceptionHandler (handler.HandlerStart);
			}

			Scanner.DumpBlocks ();

			Scanner.LogDebug (1, $"ANALYZE: {Method.Name}");

			if (Scanner.DebugLevel > 0)
				Scanner.Context.Debug ();

			var reachability = Reachability.Normal;
			Origin current = null;

			foreach (var block in BlockList.Blocks) {
				if (current != null) {
					block.FlowOrigins.Add (current);
					current = null;
				}

				var oldr = reachability;
				var oldb = block.Reachability;

				if (block.Reachability != Reachability.Exception) {
					foreach (var origin in block.FlowOrigins) {
						if (origin.Block == block)
							continue;
						var effectiveOrigin = And (origin.Block.Reachability, origin.Reachability);
						reachability = Or (reachability, effectiveOrigin);
					}
					block.Reachability = reachability;
				}

				if (block.Reachability == Reachability.Unknown) {
					reachability = oldr;
					block.Reachability = oldb;
					foreach (var origin in block.FlowOrigins) {
						if (origin.Block == block)
							continue;
						var effectiveOrigin = And (origin.Block.Reachability, origin.Reachability);
						reachability = Or (reachability, effectiveOrigin);
					}
					block.Reachability = reachability;
					throw new MartinTestException ();
				}

				DumpBlock (block);

				switch (block.BranchType) {
				case BranchType.None:
					current = new Origin (block, Reachability.Normal);
					break;
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
					UpdateStatus (ref reachability, Reachability.Conditional);
					MarkBlock (block, Reachability.Conditional, (Instruction)block.LastInstruction.Operand);
					current = new Origin (block, Reachability.Conditional);
					break;
				case BranchType.Exit:
				case BranchType.Return:
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Jump:
					MarkBlock (block, Reachability.Normal, (Instruction)block.LastInstruction.Operand);
					UpdateStatus (ref reachability, Reachability.Unreachable);
					break;
				case BranchType.Switch:
					foreach (var label in (Instruction [])block.LastInstruction.Operand)
						MarkBlock (block, Reachability.Conditional, label);
					UpdateStatus (ref reachability, Reachability.Conditional);
					current = new Origin (block, Reachability.Conditional);
					break;
				}
			}

			DumpBlockList ();

			if (Scanner.DebugLevel > 0)
				Scanner.Context.Debug ();

			while (ResolveOrigins ()) {
				Scanner.LogDebug (1, $"ANALYZE -> AGAIN");
			}

			DumpBlockList ();

 			Scanner.DumpBlocks ();

			Scanner.LogDebug (1, $"FLOW ANALYSIS COMPLETE");

			if (Scanner.DebugLevel > 0)
				Scanner.Context.Debug ();

			return;
		}

		void DumpBlockList ()
		{
			Scanner.LogDebug (2, $"BLOCK LIST: {Method.Name}");
			for (int i = 0; i < BlockList.Count; i++) {
				Scanner.LogDebug (2, $"  #{i} ({BlockList[i].Reachability}): {BlockList [i]}");
				foreach (var origin in BlockList [i].FlowOrigins)
					Scanner.LogDebug (2, $"    {origin}");
			}
		}

		void DumpBlock (BasicBlock block)
		{
			Scanner.LogDebug (2, $"#{block.Index} ({block.Reachability}): {block}");
			foreach (var origin in block.FlowOrigins)
				Scanner.LogDebug (2, $"    ORIGIN: {origin}");
			foreach (var instruction in block.Instructions) {
				Scanner.LogDebug (2, $"    {CecilHelper.Format (instruction)}");
			}
		}

		bool ResolveOrigins ()
		{
			bool foundUnreachable = false;
			for (int i = 0; i < BlockList.Count; i++) {
				var block = BlockList [i];
				Scanner.LogDebug (3, $"    {i} {block} {block.Reachability}");
				bool foundOrigin = false;

				for (int j = 0; j < block.FlowOrigins.Count; j++) {
					var origin = block.FlowOrigins [j];
					var effectiveOrigin = And (origin.Block.Reachability, origin.Reachability);
					Scanner.LogDebug (3, $"        ORIGIN: {origin} - {effectiveOrigin}");
					if (origin.Block.Reachability == Reachability.Dead) {
						block.FlowOrigins.RemoveAt (j--);
						continue;
					}

					foundOrigin = true;
					switch (block.Reachability) {
					case Reachability.Dead:
						throw new MartinTestException ();
					case Reachability.Unreachable:
						if (block.Reachability != effectiveOrigin)
							Scanner.LogDebug (3, $"        -> EFFECTIVE ORIGIN {effectiveOrigin}");
						block.Reachability = effectiveOrigin;
						break;
					case Reachability.Conditional:
						if (effectiveOrigin == Reachability.Normal) {
							Scanner.LogDebug (3, $"        -> NORMAL");
							block.Reachability = Reachability.Normal;
						}
						break;
					}
				}

				if (block.Reachability == Reachability.Unreachable) {
					if (foundOrigin || block.FlowOrigins.Count == 0) {
						block.Reachability = Reachability.Dead;
						Scanner.LogDebug (3, $"        -> MARKING DEAD");
						MarkDead (block);
					} else
						foundUnreachable = true;
				}
			}

			return foundUnreachable;
		}

		void MarkDead (BasicBlock block)
		{
			block.Reachability = Reachability.Dead;
			if (block.Type == BasicBlockType.Normal)
				return;

			Scanner.LogDebug (2, $"    MARK DEAD: {block}");

			if (block.Type != BasicBlockType.Try)
				throw new MartinTestException ();

			var index = BlockList.IndexOf (block);
			int end_index = index + 1;

			foreach (var handler in block.ExceptionHandlers) {
				var handler_block = BlockList.GetBlock (handler.HandlerEnd);
				var handler_index = BlockList.IndexOf (handler_block);
				if (handler_index > end_index)
					end_index = handler_index;
			}

			Scanner.LogDebug (2, $"    MARK DEAD TRY: {index} {end_index}");

			for (int i = index; i < end_index; i++) {
				Scanner.LogDebug (2, $"    MARK DEAD TRY #1: {i} {BlockList[i]}");
				BlockList [i].Reachability = Reachability.Dead;
			}
		}

		internal class Origin
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
				return $"[{Reachability}: {Block}]";
			}
		}
	}
}
