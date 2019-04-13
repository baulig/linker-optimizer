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
		public TypeDefinition Type {
			get;
		}

		public Action<TypeAction> Action {
			get;
		}

		public ActionVisitor (TypeDefinition type, Action<TypeAction> action)
		{
			Type = type;
			Action = action;
		}

		public void Visit (RootNode node)
		{
			node.VisitChildren (this);
		}

		public void Visit (ActionList node)
		{
			node.VisitChildren (this);
		}

		public void Visit (SizeReport node)
		{
			throw new NotImplementedException ();
		}

		public void Visit (Assembly node)
		{
			throw new NotImplementedException ();
		}

		public void Visit (Namespace node)
		{
			if (!node.Matches (Type))
				return;
			if (node.Action != TypeAction.None)
				Action (node.Action);
			node.VisitChildren (this);
		}

		public void Visit (Type node)
		{
			throw new NotImplementedException ();
		}

		public void Visit (Method node)
		{
			throw new NotImplementedException ();
		}

		public void Visit (FailList node)
		{
			throw new NotImplementedException ();
		}

		public void Visit (FailListEntry node)
		{
			throw new NotImplementedException ();
		}
	}
}
