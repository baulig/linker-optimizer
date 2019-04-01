//
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
	public class ReportWriter
	{
		public OptimizerContext Context {
			get;
		}

		readonly Dictionary<string, TypeEntry> _namespace_hash;

		public ReportWriter (OptimizerContext context)
		{
			Context = context;

			_namespace_hash = new Dictionary<string, TypeEntry> ();
		}

		public void DumpConstantProperties ()
		{
			var settings = new XmlWriterSettings {
				Indent = true,
				OmitXmlDeclaration = true,
				ConformanceLevel = ConformanceLevel.Fragment,
				IndentChars = "\t"
			};
			var output = new StringBuilder ();
			output.AppendLine ();
			using (var xml = XmlWriter.Create (output, settings))
				DumpConstantProperties (xml);
			output.AppendLine ();
			Context.LogMessage (MessageImportance.High, $"CONDITIONAL XML SECTION:");
			Context.LogMessage (MessageImportance.High, output.ToString ());
		}

		void DumpConstantProperties (XmlWriter xml)
		{
			var methods = Context.GetConstantMethods ();
			if (methods.Count == 0)
				return;

			var ns = new Dictionary<string, TypeEntry> ();

			foreach (var method in methods) {
				if (method.DeclaringType.DeclaringType != null)
					throw new NotSupportedException ($"Conditionals in nested classes are not supported yet.");

				if (!ns.TryGetValue (method.DeclaringType.Namespace, out var entry)) {
					entry = new TypeEntry (method.DeclaringType.Namespace);
					ns.Add (entry.Name, entry);
				}

				if (!entry.Children.TryGetValue (method.DeclaringType.Name, out var typeEntry)) {
					typeEntry = new TypeEntry (method.DeclaringType.Name);
					entry.Children.Add (typeEntry.Name, typeEntry);
				}

				typeEntry.Items.Add (method.Name);
			}

			foreach (var entry in ns.Values) {
				xml.WriteStartElement ("namespace");
				xml.WriteAttributeString ("name", entry.Name);

				foreach (var type in entry.Children.Values) {
					xml.WriteStartElement ("type");
					xml.WriteAttributeString ("name", type.Name);
					foreach (var item in type.Items) {
						xml.WriteStartElement ("method");
						xml.WriteAttributeString ("name", item);
						xml.WriteAttributeString ("action", "scan");
						xml.WriteEndElement ();
					}
					xml.WriteEndElement ();
				}
				xml.WriteEndElement ();
			}
		}

		TypeEntry GetTypeEntry (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				throw new NotSupportedException ("Nested types are not supported yet.");

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

		class TypeEntry
		{
			public readonly string Name;
			public readonly Dictionary<string, TypeEntry> Children;
			public readonly List<string> Items;

			public TypeEntry (string name)
			{
				Name = name;
				Children = new Dictionary<string, TypeEntry> ();
				Items = new List<string> ();
			}
		}

		class MethodEntry
		{
			public readonly string Name;

			public MethodEntry (string name)
			{
				Name = name;
			}
		}
	}
}
