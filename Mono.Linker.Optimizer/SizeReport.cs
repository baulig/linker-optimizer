//
// SizeReport.cs
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
using System.Threading;
using Mono.Cecil;
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;

	public class SizeReport
	{
		public OptimizerOptions Options {
			get;
		}

		readonly List<ConfigurationEntry> _configuration_entries;

		public SizeReport (OptimizerOptions options)
		{
			Options = options;

			_configuration_entries = new List<ConfigurationEntry> ();
		}

		public void Read (XPathNavigator nav)
		{
			var name = OptionsReader.GetAttribute (nav, "configuration");
			var configuration = GetConfigurationEntry (name, true);

			OptionsReader.ProcessChildren (nav, "profile", child => OnProfileEntry (child, configuration));
		}

		void OnProfileEntry (XPathNavigator nav, ConfigurationEntry configuration)
		{
			var profile = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<profile> requires `name` attribute.");

			var entry = new SizeReportEntry (profile);
			configuration.SizeReportEntries.Add (entry);

			OptionsReader.ProcessChildren (nav, "assembly", child => OnAssemblyEntry (child, entry));
		}

		void OnAssemblyEntry (XPathNavigator nav, SizeReportEntry entry)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<assembly> requires `name` attribute.");
			var sizeAttr = OptionsReader.GetAttribute (nav, "size");
			if (sizeAttr == null || !int.TryParse (sizeAttr, out var size))
				throw OptionsReader.ThrowError ("<assembly> requires `name` attribute.");
			var toleranceAttr = OptionsReader.GetAttribute (nav, "tolerance");

			var assembly = new AssemblySizeEntry (name, size, toleranceAttr);
			entry.Assemblies.Add (assembly);

			OptionsReader.ProcessChildren (nav, "namespace", child => OnNamespaceEntry (child, assembly));
		}

		void OnNamespaceEntry (XPathNavigator nav, AssemblySizeEntry assembly)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<namespace> requires `name` attribute.");

			var ns = assembly.GetNamespace (name);

			OptionsReader.ProcessChildren (nav, "type", child => OnTypeEntry (child, ns));
		}

		void OnTypeEntry (XPathNavigator nav, NamespaceEntry parent)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<type> requires `name` attribute.");
			var fullName = OptionsReader.GetAttribute (nav, "full-name") ?? throw OptionsReader.ThrowError ("<type> requires `full-name` attribute.");
			parent.GetType (name, fullName);
		}

		SizeReportEntry GetSizeReportEntry (string configuration, string profile)
		{
			var configEntry = GetConfigurationEntry (configuration, false);
			if (configEntry == null)
				return null;
			return configEntry.SizeReportEntries.FirstOrDefault (e => e.Profile == profile);
		}

		ConfigurationEntry GetConfigurationEntry (string configuration, bool add)
		{
			var entry = _configuration_entries.FirstOrDefault (e => e.Configuration == configuration);
			if (add && entry == null) {
				entry = new ConfigurationEntry (configuration);
				_configuration_entries.Add (entry);
			}
			return entry;
		}

		string SizeReportProfile {
			get {
				switch (Options.CheckSize) {
				case null:
				case "false":
					return null;
				case "true":
					return Options.ProfileName ?? "default";
				default:
					return Options.CheckSize;
				}
			}
		}

		bool CheckAssemblySize (OptimizerContext context, string assembly, int size)
		{
			if (SizeReportProfile == null)
				return true;

			var entry = GetSizeReportEntry (Options.SizeCheckConfiguration, SizeReportProfile);
			if (entry == null) {
				context.LogMessage (MessageImportance.High, $"Cannot find size entries for profile `{SizeReportProfile}`.");
				return false;
			}

			var asmEntry = entry.Assemblies.FirstOrDefault (e => e.Name == assembly);
			if (asmEntry == null)
				return true;

			int tolerance;
			string toleranceValue = asmEntry.Tolerance ?? Options.SizeCheckTolerance ?? "0.05%";

			if (toleranceValue.EndsWith ("%", StringComparison.Ordinal)) {
				var percent = float.Parse (toleranceValue.Substring (0, toleranceValue.Length - 1));
				tolerance = (int)(asmEntry.Size * percent / 100.0f);
			} else {
				tolerance = int.Parse (toleranceValue);
			}

			context.LogDebug ($"Size check: {asmEntry.Name}, actual={size}, expected={asmEntry.Size} (tolerance {toleranceValue})");

			if (size < asmEntry.Size - tolerance) {
				context.LogMessage (MessageImportance.High, $"Assembly `{asmEntry.Name}` size below minimum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}
			if (size > asmEntry.Size + tolerance) {
				context.LogMessage (MessageImportance.High, $"Assembly `{asmEntry.Name}` size above maximum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}

			return true;
		}

		void ReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			var configEntry = GetConfigurationEntry (Options.SizeCheckConfiguration, true);
			var sizeEntry = configEntry.SizeReportEntries.FirstOrDefault (e => e.Profile == SizeReportProfile);
			if (sizeEntry == null) {
				sizeEntry = new SizeReportEntry (SizeReportProfile);
				configEntry.SizeReportEntries.Add (sizeEntry);
			}

			var asmEntry = sizeEntry.Assemblies.FirstOrDefault (e => e.Name == assembly.Name.Name);
			if (asmEntry == null) {
				asmEntry = new AssemblySizeEntry (assembly.Name.Name, size, null);
				sizeEntry.Assemblies.Add (asmEntry);
			} else {
				asmEntry.Size = size;
			}

			ReportDetailed (context, assembly, asmEntry);
		}

		public void Write (XmlWriter xml)
		{
			foreach (var configuration in _configuration_entries) {
				xml.WriteStartElement ("size-check");
				xml.WriteAttributeString ("configuration", configuration.Configuration);

				foreach (var entry in configuration.SizeReportEntries) {
					xml.WriteStartElement ("profile");
					xml.WriteAttributeString ("name", entry.Profile);
					foreach (var asm in entry.Assemblies) {
						xml.WriteStartElement ("assembly");
						xml.WriteAttributeString ("name", asm.Name);
						xml.WriteAttributeString ("size", asm.Size.ToString ());
						if (asm.Tolerance != null)
							xml.WriteAttributeString ("tolerance", asm.Tolerance);

						if (Options.DetailedSizeReport) {
							foreach (var ns in asm.GetNamespaces ()) {
								if (string.IsNullOrEmpty (ns.Name))
									continue;
								WriteDetailedReport (xml, ns);
							}
						}

						xml.WriteEndElement ();

					}
					xml.WriteEndElement ();
				}

				xml.WriteEndElement ();
			}
		}

		internal bool CheckAndReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			ReportAssemblySize (context, assembly, size);

			return CheckAssemblySize (context, assembly.Name.Name, size);
		}

		void ReportDetailed (OptimizerContext context, AssemblyDefinition assembly, AssemblySizeEntry entry)
		{
			foreach (var type in assembly.MainModule.Types) {
				ProcessType (context, entry, type);
			}
		}

		void WriteDetailedReport (XmlWriter xml, NamespaceEntry entry)
		{
			xml.WriteStartElement ("namespace");
			xml.WriteAttributeString ("name", entry.Name);
			xml.WriteAttributeString ("size", entry.Size.ToString ());
			xml.WriteAttributeString ("marked", entry.Marked.ToString ());

			foreach (var type in entry.GetTypes ())
				WriteDetailedReport (xml, type);

			xml.WriteEndElement ();
		}

		void WriteDetailedReport (XmlWriter xml, TypeEntry entry)
		{
			xml.WriteStartElement ("type");
			xml.WriteAttributeString ("name", entry.Name);
			xml.WriteAttributeString ("full-name", entry.FullName);
			xml.WriteAttributeString ("size", entry.Size.ToString ());
			xml.WriteAttributeString ("marked", entry.Marked.ToString ());

			foreach (var type in entry.GetNestedTypes ())
				WriteDetailedReport (xml, type);

			foreach (var method in entry.GetMethods ())
				WriteDetailedReport (xml, method);

			xml.WriteEndElement ();
		}

		void WriteDetailedReport (XmlWriter xml, MethodEntry entry)
		{
			xml.WriteStartElement ("method");
			xml.WriteAttributeString ("name", entry.Name);
			xml.WriteAttributeString ("size", entry.Size.ToString ());
			xml.WriteEndElement ();
		}

		void ProcessType (OptimizerContext context, AssemblySizeEntry parent, TypeDefinition type)
		{
			if (type.Name == "<Module>")
				return;
			if (!context.Annotations.IsMarked (type))
				return;
			if (type.FullName.StartsWith ("<PrivateImplementationDetails>", StringComparison.Ordinal))
				return;

			var ns = parent.GetNamespace (type.Namespace);
			ns.Marked = true;

			var entry = ns.GetType (type);
			entry.Marked = true;

			foreach (var method in type.Methods)
				ProcessMethod (context, entry, method);

			foreach (var nested in type.NestedTypes)
				ProcessType (context, entry, nested);
		}

		void ProcessType (OptimizerContext context, TypeEntry parent, TypeDefinition type)
		{
			if (!context.Annotations.IsMarked (type))
				return;

			var entry = parent.GetNestedType (type, true);
			entry.Marked = true;

			foreach (var method in type.Methods)
				ProcessMethod (context, entry, method);

			foreach (var nested in type.NestedTypes)
				ProcessType (context, entry, nested);
		}

		void ProcessMethod (OptimizerContext context, TypeEntry parent, MethodDefinition method)
		{
			if (!method.HasBody || !context.Annotations.IsMarked (method))
				return;

			if (!parent.AddMethod (method))
				return;

			if (Options.HasTypeEntry (method.DeclaringType, OptimizerOptions.TypeAction.Size))
				context.LogMessage (MessageImportance.Normal, $"SIZE: {method.FullName} {method.Body.CodeSize}");
		}

		class ConfigurationEntry
		{
			public string Configuration {
				get;
			}

			public List<SizeReportEntry> SizeReportEntries {
				get;
			}

			public ConfigurationEntry (string configuration)
			{
				Configuration = configuration;
				SizeReportEntries = new List<SizeReportEntry> ();
			}
		}

		class SizeReportEntry
		{
			public string Profile {
				get;
			}

			public List<AssemblySizeEntry> Assemblies {
				get;
			}

			public SizeReportEntry (string profile)
			{
				Profile = profile;
				Assemblies = new List<AssemblySizeEntry> ();
			}
		}

		abstract class SizeEntry : IComparable<SizeEntry>
		{
			public SizeEntry Parent {
				get;
			}

			public string Name {
				get;
			}

			public int Size {
				get; set;
			}

			public bool Marked {
				get; set;
			}

			void AddSize (int size)
			{
				Size += size;
				Parent?.AddSize (size);
			}

			protected SizeEntry (SizeEntry parent, string name, int size)
			{
				Parent = parent;
				Name = name;

				AddSize (size);
			}

			public int CompareTo (SizeEntry obj)
			{
				return Size.CompareTo (obj.Size);
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size}]";
			}
		}

		class AssemblySizeEntry : SizeEntry
		{
			public string Tolerance {
				get;
			}

			Dictionary<string, NamespaceEntry> namespaces;

			public NamespaceEntry GetNamespace (string name, bool add = true)
			{
				LazyInitializer.EnsureInitialized (ref namespaces);
				if (namespaces.TryGetValue (name, out var entry))
					return entry;
				if (!add)
					return null;
				entry = new NamespaceEntry (this, name);
				namespaces.Add (name, entry);
				return entry;
			}

			public SortedSet<NamespaceEntry> GetNamespaces ()
			{
				var set = new SortedSet<NamespaceEntry> ();
				if (namespaces != null) {
					foreach (var ns in namespaces.Values)
						set.Add (ns);
				}
				return set;
			}

			public AssemblySizeEntry (string name, int size, string tolerance)
				: base (null, name, size)
			{
				Tolerance = tolerance;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size} {Tolerance}]";
			}
		}

		class NamespaceEntry : SizeEntry
		{
			Dictionary<string, TypeEntry> types;

			public TypeEntry GetType (TypeDefinition type, bool add = true)
			{
				return GetType (type.Name, type.FullName, add);
			}

			public TypeEntry GetType (string name, string fullName, bool add = true)
			{
				LazyInitializer.EnsureInitialized (ref types);
				if (types.TryGetValue (name, out var entry))
					return entry;
				if (!add)
					return null;
				entry = new TypeEntry (this, name, fullName);
				types.Add (name, entry);
				return entry;
			}

			public SortedSet<TypeEntry> GetTypes ()
			{
				var set = new SortedSet<TypeEntry> ();
				if (types != null) {
					foreach (var type in types.Values)
						set.Add (type);
				}
				return set;
			}

			public NamespaceEntry (AssemblySizeEntry parent, string name)
				: base (parent, name, 0)
			{
			}
		}

		class TypeEntry : SizeEntry
		{
			public bool HasNestedTypes => nested != null;

			public TypeEntry GetNestedType (TypeDefinition type, bool add)
			{
				LazyInitializer.EnsureInitialized (ref nested);
				if (nested.TryGetValue (type.Name, out var entry))
					return entry;
				if (!add)
					return null;
				entry = new TypeEntry (this, type.Name, type.FullName);
				nested.Add (type.Name, entry);
				return entry;
			}

			public SortedSet<TypeEntry> GetNestedTypes ()
			{
				var set = new SortedSet<TypeEntry> ();
				if (nested != null) {
					foreach (var type in nested.Values)
						set.Add (type);
				}
				return set;
			}

			public bool AddMethod (MethodDefinition method)
			{
				LazyInitializer.EnsureInitialized (ref methods);

				var name = method.Name + CecilHelper.GetMethodSignature (method);
				if (methods.ContainsKey (name))
					return false;

				methods.Add (name, new MethodEntry (this, name, method.Body.CodeSize));
				return true;
			}

			public SortedSet<MethodEntry> GetMethods ()
			{
				var set = new SortedSet<MethodEntry> ();
				if (methods != null) {
					foreach (var method in methods.Values)
						set.Add (method);
				}
				return set;
			}

			Dictionary<string, TypeEntry> nested;
			Dictionary<string, MethodEntry> methods;

			public string FullName {
				get;
			}

			public TypeEntry (SizeEntry parent, string name, string fullName)
				: base (parent, name, 0)
			{
				FullName = fullName;
			}
		}

		class MethodEntry : SizeEntry
		{
			public MethodEntry (TypeEntry parent, string name, int size)
				: base (parent, name, size)
			{
			}
		}
	}
}
