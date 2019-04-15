//
// OptimizerReport.cs
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

namespace Mono.Linker.Optimizer.Configuration
{
	using BasicBlocks;

	public class OptimizerReport : Node
	{
		public ActionList ActionList { get; } = new ActionList ();

		public FailList FailList { get; } = new FailList ();

		public void MarkAsContainingConditionals (MethodDefinition method)
		{
			ActionList.GetMethod (method);
		}

		public void MarkAsConstantMethod (MethodDefinition method, ConstantValue value)
		{
			switch (value) {
			case ConstantValue.False:
				ActionList.GetMethod (method, true, MethodAction.ReturnFalse);
				break;
			case ConstantValue.True:
				ActionList.GetMethod (method, true, MethodAction.ReturnTrue);
				break;
			case ConstantValue.Null:
				ActionList.GetMethod (method, true, MethodAction.ReturnNull);
				break;
			case ConstantValue.Throw:
				ActionList.GetMethod (method, true, MethodAction.Throw);
				break;
			default:
				throw DebugHelpers.AssertFail ($"Invalid constant value: `{value}`.");
			}
		}

		public void RemovedDeadBlocks (MethodDefinition method)
		{
			ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedDeadBlocks;
		}

		public void RemovedDeadExceptionBlocks (MethodDefinition method)
		{
			ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedExceptionBlocks;
		}

		public void RemovedDeadJumps (MethodDefinition method)
		{
			ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedDeadJumps;
		}

		public void RemovedDeadConstantJumps (MethodDefinition method)
		{
			ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedConstantJumps;
		}

		public void RemovedDeadVariables (MethodDefinition method)
		{
			ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedDeadVariables;
		}

		public override void Visit (IVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void VisitChildren (IVisitor visitor)
		{
			ActionList.Visit (visitor);
			FailList.Visit (visitor);
		}
	}
}
