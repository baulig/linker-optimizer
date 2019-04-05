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
using System.IO;
using System.Linq;
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
		readonly List<DetailedAssemblyEntry> _detailed_assembly_entries;

		public SizeReport (OptimizerOptions options)
		{
			Options = options;

			_configuration_entries = new List<ConfigurationEntry> ();
			_detailed_assembly_entries = new List<DetailedAssemblyEntry> ();
		}

		public void Read (XPathNavigator nav)
		{
			var name = OptionsReader.GetAttribute (nav, "configuration");
			var configuration = GetConfigurationEntry (name, true);

			OptionsReader.ProcessChildren (nav, "profile", child => OnProfileEntry (child, configuration));
		}

		void OnProfileEntry (XPathNavigator nav, ConfigurationEntry configuration)
		{
			var profile = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<proifle> requires `name` attribute.");

			var entry = new SizeReportEntry (configuration, profile);
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
			entry.Assemblies.Add (new AssemblySizeEntry (name, size, toleranceAttr));
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

		public bool IsEnabled => SizeReportProfile != null;

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
			if (!IsEnabled)
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

		public void ReportAssemblySize (string assembly, int size)
		{
			ReportAssemblySize (Options.SizeCheckConfiguration, SizeReportProfile, assembly, size);
		}

		public void ReportAssemblySize (string configuration, string profile, string assembly, int size)
		{
			var configEntry = GetConfigurationEntry (configuration, true);
			var sizeEntry = configEntry.SizeReportEntries.FirstOrDefault (e => e.Profile == profile);
			if (sizeEntry == null) {
				sizeEntry = new SizeReportEntry (configEntry, profile);
				configEntry.SizeReportEntries.Add (sizeEntry);
			}
			var asmEntry = sizeEntry.Assemblies.FirstOrDefault (e => e.Name == assembly);
			if (asmEntry == null) {
				asmEntry = new AssemblySizeEntry (assembly, size, null);
				sizeEntry.Assemblies.Add (asmEntry);
			} else {
				asmEntry.Size = size;
			}
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
						xml.WriteEndElement ();

					}
					xml.WriteEndElement ();
				}
				xml.WriteEndElement ();
			}
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
			public ConfigurationEntry Configuration {
				get;
			}

			public string Profile {
				get;
			}

			public List<AssemblySizeEntry> Assemblies {
				get;
			}

			public SizeReportEntry (ConfigurationEntry configuration, string profile)
			{
				Configuration = configuration;
				Profile = profile;
				Assemblies = new List<AssemblySizeEntry> ();
			}
		}

		class AssemblySizeEntry
		{
			public string Name {
				get;
			}

			public int Size {
				get; set;
			}

			public string Tolerance {
				get;
			}

			public AssemblySizeEntry (string name, int size, string tolerance)
			{
				Name = name;
				Size = size;
				Tolerance = tolerance;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size} {Tolerance}]";
			}
		}

		internal bool CheckAndReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			if (!IsEnabled)
				return true;

			var detailed = new DetailedAssemblyEntry (assembly.Name.Name, size);
			_detailed_assembly_entries.Add (detailed);
			ReportDetailed (context, assembly, detailed);

			return CheckAssemblySize (context, assembly.Name.Name, size);
		}

		void ReportDetailed (OptimizerContext context, AssemblyDefinition assembly, DetailedAssemblyEntry entry)
		{
			foreach (var type in assembly.MainModule.Types) {
				ProcessType (context, entry, type);
			}

		}

		void ProcessType (OptimizerContext context, DetailedAssemblyEntry parent, TypeDefinition type)
		{
			if (!context.Annotations.IsMarked (type))
				return;

			if (!parent.Namespaces.TryGetValue (type.Namespace, out var ns)) {
				ns = new DetailedNamespaceEntry (type.Namespace);
				parent.Namespaces.Add (type.Namespace, ns);
			}

			if (!ns.Types.TryGetValue (type.Name, out var entry)) {
				entry = new DetailedTypeEntry (type.Name);
				ns.Types.Add (type.Name, entry);
			}


			foreach (var method in type.Methods)
				ProcessMethod (context, entry, method);

			foreach (var nested in type.NestedTypes)
				ProcessType (context, entry, nested);
		}

		void ProcessType (OptimizerContext context, DetailedTypeEntry parent, TypeDefinition type)
		{
			if (!context.Annotations.IsMarked (type))
				return;

			if (!parent.NestedTypes.TryGetValue (type.Name, out var entry)) {
				entry = new DetailedTypeEntry (type.Name);
				parent.NestedTypes.Add (type.Name, entry);
			}

			foreach (var method in type.Methods)
				ProcessMethod (context, entry, method);

			foreach (var nested in type.NestedTypes)
				ProcessType (context, entry, nested);
		}

		void ProcessMethod (OptimizerContext context, DetailedTypeEntry parent, MethodDefinition method)
		{
			if (!method.HasBody || !context.Annotations.IsMarked (method))
				return;

			var name = method.Name + CecilHelper.GetMethodSignature (method);
			if (parent.Methods.ContainsKey (name))
				return;

			if (Options.HasTypeEntry (method.DeclaringType, OptimizerOptions.TypeAction.Size))
				context.LogMessage (MessageImportance.Normal, $"SIZE: {method.FullName} {method.Body.CodeSize}");

			parent.Methods.Add (name, new DetailedMethodEntry (name, method.Body.CodeSize));
		}

		abstract class DetailedEntry
		{
			public string Name {
				get;
			}

			protected DetailedEntry (string name)
			{
				Name = name;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name}]";
			}
		}

		class DetailedAssemblyEntry : DetailedEntry
		{
			public int Size {
				get;
			}

			public Dictionary<string, DetailedNamespaceEntry> Namespaces {
				get;
			}

			public DetailedAssemblyEntry (string name, int size)
				: base (name)
			{
				Size = size;
				Namespaces = new Dictionary<string, DetailedNamespaceEntry> ();
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size}]";
			}
		}

		class DetailedNamespaceEntry : DetailedEntry
		{
			public Dictionary<string, DetailedTypeEntry> Types {
				get;
			}

			public DetailedNamespaceEntry (string name)
				: base (name)
			{
				Types = new Dictionary<string, DetailedTypeEntry> ();
			}
		}

		class DetailedTypeEntry : DetailedEntry
		{
			public Dictionary<string, DetailedMethodEntry> Methods {
				get;
			}

			public Dictionary<string, DetailedTypeEntry> NestedTypes {
				get;
			}

			public DetailedTypeEntry (string name)
				: base (name)
			{
				Methods = new Dictionary<string, DetailedMethodEntry> ();
				NestedTypes = new Dictionary<string, DetailedTypeEntry> ();
			}
		}

		class DetailedMethodEntry : DetailedEntry
		{
			public int Size {
				get;
			}

			public DetailedMethodEntry (string name, int size)
				: base (name)
			{
				Size = size;
			}
		}
	}
}
