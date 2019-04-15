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
using System.Linq;
using System.Collections.Generic;
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

		public Action<Type> TypeCallback {
			get;
		}

		public Action<Method> MethodCallback {
			get;
		}

		ActionVisitor (OptimizerOptions options, TypeDefinition type, Action<Type> callback)
		{
			Options = options;
			Type = type;
			TypeCallback = callback;
		}

		ActionVisitor (OptimizerOptions options, MethodDefinition method, Action<Method> callback)
		{
			Options = options;
			Type = method.DeclaringType;
			Method = method;
			MethodCallback = callback;
		}

		public static void Visit (OptimizerOptions options, TypeDefinition type, Action<Type> callback)
		{
			new ActionVisitor (options, type, callback).Visit ();
		}

		public static void Visit (OptimizerOptions options, MethodDefinition method, Action<Method> callback)
		{
			new ActionVisitor (options, method, callback).Visit ();
		}

		public static void Visit (OptimizerOptions options, TypeDefinition type, Action<TypeAction> callback)
		{
			new ActionVisitor (options, type, node => callback (node.Action)).Visit ();
		}

		public static void Visit (OptimizerOptions options, MethodDefinition method, Action<MethodAction> callback)
		{
			new ActionVisitor (options, method, node => callback (node.Action ?? MethodAction.None)).Visit ();
		}

		public static IList<Type> GetNodes (OptimizerOptions options, TypeDefinition type, Func<Type, bool> filter)
		{
			var list = new List<Type> ();
			new ActionVisitor (options, type, node => {
				if (filter (node))
					list.Add (node);
			}).Visit ();
			return list;
		}

		public static IList<Method> GetNodes (OptimizerOptions options, MethodDefinition method, Func<Method, bool> filter)
		{
			var list = new List<Method> ();
			new ActionVisitor (options, method, node => {
				if (filter (node))
					list.Add (node);
			}).Visit ();
			return list;
		}

		public static bool Any (OptimizerOptions options, TypeDefinition type, TypeAction action)
		{
			return GetNodes (options, type, node => node.Action == action).Count > 0;
		}

		public static bool Any (OptimizerOptions options, MethodDefinition method, MethodAction action)
		{
			return GetNodes (options, method, node => node.Action == action).Count > 0;
		}

		public void Visit ()
		{
			Options.OptimizerConfiguration.Visit (this);
		}

		public void Visit (OptimizerConfiguration node)
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

		public void Visit (OptimizerReport report)
		{
		}

		public void Visit (Assembly node)
		{
		}

		public void Visit (Type node)
		{
			if (!node.Matches (Type))
				return;
			if (TypeCallback != null && node.Action != TypeAction.None)
				TypeCallback (node);
			node.VisitChildren (this);
		}

		public void Visit (Method node)
		{
			if (Method == null || !node.Matches (Method))
				return;
			if (MethodCallback != null && node.Action != MethodAction.None)
				MethodCallback (node);
			node.VisitChildren (this);
		}

		public void Visit (FailList node)
		{
		}

		public void Visit (FailListEntry node)
		{
		}

		public void Visit (FailListNode node)
		{
		}
	}
}
