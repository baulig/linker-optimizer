//
// Method.cs
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
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	using BasicBlocks;

	public class Method : Node
	{
		public string Name {
			get;
		}

		public MatchKind Match {
			get;
		}

		public MethodAction Action {
			get;
		}

		public bool HasAction {
			get; set;
		}

		public bool Matches (MethodDefinition method, MethodAction? action = null)
		{
			if (action != null && action.Value != Action)
				return false;

			switch (Match) {
			case MatchKind.FullName:
				return method.FullName == Name;
			case MatchKind.Substring:
				return method.FullName.Contains (Name);
			default:
				if (Name.Contains ('('))
					return method.Name + CecilHelper.GetMethodSignature (method) == Name;
				return method.Name == Name;
			}
		}

		public Method (string name)
		{
			Name = name;
			Match = MatchKind.Name;
			Action = MethodAction.None;
		}

		public Method (string name, MatchKind match, MethodAction action)
		{
			Name = name;
			Match = match;
			Action = action;
		}

		public override void Visit (IVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void VisitChildren (IVisitor visitor)
		{
		}

		public override string ToString ()
		{
			return $"[{GetType ().Name} {Name} {Match} {Action}]";
		}
	}
}
