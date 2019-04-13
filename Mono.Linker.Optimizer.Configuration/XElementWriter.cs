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
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	public class XElementWriter : IVisitor
	{
		public XElement Root {
			get;
		}

		public bool IsEmpty => !Root.IsEmpty || Root.HasElements || Root.HasAttributes;

		Stack<CurrentNode> Stack { get; } = new Stack<CurrentNode> ();

		class CurrentNode
		{
			public XElement Element {
				get;
			}

			public bool IsMarked {
				get; set;
			}

			public CurrentNode (XElement element)
			{
				Element = element;
			}

			public CurrentNode (string name)
			{
				Element = new XElement (name);
			}
		}

		public XElementWriter (string name)
		{
			Root = new XElement (name);
			Stack.Push (new CurrentNode (Root));
		}

		void Visit<T> (T node, string elementName, Func<T, XElement, bool> func)
			where T : Node
		{
			var current = new CurrentNode (elementName);

			if (!func (node, current.Element))
				return;

			Stack.Push (current);
			node.VisitChildren (this);
			Stack.Pop ();

			if (current.IsMarked) {
				Stack.Peek ().Element.Add (current.Element);
				Stack.Peek ().IsMarked = true;
			}
		}

		void IVisitor.Visit (RootNode node) => Visit (node, "root", Visit);

		void IVisitor.Visit (SizeReport node)
		{
			node.VisitChildren (this);
		}

		void IVisitor.Visit (Assembly node) => Visit (node, "assembly", Visit);

		void IVisitor.Visit (Namespace node) => Visit (node, "namespace", Visit);

		void IVisitor.Visit (Type node) => Visit (node, "type", Visit);

		void IVisitor.Visit (Method node) => Visit (node, "method", Visit);

		void IVisitor.Visit (FailList node)
		{
			throw new NotImplementedException ();
		}

		void IVisitor.Visit (FailListEntry node)
		{
			throw new NotImplementedException ();
		}

		protected bool Visit (RootNode node, XElement element)
		{
			return true;
		}

		protected bool Visit (Assembly node, XElement element)
		{
			element.SetAttributeValue ("name", node.Name);
			return true;
		}

		protected bool Visit (Namespace node, XElement element)
		{
			element.SetAttributeValue ("name", node.Name);
			return true;
		}

		protected bool Visit (Type node, XElement element)
		{
			element.SetAttributeValue ("name", node.Name);
			return true;
		}

		protected bool Visit (Method node, XElement element)
		{
			Stack.Peek ().IsMarked = true;
			element.SetAttributeValue ("name", node.Name);
			return true;
		}
	}
}
