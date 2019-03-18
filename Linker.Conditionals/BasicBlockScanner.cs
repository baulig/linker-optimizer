﻿//
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

		public MethodDefinition Method {
			get;
		}

		public bool FoundConditionals {
			get; private set;
		}

		public BasicBlockList BlockList {
			get;
		}

		BasicBlockScanner (MartinContext context, MethodDefinition method)
		{
			Context = context;
			Method = method;

			BlockList = new BasicBlockList (context, method.Body);
		}

		public static bool ThrowOnError;

		public static BasicBlockScanner Scan (MartinContext context, MethodDefinition method)
		{
			var scanner = new BasicBlockScanner (context, method);
			if (!scanner.Scan ())
				return null;
			return scanner;
		}

		public IReadOnlyCollection<BasicBlock> BasicBlocks => BlockList.Blocks;

		bool Scan ()
		{
			Context.LogMessage ($"SCAN: {Method}");

			BasicBlock bb = null;

			var allTargets = CecilHelper.GetAllTargets (Method.Body);
			foreach (var instruction in allTargets) {
				BlockList.NewBlock (instruction);
			}

			for (int i = 0; i < Method.Body.Instructions.Count; i++) {
				var instruction = Method.Body.Instructions [i];

				if (bb == null) {
					if (BlockList.TryGetBlock (instruction, out bb)) {
						Context.LogMessage ($"  KNOWN BB: {bb}");
					} else {
						bb = BlockList.NewBlock (instruction);
						Context.LogMessage ($"  NEW BB: {bb}");
					}
				} else if (BlockList.TryGetBlock (instruction, out var newBB)) {
					if (bb.BranchType != BranchType.None)
						throw new MartinTestException ();
					Context.LogMessage ($"  KNOWN BB: {bb} -> {newBB}");
					bb = newBB;
				} else {
					bb.AddInstruction (instruction);
				}

				if (instruction.OpCode.OperandType == OperandType.InlineMethod) {
					Context.LogMessage ($"    CALL: {CecilHelper.Format (instruction)}");
					HandleCall (ref bb, ref i, instruction);
					continue;
				}

				var type = CecilHelper.GetBranchType (instruction);
				Context.LogMessage ($"    INS: {CecilHelper.Format (instruction)} {type}");
				switch (type) {
				case BranchType.None:
					break;
				case BranchType.Conditional:
				case BranchType.False:
				case BranchType.True:
				case BranchType.Jump:
				case BranchType.Switch:
				case BranchType.Exit:
				case BranchType.Return:
					bb = null;
					break;

				default:
					throw new NotSupportedException ();
				}
			}

			BlockList.ComputeOffsets ();

			BlockList.Dump ();

			return true;
		}

		void HandleCall (ref BasicBlock bb, ref int index, Instruction instruction)
		{
			var target = (MethodReference)instruction.Operand;
			Context.LogMessage ($"    CALL: {target}");

			if (instruction.Operand is GenericInstanceMethod genericInstance) {
				if (genericInstance.ElementMethod == Context.IsWeakInstanceOfMethod) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					IsWeakInstanceOfConditional.Create (BlockList, ref bb, ref index, conditionalType);
					FoundConditionals = true;
				} else if (genericInstance.ElementMethod == Context.AsWeakInstanceOfMethod) {
					var conditionalType = genericInstance.GenericArguments [0].Resolve ();
					if (conditionalType == null)
						throw new ResolutionException (genericInstance.GenericArguments [0]);
					AsWeakInstanceOfConditional.Create (BlockList, ref bb, ref index, conditionalType);
					FoundConditionals = true;
				}
			} else if (target == Context.IsFeatureSupportedMethod) {
				IsFeatureSupportedConditional.Create (BlockList, ref bb, ref index);
				FoundConditionals = true;
			} else if (target == Context.MarkFeatureMethod) {
				HandleMarkFeature (ref bb, ref index);
			}
		}

		void HandleMarkFeature (ref BasicBlock bb, ref int index)
		{
			if (bb.Instructions.Count == 1)
				throw new NotSupportedException ();
			if (index + 1 >= Method.Body.Instructions.Count)
				throw new NotSupportedException ();

			var feature = CecilHelper.GetFeatureArgument (bb.Instructions [bb.Count - 2]);
			Context.SetFeatureEnabled (feature, true);

			Context.LogMessage ($"MARK FEATURE: {feature}");

			/*
			 * `void MonoLinkerSupport.MarkFeature (MonoLinkerFeature feature)`
			 *
			 */

			if (bb.Instructions.Count > 2) {
				/*
				 * If we are in the middle of a basic block, then we can simply remove
				 * the two instructions.
				 */
				BlockList.RemoveInstructionAt (bb, bb.Count - 1);
				BlockList.RemoveInstructionAt (bb, bb.Count - 1);
				index -= 2;
				return;
			} else {
				/*
				 * We are at the beginning of a basic block.  Since somebody might jump
				 * to us, we replace the call with a `nop`.
				 */
				BlockList.RemoveInstructionAt (bb, bb.Count - 1);
				BlockList.ReplaceInstructionAt (ref bb, bb.Count - 1, Instruction.Create (OpCodes.Nop));
				index--;
				return;
			}
		}

		public void RewriteConditionals ()
		{
			Context.LogMessage ($"REWRITE CONDITIONALS");

			BlockList.Dump ();

			var foundConditionals = false;

			foreach (var block in BlockList.Blocks.ToArray ()) {
				if (block.LinkerConditional != null) {
					RewriteLinkerConditional (block);
					block.LinkerConditional = null;
					foundConditionals = true;
				}
			}

			if (!foundConditionals)
				return;

			BlockList.ComputeOffsets ();

			BlockList.Dump ();

			Context.LogMessage ($"DONE REWRITING CONDITIONALS");

			EliminateDeadBlocks ();
		}

		void RewriteLinkerConditional (BasicBlock block)
		{
			Context.LogMessage ($"REWRITE LINKER CONDITIONAL: {block.LinkerConditional}");

			BlockList.Dump ();

			block.LinkerConditional.RewriteConditional (ref block);

			BlockList.ComputeOffsets ();

			BlockList.Dump ();

			Context.LogMessage ($"DONE REWRITING LINKER CONDITIONAL");
		}

		void EliminateDeadBlocks ()
		{
			Context.LogMessage ($"ELIMINATING DEAD BLOCKS");

			var flow = new FlowAnalysis (BlockList);
			flow.Analyze ();
			flow.RemoveDeadBlocks ();
			flow.RemoveDeadJumps ();
			flow.RemoveUnusedVariables ();

			Context.LogMessage ($"ELIMINATING DEAD BLOCKS DONE");
		}
	}
}
