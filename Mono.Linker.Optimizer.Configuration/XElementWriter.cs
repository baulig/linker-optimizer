//
// XElementWriter.cs
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
using System.Xml.Linq;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer.Configuration
{
	public abstract class XElementWriter : IVisitor
	{
		public XDocument Root {
			get;
		}

		Stack<CurrentNode> Stack { get; } = new Stack<CurrentNode> ();

		class CurrentNode
		{
			public XNode Node {
				get;
			}

			public CurrentNode (XNode node)
			{
				Node = node;
			}
		}

		protected XElementWriter ()
		{
			Root = new XDocument ();
			Stack.Push (new CurrentNode (Root));
		}

		void Visit<T> (T node, string elementName, Func<T, XElement, bool> func)
			where T : Node
		{
			var element = new XElement (elementName);
			var current = new CurrentNode (element);
			Stack.Push (current);

			if (func (node, element))
				node.VisitChildren (this);

			Stack.Pop ();

			var parent = Stack.Peek ();
			if (parent.Node is XDocument document)
				document.Add (current.Node);
			else
				((XElement)parent.Node).Add (current.Node);
		}

		void IVisitor.Visit (RootNode node) => Visit (node, "root", Visit);

		void IVisitor.Visit (ActionList node) => Visit (node, "action-list", Visit);

		void IVisitor.Visit (SizeReport node) => Visit (node, "size-report", Visit);

		void IVisitor.Visit (OptimizerReport node) => Visit (node, "optimizer-report", Visit);

		void IVisitor.Visit (Assembly node) => Visit (node, "assembly", Visit);

		void IVisitor.Visit (Type node) => Visit (node, node.Match == MatchKind.Namespace ? "namespace" : "type", Visit);

		void IVisitor.Visit (Method node) => Visit (node, "method", Visit);

		void IVisitor.Visit (FailList node) => Visit (node, "fail-list", Visit);

		void IVisitor.Visit (FailListEntry node) => Visit (node, node.IsFatal ? "warn" : "fail", Visit);

		void IVisitor.Visit (FailListNode node) => Visit (node, node.ElementName, Visit);

		protected abstract bool Visit (RootNode node, XElement element);

		protected abstract bool Visit (SizeReport node, XElement element);

		protected abstract bool Visit (OptimizerReport node, XElement element);

		protected abstract bool Visit (ActionList node, XElement element);

		protected abstract bool Visit (Assembly node, XElement element);

		protected abstract bool Visit (Type node, XElement element);

		protected abstract bool Visit (Method node, XElement element);

		protected abstract bool Visit (FailList node, XElement element);

		protected abstract bool Visit (FailListEntry node, XElement element);

		protected abstract bool Visit (FailListNode node, XElement element);
	}
}
