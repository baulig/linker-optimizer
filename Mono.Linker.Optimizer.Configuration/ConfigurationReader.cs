﻿//
// ConfigurationReader.cs
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
	public class ConfigurationReader
	{
		public RootNode Root {
			get;
		}

		public ConfigurationReader (RootNode root)
		{
			Root = root;
		}

		public void Read (XPathNavigator nav)
		{
			nav.ProcessChildren ("conditional", OnConditional);

			var list = new ActionList ();

			nav.ProcessChildren ("namespace", child => OnNamespaceEntry (child, list));

			//			ProcessChildren (root, "type", child => OnTypeEntry (child, null));
//			ProcessChildren (root, "method", child => OnMethodEntry (child));

		}

		void OnConditional (XPathNavigator nav)
		{
			var name = nav.GetAttribute ("feature");
			if (name == null || !nav.GetBoolAttribute ("enabled", out var enabled))
				throw ThrowError ("<conditional> needs both `feature` and `enabled` arguments.");

			var feature = OptimizerOptions.FeatureByName (name);

			var conditional = new ActionList (name);
			nav.ProcessChildren ("namespace", child => OnNamespaceEntry (child, conditional));
			nav.ProcessChildren ("type", child => OnTypeEntry (child, conditional, null));

			// ProcessChildren (nav, "namespace", child => OnNamespaceEntry (child, Conditional));
			// ProcessChildren (nav, "type", child => OnTypeEntry (child, null, Conditional));
			// ProcessChildren (nav, "method", child => OnMethodEntry (child, null, Conditional));

			// bool Conditional (MemberReference reference) => Options.IsFeatureEnabled (feature) == enabled;
		}

		void OnNamespaceEntry (XPathNavigator nav, ActionList parent)
		{
			var name = nav.GetAttribute ("name") ?? throw ThrowError ("<namespace> entry needs `name` attribute.");

			var action = nav.GetTypeAction ("action");
			var node = new Namespace (name, action);
			parent.Add (node);

			nav.ProcessChildren ("type", child => OnTypeEntry (child, parent, node));
			nav.ProcessChildren ("method", child => OnMethodEntry (child, parent, node));
		}

		void OnTypeEntry (XPathNavigator nav, ActionList list, AbstractType parent)
		{
			if (!GetName (nav, out var name, out var match))
				throw ThrowError ($"Ambiguous name in type entry `{nav.OuterXml}`.");

			var action = nav.GetTypeAction ("action");
			var type = new Type (name, match, action);
			if (parent != null)
				parent.Types.Add (type);
			else
				list.Add (type);

			nav.ProcessChildren ("type", child => OnTypeEntry (nav, list, type));
			nav.ProcessChildren ("method", child => OnMethodEntry (child, list, type));
		}

		void OnMethodEntry (XPathNavigator nav, ActionList list, AbstractType parent)
		{
			if (!GetName (nav, out var name, out var match))
				throw ThrowError ($"Ambiguous name in method entry `{nav.OuterXml}`.");

			if (!nav.TryGetMethodAction ("action", out var action))
				throw ThrowError ($"Missing `action` attribute in {nav.OuterXml}.");

			var method = new Method (name);
			if (parent != null)
				parent.Methods.Add (method);
			else
				list.Add (method);
		}

		bool GetName (XPathNavigator nav, out string name, out MatchKind match)
		{
			name = nav.GetAttribute ("name");
			var fullname = nav.GetAttribute ("fullname");
			var substring = nav.GetAttribute ("substring");

			if (fullname != null) {
				match = MatchKind.FullName;
				if (name != null || substring != null)
					return false;
				name = fullname;
			} else if (name != null) {
				match = MatchKind.Name;
				if (fullname != null || substring != null)
					return false;
			} else if (substring != null) {
				match = MatchKind.Substring;
				if (name != null || fullname != null)
					return false;
				name = substring;
			} else {
				match = MatchKind.Name;
				return false;
			}

			return true;
		}

		internal static Exception ThrowError (string message)
		{
			throw new OptimizerException ($"Invalid XML: {message}");
		}
	}
}
