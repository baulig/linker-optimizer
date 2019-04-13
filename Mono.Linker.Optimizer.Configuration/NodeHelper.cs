﻿//
// NodeHelper.cs
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
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	using BasicBlocks;

	static class NodeHelper
	{
		internal static Assembly GetAssembly (this NodeList<Assembly> list, AssemblyDefinition assembly, bool add)
		{
			return GetAssembly (list, assembly.Name.Name, add);
		}

		internal static Assembly GetAssembly (this NodeList<Assembly> list, string name, bool add)
		{
			return list.GetChild (a => a.Name == name, () => add ? new Assembly (name) : null);
		}

		internal static Namespace GetNamespace (this NodeList<Namespace> list, string name, bool add)
		{
			return list.GetChild (n => n.Name == name, () => add ? new Namespace (name) : null);
		}

		internal static Type GetType (this NodeList<Type> list, TypeDefinition type, bool add)
		{
			return GetType (list, type.Name, type.FullName, add);
		}

		internal static Type GetType (this NodeList<Type> list, string name, string fullName, bool add)
		{
			return list.GetChild (t => t.Name == name, () => add ? new Type (name, fullName) : null);
		}

		internal static Method GetMethod (this NodeList<Method> list, MethodDefinition method, bool add)
		{
			return GetMethod (list, method.Name + CecilHelper.GetMethodSignature (method), add);
		}

		internal static Method GetMethod (this NodeList<Method> list, string name, bool add)
		{
			return list.GetChild (m => m.Name == name, () => add ? new Method (name) : null);
		}

		internal static void ProcessChildren (this XPathNavigator nav, string children, Action<XPathNavigator> action)
		{
			var iterator = nav.Select (children);
			while (iterator.MoveNext ())
				action (iterator.Current);
		}

		internal static bool GetBoolAttribute (this XPathNavigator nav, string name, out bool value)
		{
			var attr = GetAttribute (nav, name);
			if (attr != null && bool.TryParse (attr, out value))
				return true;
			value = false;
			return false;
		}

		internal static string GetAttribute (this XPathNavigator nav, string attribute)
		{
			var attr = nav.GetAttribute (attribute, string.Empty);
			return string.IsNullOrWhiteSpace (attr) ? null : attr;
		}

		internal static TypeAction GetTypeAction (this XPathNavigator nav, string name)
		{
			if (TryGetTypeAction (nav, name, out var action))
				return action;
			return TypeAction.None;
		}

		internal static bool TryGetTypeAction (this XPathNavigator nav, string name, out TypeAction action)
		{
			var attribute = GetAttribute (nav, name);
			if (attribute == null) {
				action = TypeAction.None;
				return false;
			}
			return Enum.TryParse (name, true, out action);
		}

		internal static bool TryGetMethodAction (this XPathNavigator nav, string name, out MethodAction action)
		{
			var attribute = GetAttribute (nav, name);
			if (attribute == null) {
				action = MethodAction.None;
				return false;
			}

			switch (attribute.ToLowerInvariant ()) {
			case "return-null":
				action = MethodAction.ReturnNull;
				return true;
			case "return-false":
				action = MethodAction.ReturnFalse;
				return true;
			case "return-true":
				action = MethodAction.ReturnTrue;
				return true;
			default:
				return Enum.TryParse (name, true, out action);
			}
		}
	}
}
