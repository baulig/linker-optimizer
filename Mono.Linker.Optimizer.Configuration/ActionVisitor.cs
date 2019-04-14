//
// ActionVisitor.cs
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
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	public class ActionVisitor : IVisitor
	{
		public OptimizerOptions Options {
			get;
		}

		public TypeDefinition Type {
			get;
		}

		public MethodDefinition Method {
			get;
		}

		public Action<TypeAction> TypeCallback {
			get;
		}

		public Action<MethodAction> MethodCallback {
			get;
		}

		public ActionVisitor (OptimizerOptions options, TypeDefinition type, Action<TypeAction> callback)
		{
			Options = options;
			Type = type;
			TypeCallback = callback;
		}

		public ActionVisitor (OptimizerOptions options, MethodDefinition method, Action<MethodAction> callback)
		{
			Options = options;
			Type = method.DeclaringType;
			Method = method;
			MethodCallback = callback;
		}

		public void Visit (RootNode node)
		{
			node.VisitChildren (this);
		}

		public void Visit (ActionList node)
		{
			if (node.Conditional != null && Options.IsFeatureEnabled (node.Conditional) != node.Enabled)
				return;
			node.VisitChildren (this);
		}

		public void Visit (SizeReport node)
		{
		}

		public void Visit (Assembly node)
		{
		}

		public void Visit (Namespace node)
		{
			if (true || TypeCallback != null) {
				if (!node.Matches (Type))
					return;
				if (TypeCallback != null && node.Action != TypeAction.None)
					TypeCallback (node.Action);
			}
			node.VisitChildren (this);
		}

		public void Visit (Type node)
		{
			if (true || TypeCallback != null) {
				if (!node.Matches (Type))
					return;
				if (TypeCallback != null && node.Action != TypeAction.None)
					TypeCallback (node.Action);
			}
			node.VisitChildren (this);
		}

		public void Visit (Method node)
		{
			if (Method == null || !node.Matches (Method))
				return;
			if (MethodCallback != null && node.Action != MethodAction.None)
				MethodCallback (node.Action);
			node.VisitChildren (this);
		}

		public void Visit (FailList node)
		{
		}

		public void Visit (FailListEntry node)
		{
		}
	}
}
