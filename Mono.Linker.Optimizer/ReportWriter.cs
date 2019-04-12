﻿//
// ReportWriter.cs
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
using System.Xml;
using System.Text;
using Mono.Cecil;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;

	public class ReportWriter
	{
		public OptimizerContext Context {
			get;
		}

		public OptimizerOptions Options => Context.Options;

		readonly Dictionary<string, TypeEntry> _namespace_hash;
		readonly List<FailListEntry> _fail_list;

		public ReportWriter (OptimizerContext context)
		{
			Context = context;

			_namespace_hash = new Dictionary<string, TypeEntry> ();
			_fail_list = new List<FailListEntry> ();
		}

		public void MarkAsContainingConditionals (MethodDefinition method)
		{
			if (method.DeclaringType.DeclaringType != null)
				throw new OptimizerAssertionException ($"Conditionals in nested classes are not supported yet.");

			GetMethodEntry (method);
		}

		public void MarkAsConstantMethod (MethodDefinition method, ConstantValue value)
		{
			if (method.DeclaringType.DeclaringType != null)
				throw new OptimizerAssertionException ($"Conditionals in nested classes are not supported yet.");

			GetMethodEntry (method).ConstantValue = value;
		}

		public void RemovedDeadBlocks (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedDeadBlocks;
		}

		public void RemovedDeadExceptionBlocks (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedExceptionBlocks;
		}

		public void RemovedDeadJumps (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedDeadJumps;
		}

		public void RemovedDeadConstantJumps (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedConstantJumps;
		}

		public void RemovedDeadVariables (MethodDefinition method)
		{
			GetMethodEntry (method).DeadCodeMode |= DeadCodeMode.RemovedDeadVariables;
		}

		public void ReportFailListEntry (TypeDefinition type, OptimizerOptions.TypeEntry entry, string original, List<string> stack)
		{
			var fail = new FailListEntry (type.FullName) {
				Original = original
			};
			fail.TracerStack.AddRange (stack);
			_fail_list.Add (fail);

			while (entry != null) {
				fail.EntryStack.Add (entry.ToString ());
				entry = entry.Parent;
			}
		}

		public void ReportFailListEntry (MethodDefinition method, OptimizerOptions.MethodEntry entry, List<string> stack)
		{
			var fail = new FailListEntry (method.FullName);
			fail.TracerStack.AddRange (stack);
			_fail_list.Add (fail);

			if (entry != null) {
				fail.EntryStack.Add (entry.ToString ());

				var type = entry.Parent;
				while (type != null) {
					fail.EntryStack.Add (type.ToString ());
					type = type.Parent;
				}
			}
		}

		TypeEntry GetTypeEntry (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				throw new OptimizerAssertionException ("Nested types are not supported yet.");

			if (!_namespace_hash.TryGetValue (type.Namespace, out var entry)) {
				entry = new TypeEntry (type.Namespace);
				_namespace_hash.Add (entry.Name, entry);
			}

			if (!entry.Children.TryGetValue (type.Name, out var typeEntry)) {
				typeEntry = new TypeEntry (type.Name);
				entry.Children.Add (typeEntry.Name, typeEntry);
			}

			return typeEntry;
		}

		MethodEntry GetMethodEntry (MethodDefinition method)
		{
			var parent = GetTypeEntry (method.DeclaringType);
			if (!parent.Methods.TryGetValue (method, out var entry)) {
				entry = new MethodEntry (method.Name + CecilHelper.GetMethodSignature (method));
				parent.Methods.Add (method, entry);
			}
			return entry;
		}

		public void WriteReport ()
		{
			var settings = new XmlWriterSettings {
				Indent = true,
				OmitXmlDeclaration = false,
				NewLineHandling = NewLineHandling.None,
				ConformanceLevel = ConformanceLevel.Document,
				IndentChars = "\t",
				Encoding = Encoding.Default
			};

			using (var xml = XmlWriter.Create (Options.ReportFileName, settings)) {
				xml.WriteStartDocument ();
				xml.WriteStartElement ("optimizer-report");
				WriteReport (xml);
				xml.WriteEndElement ();
				xml.WriteEndDocument ();
			}
		}

		void WriteReport (XmlWriter xml)
		{
			WriteActionReport (xml);

			WriteFailReport (xml);

			Context.Report.Write (xml);
		}

		void WriteActionReport (XmlWriter xml)
		{
			xml.WriteStartElement ("action-report");

			foreach (var entry in _namespace_hash.Values) {
				xml.WriteStartElement ("namespace");
				xml.WriteAttributeString ("name", entry.Name);

				foreach (var type in entry.Children.Values) {
					xml.WriteStartElement ("type");
					xml.WriteAttributeString ("name", type.Name);

					foreach (var item in type.Items) {
						xml.WriteStartElement ("item");
						xml.WriteAttributeString ("name", item);
						xml.WriteAttributeString ("action", "scan");
						xml.WriteEndElement ();
					}

					foreach (var item in type.Methods.Values)
						WriteMethodEntry (xml, item);

					xml.WriteEndElement ();
				}
				xml.WriteEndElement ();
			}

			xml.WriteEndElement ();
		}

		void WriteFailReport (XmlWriter xml)
		{
			if (_fail_list == null || _fail_list.Count == 0)
				return;

			xml.WriteStartElement ("fail-list");

			foreach (var fail in _fail_list) {
				xml.WriteStartElement ("fail");
				xml.WriteAttributeString ("name", fail.Name);
				if (fail.Original != null)
					xml.WriteAttributeString ("full-name", fail.Original);
				foreach (var entry in fail.EntryStack) {
					xml.WriteStartElement ("entry");
					xml.WriteAttributeString ("name", entry);
					xml.WriteEndElement ();
				}
				foreach (var entry in fail.TracerStack) {
					xml.WriteStartElement ("stack");
					xml.WriteAttributeString ("name", entry);
					xml.WriteEndElement ();
				}
				xml.WriteEndElement ();
			}

			xml.WriteEndElement ();
		}

		void WriteMethodEntry (XmlWriter xml, MethodEntry entry)
		{
			xml.WriteStartElement ("method");
			xml.WriteAttributeString ("name", entry.Name);

			switch (entry.ConstantValue) {
			case ConstantValue.False:
				xml.WriteAttributeString ("action", "return-false");
				break;
			case ConstantValue.True:
				xml.WriteAttributeString ("action", "return-true");
				break;
			case ConstantValue.Null:
				xml.WriteAttributeString ("action", "return-null");
				break;
			case ConstantValue.Throw:
				xml.WriteAttributeString ("action", "throw");
				break;
			default:
				xml.WriteAttributeString ("action", "scan");
				break;
			}

			if (entry.DeadCodeMode != DeadCodeMode.None)
				xml.WriteAttributeString ("dead-code", FormatDeadCodeMode (entry.DeadCodeMode));

			xml.WriteEndElement ();
		}

		string FormatDeadCodeMode (DeadCodeMode mode)
		{
			if (mode == DeadCodeMode.None)
				return "none";

			var modes = new List<string> ();
			if ((mode & DeadCodeMode.RemovedDeadBlocks) != 0)
				modes.Add ("blocks");
			if ((mode & DeadCodeMode.RemovedExceptionBlocks) != 0)
				modes.Add ("exception-blocks");
			if ((mode & DeadCodeMode.RemovedDeadJumps) != 0)
				modes.Add ("jumps");
			if ((mode & DeadCodeMode.RemovedConstantJumps) != 0)
				modes.Add ("constant-jumps");
			if ((mode & DeadCodeMode.RemovedDeadVariables) != 0)
				modes.Add ("variables");
			return string.Join (",", modes);
		}

		class TypeEntry
		{
			public readonly string Name;
			public readonly Dictionary<string, TypeEntry> Children;
			public readonly Dictionary<MethodDefinition, MethodEntry> Methods;
			public readonly List<string> Items;

			public TypeEntry (string name)
			{
				Name = name;
				Children = new Dictionary<string, TypeEntry> ();
				Methods = new Dictionary<MethodDefinition, MethodEntry> ();
				Items = new List<string> ();
			}
		}

		class MethodEntry
		{
			public readonly string Name;
			public ConstantValue? ConstantValue;
			public DeadCodeMode DeadCodeMode;

			public MethodEntry (string name)
			{
				Name = name;
			}
		}

		[Flags]
		enum DeadCodeMode
		{
			None				= 0,
			RemovedDeadBlocks		= 1,
			RemovedExceptionBlocks		= 2,
			RemovedDeadJumps		= 4,
			RemovedConstantJumps		= 8,
			RemovedDeadVariables		= 16
		}

		class FailListEntry
		{
			public readonly string Name;
			public string Original;
			public readonly List<string> EntryStack;
			public readonly List<string> TracerStack;

			public FailListEntry (string name)
			{
				Name = name;
				EntryStack = new List<string> ();
				TracerStack = new List<string> ();
			}
		}
	}
}
