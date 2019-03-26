//
// PreloadStep.cs
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
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Conditionals
{
	public class PreprocessStep : BaseStep
	{
		protected override bool ConditionToProcess ()
		{
			return Context.MartinContext.Options.Preprocess;
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (var type in assembly.MainModule.Types) {
				ProcessType (type);
			}

			base.ProcessAssembly (assembly);
		}

		protected override void EndProcess ()
		{
			DumpConstantProperties ();

			base.EndProcess ();
		}

		void ProcessType (TypeDefinition type)
		{
			Context.MartinContext.Options.ProcessTypeEntries (type, a => ProcessTypeActions (type, a));

			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					ProcessType (nested);
			}

			foreach (var method in type.Methods) {
				ProcessMethod (method);
			}

			foreach (var property in type.Properties) {
				ProcessProperty (property);
			}
		}

		void ProcessMethod (MethodDefinition method)
		{
			Context.MartinContext.Options.ProcessMethodEntries (method, a => ProcessMethodActions (method, a));
		}

		void ProcessProperty (PropertyDefinition property)
		{
			if (property.SetMethod != null)
				return;
			if (property.GetMethod == null || !property.GetMethod.HasBody)
				return;
			if (property.PropertyType.MetadataType != MetadataType.Boolean)
				return;

			var scanner = BasicBlockScanner.Scan (Context.MartinContext, property.GetMethod);
			if (scanner == null || !scanner.FoundConditionals)
				return;

			Context.MartinContext.LogMessage (MessageImportance.Normal, $"Found conditional property: {property}");

			scanner.RewriteConditionals ();

			if (!CecilHelper.IsConstantLoad (scanner.Body, out var value)) {
				Context.MartinContext.LogMessage (MessageImportance.High, $"Property `{property}` uses conditionals, but does not return a constant.");
				return;
			}

			Context.MartinContext.MarkAsConstantProperty (property, value);

			Context.MartinContext.Debug ();
		}

		void ProcessTypeActions (TypeDefinition type, MartinOptions.TypeAction action)
		{
			switch (action) {
			case MartinOptions.TypeAction.Debug:
				Context.MartinContext.LogMessage (MessageImportance.High, $"Debug type: {type} {action}");
				Context.MartinContext.Debug ();
				break;

			case MartinOptions.TypeAction.Preserve:
				Context.Annotations.SetPreserve (type, TypePreserve.All);
				Context.Annotations.Mark (type);
				break;
			}
		}


		void ProcessMethodActions (MethodDefinition method, MartinOptions.MethodAction action)
		{
			switch (action) {
			case MartinOptions.MethodAction.Debug:
				Context.MartinContext.LogMessage (MessageImportance.High, $"Debug method: {method} {action}");
				Context.MartinContext.Debug ();
				break;

			case MartinOptions.MethodAction.Throw:
				Context.Annotations.SetAction (method, MethodAction.ConvertToThrow);
				break;
			}
		}

		void DumpConstantProperties ()
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
			Context.MartinContext.LogMessage (MessageImportance.High, $"CONDITIONAL XML SECTION:");
			Context.MartinContext.LogMessage (MessageImportance.High, output.ToString ());
		}

		void DumpConstantProperties (XmlWriter xml)
		{
			var properties = Context.MartinContext.GetConstantProperties ();
			if (properties.Count == 0)
				return;

			var ns = new Dictionary<string, TypeEntry> ();

			foreach (var property in properties) {
				if (property.DeclaringType.DeclaringType != null)
					throw new NotSupportedException ($"Conditionals in nested classes are not supported yet.");

				if (!ns.TryGetValue (property.DeclaringType.Namespace, out var entry)) {
					entry = new TypeEntry (property.DeclaringType.Namespace);
					ns.Add (entry.Name, entry);
				}

				if (!entry.Children.TryGetValue (property.DeclaringType.Name, out var typeEntry)) {
					typeEntry = new TypeEntry (property.DeclaringType.Name);
					entry.Children.Add (typeEntry.Name, typeEntry);
				}

				typeEntry.Items.Add (property.Name);
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
	}
}
