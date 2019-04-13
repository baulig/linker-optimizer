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

		Stack<XElement> CurrentNode { get; } = new Stack<XElement> ();

		public XElementWriter (string name)
		{
			Root = new XElement (name);
			CurrentNode.Push (Root);
		}

#if FIXME
		void Visit<T> (T entry, Func<T, XElement, bool> func)
			where T : Node
		{
			var element = new XElement (entry.ElementName);

			if (!func (entry, element))
				return;

			CurrentNode.Push (element);
			entry.VisitChildren (this);
			CurrentNode.Pop ();

			if (!element.IsEmpty || element.HasAttributes || element.HasElements)
				CurrentNode.Peek ().Add (element);
		}
#endif

		void IVisitor.Visit (RootNode node)
		{
			var element = new XElement ("root");
			CurrentNode.Push (element);
			node.VisitChildren (this);

			CurrentNode.Pop ();

			if (!element.IsEmpty || element.HasAttributes || element.HasElements)
				CurrentNode.Peek ().Add (element);
		}

		void IVisitor.Visit (SizeReport node)
		{
			node.VisitChildren (this);
		}

		void IVisitor.Visit (Assembly node)
		{
			// throw new NotImplementedException ();
		}

		void IVisitor.Visit (Namespace node)
		{
			throw new NotImplementedException ();
		}

		void IVisitor.Visit (Type node)
		{
			throw new NotImplementedException ();
		}

		void IVisitor.Visit (Method node)
		{
			throw new NotImplementedException ();
		}

		void IVisitor.Visit (FailList node)
		{
			throw new NotImplementedException ();
		}

		void IVisitor.Visit (FailListEntry node)
		{
			throw new NotImplementedException ();
		}
	}
}
