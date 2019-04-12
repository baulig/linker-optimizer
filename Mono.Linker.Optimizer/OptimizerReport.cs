﻿//
// OptimizerReport.cs
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
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;

	public class OptimizerReport
	{
		public OptimizerOptions Options {
			get;
		}

		public ILogger Logger {
			get;
			private set;
		}

		public ReportMode Mode => Options.ReportMode;

		public bool IsEnabled (ReportMode mode) => (Mode & mode) != 0;

		List<ConfigurationEntry> _configuration_entries;
		readonly FailList _fail_list;
		AssemblyListEntry _root_entry;

		public OptimizerReport (OptimizerOptions options)
		{
			Options = options;

			_fail_list = new FailList ();
		}

		public void Initialize (OptimizerContext context)
		{
			Logger = context.Context.Logger;

			if (Options.ReportConfiguration == null && Options.ReportProfile == null) {
				_root_entry = new SizeReportEntry ();
			} else {
				_configuration_entries = new List<ConfigurationEntry> ();
				_root_entry = GetProfileEntry (Options.ReportConfiguration, Options.ReportProfile, false);

				if (_root_entry == null) {
					if (Options.CheckSize)
						LogWarning ($"Cannot find size entries for configuration `{Options.ReportConfiguration}`, profile `{Options.ReportProfile}`.");
					_root_entry = new SizeReportEntry ();
				}
			}
		}

		public void Read (XPathNavigator nav)
		{
			var name = OptionsReader.GetAttribute (nav, "configuration");
			var configuration = GetConfigurationEntry (name, true);

			OptionsReader.ProcessChildren (nav, "profile", child => OnProfileEntry (child, configuration));
		}

		public void MarkAsContainingConditionals (MethodDefinition method)
		{
			if (!IsEnabled (ReportMode.Actions))
				return;
			if (method.DeclaringType.DeclaringType != null)
				throw new OptimizerAssertionException ($"Conditionals in nested classes are not supported yet.");

			var entry = GetMethodEntry (_root_entry, method, true);
		}

		public void ReportFailListEntry (TypeDefinition type, OptimizerOptions.TypeEntry entry, string original, List<string> stack)
		{
			var fail = new FailListEntry (type.FullName) {
				Original = original
			};
			fail.TracerStack.AddRange (stack);
			_fail_list.FailListEntries.Add (fail);

			while (entry != null) {
				fail.EntryStack.Add (entry.ToString ());
				entry = entry.Parent;
			}
		}

		public void ReportFailListEntry (MethodDefinition method, OptimizerOptions.MethodEntry entry, List<string> stack)
		{
			var fail = new FailListEntry (method.FullName);
			fail.TracerStack.AddRange (stack);
			_fail_list.FailListEntries.Add (fail);

			if (entry != null) {
				fail.EntryStack.Add (entry.ToString ());

				var type = entry.Parent;
				while (type != null) {
					fail.EntryStack.Add (type.ToString ());
					type = type.Parent;
				}
			}
		}

		public bool CheckAndReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			ReportAssemblySize (context, assembly, size);

			return CheckAssemblySize (assembly.Name.Name, size);
		}

		public void WriteReport (string filename)
		{
			var settings = new XmlWriterSettings {
				Indent = true,
				OmitXmlDeclaration = false,
				NewLineHandling = NewLineHandling.None,
				ConformanceLevel = ConformanceLevel.Document,
				IndentChars = "\t",
				Encoding = Encoding.Default
			};

			using (var xml = XmlWriter.Create (filename, settings)) {
				xml.WriteStartDocument ();
				xml.WriteStartElement ("optimizer-report");

				Write (xml);

				xml.WriteEndElement ();
				xml.WriteEndDocument ();
			}
		}

		public void Write (XmlWriter xml)
		{
			var writer = new ReportWriter (xml, Mode);

			if (_configuration_entries != null && _configuration_entries.Count > 0) {
				foreach (var configuration in _configuration_entries)
					configuration.Visit (writer);
			} else {
				_root_entry.Visit (writer);
			}

			if (IsEnabled (ReportMode.Warnings))
				_fail_list.Visit (writer);
		}

		void LogMessage (string message)
		{
			Logger.LogMessage (MessageImportance.Normal, message);
		}

		void LogWarning (string message)
		{
			Logger.LogMessage (MessageImportance.High, message);
		}

		[Conditional ("DEBUG")]
		void LogDebug (string message)
		{
			Logger.LogMessage (MessageImportance.Low, message);
		}

		void OnProfileEntry (XPathNavigator nav, ConfigurationEntry configuration)
		{
			var profile = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<profile> requires `name` attribute.");

			var entry = new ProfileEntry (profile);
			configuration.ProfileEntries.Add (entry);

			OptionsReader.ProcessChildren (nav, "assembly", child => OnAssemblyEntry (child, entry));
		}

		void OnAssemblyEntry (XPathNavigator nav, ProfileEntry entry)
		{
			var name = OptionsReader.GetAttribute (nav, "name") ?? throw OptionsReader.ThrowError ("<assembly> requires `name` attribute.");
			var sizeAttr = OptionsReader.GetAttribute (nav, "size");
			if (sizeAttr == null || !int.TryParse (sizeAttr, out var size))
				throw OptionsReader.ThrowError ("<assembly> requires `size` attribute.");
			var toleranceAttr = OptionsReader.GetAttribute (nav, "tolerance");

			var assembly = new AssemblyEntry (name, size, toleranceAttr);
			entry.Assemblies.Add (assembly);

			OptionsReader.ProcessChildren (nav, "namespace", child => OnNamespaceEntry (child, assembly));
		}

		void OnNamespaceEntry (XPathNavigator nav, AssemblyEntry assembly)
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

		ConfigurationEntry GetConfigurationEntry (string configuration, bool add)
		{
			LazyInitializer.EnsureInitialized (ref _configuration_entries);
			var entry = _configuration_entries.FirstOrDefault (e => e.Configuration == configuration);
			if (add && entry == null) {
				entry = new ConfigurationEntry (configuration);
				_configuration_entries.Add (entry);
			}
			return entry;
		}

		ProfileEntry GetProfileEntry (string configuration, string profile, bool add)
		{
			var configEntry = GetConfigurationEntry (configuration, add);
			if (configEntry == null)
				return null;
			var profileEntry = configEntry.ProfileEntries.FirstOrDefault (e => e.Profile == profile);
			if (add && profileEntry == null) {
				profileEntry = new ProfileEntry (profile);
				configEntry.ProfileEntries.Add (profileEntry);
			}
			return profileEntry;
		}

		AssemblyEntry GetAssemblyEntry (AssemblyListEntry root, string name, bool add)
		{
			var assembly = root.Assemblies.FirstOrDefault (e => e.Name == name);
			if (add && assembly == null) {
				assembly = new AssemblyEntry (name, 0, null);
				root.Assemblies.Add (assembly);
			}
			return assembly;
		}

		TypeEntry GetTypeEntry (AssemblyListEntry root, TypeDefinition type, bool add)
		{
			if (type.DeclaringType != null)
				throw DebugHelpers.AssertFail ("Nested types are not supported yet.");

			var assembly = GetAssemblyEntry (root, type.Module.Assembly.Name.Name, add);
			if (assembly == null)
				return null;

			var ns = assembly.GetNamespace (type.Name, add);
			if (ns == null)
				return null;

			return ns.GetType (type, add);
		}

		MethodEntry GetMethodEntry (AssemblyListEntry root, MethodDefinition method, bool add)
		{
			var type = GetTypeEntry (root, method.DeclaringType, add);
			if (type == null)
				return null;

			type.AddMethod (method);
			return null;
		}

		bool CheckAssemblySize (string assembly, int size)
		{
			if (!Options.CheckSize)
				return true;

			var asmEntry = GetAssemblyEntry (_root_entry, assembly, false);
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

			LogDebug ($"Size check: {asmEntry.Name}, actual={size}, expected={asmEntry.Size} (tolerance {toleranceValue})");

			if (size < asmEntry.Size - tolerance) {
				LogWarning ($"Assembly `{asmEntry.Name}` size below minimum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}
			if (size > asmEntry.Size + tolerance) {
				LogWarning ($"Assembly `{asmEntry.Name}` size above maximum: expected {asmEntry.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}

			return true;
		}

		void ReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			var asmEntry = GetAssemblyEntry (_root_entry, assembly.Name.Name, true);
			asmEntry.SetSize (size);

			ReportDetailed (context, assembly, asmEntry);
		}

		void ReportDetailed (OptimizerContext context, AssemblyDefinition assembly, AssemblyEntry entry)
		{
			foreach (var type in assembly.MainModule.Types) {
				ProcessType (context, entry, type);
			}
		}

		void CompareSize (XmlWriter xml, AssemblyEntry assembly)
		{
			xml.WriteStartElement ("removed-types");
			foreach (var ns in assembly.GetNamespaces ()) {
				if (string.IsNullOrEmpty (ns.Name))
					continue;
				CompareSize (xml, ns);
			}
			xml.WriteEndElement ();
		}

		void CompareSize (XmlWriter xml, NamespaceEntry entry)
		{
			var types = entry.GetTypes ();
			if (entry.Marked && types.All (t => t.Marked))
				return;

			xml.WriteStartElement ("namespace");
			xml.WriteAttributeString ("name", entry.Name);
			if (!entry.Marked)
				xml.WriteAttributeString ("action", "fail");

			foreach (var type in types) {
				if (type.Marked)
					continue;
				xml.WriteStartElement ("type");
				xml.WriteAttributeString ("name", type.Name);
				xml.WriteAttributeString ("action", "fail");
				xml.WriteEndElement ();
			}

			xml.WriteEndElement ();
		}

		void ProcessType (OptimizerContext context, AssemblyEntry parent, TypeDefinition type)
		{
			if (type.Name == "<Module>")
				return;
			if (!context.Annotations.IsMarked (type))
				throw DebugHelpers.AssertFail ($"Type `{type}` is not marked.");
			if (type.FullName.StartsWith ("<PrivateImplementationDetails>", StringComparison.Ordinal))
				return;

			var ns = parent.GetNamespace (type.Namespace);
			ProcessType (context, ns, type);
		}

		void ProcessType (OptimizerContext context, AbstractTypeEntry parent, TypeDefinition type)
		{
			if (!context.Annotations.IsMarked (type))
				throw DebugHelpers.AssertFail ($"Type `{type}` is not marked.");

			parent.Marked = true;

			var entry = parent.GetType (type, true);
			entry.Marked = true;

			foreach (var method in type.Methods)
				ProcessMethod (context, entry, method);

			foreach (var nested in type.NestedTypes)
				ProcessType (context, entry, nested);
		}

		void ProcessMethod (OptimizerContext context, TypeEntry parent, MethodDefinition method)
		{
			if (!method.HasBody)
				return;
			if (!context.Annotations.IsMarked (method))
				throw DebugHelpers.AssertFail ($"Method `{method}` is not marked.");

			if (!parent.AddMethod (method))
				return;

			if (Options.HasTypeEntry (method.DeclaringType, OptimizerOptions.TypeAction.Size))
				LogMessage ($"SIZE: {method.FullName} {method.Body.CodeSize}");
		}

		abstract class Visitor
		{
			public abstract void Visit (SizeReportEntry entry);

			public abstract void Visit (ConfigurationEntry entry);

			public abstract void Visit (ProfileEntry entry);

			public abstract void Visit (AssemblyEntry entry);

			public abstract void Visit (NamespaceEntry entry);

			public abstract void Visit (TypeEntry entry);

			public abstract void Visit (MethodEntry entry);

			public abstract void Visit (FailList entry);

			public abstract void Visit (FailListEntry entry);
		}

		class ReportWriter : Visitor
		{
			readonly XmlWriter xml;
			readonly ReportMode mode;

			public ReportWriter (XmlWriter xml, ReportMode mode)
			{
				this.xml = xml;
				this.mode = mode;
			}

			void Write (AbstractReportEntry entry)
			{
				xml.WriteStartElement (entry.ElementName);
				entry.WriteElement (xml);
				entry.VisitChildren (this);
				xml.WriteEndElement ();
			}

			public override void Visit (SizeReportEntry entry)
			{
				Write (entry);
			}

			public override void Visit (ConfigurationEntry entry)
			{
				Write (entry);
			}

			public override void Visit (ProfileEntry entry)
			{
				Write (entry);
			}

			public override void Visit (AssemblyEntry entry)
			{
				xml.WriteStartElement (entry.ElementName);
				entry.WriteElement (xml);

				if ((mode & ReportMode.Detailed) != 0)
					entry.VisitChildren (this);

				xml.WriteEndElement ();
			}

			public override void Visit (NamespaceEntry entry)
			{
				Write (entry);
			}

			public override void Visit (TypeEntry entry)
			{
				Write (entry);
			}

			public override void Visit (MethodEntry entry)
			{
				Write (entry);
			}

			public override void Visit (FailList entry)
			{
				Write (entry);
			}

			public override void Visit (FailListEntry entry)
			{
				Write (entry);
			}
		}

		abstract class AbstractReportEntry
		{
			public abstract string ElementName {
				get;
			}

			public abstract void WriteElement (XmlWriter xml);

			public abstract void Visit (Visitor visitor);

			public abstract void VisitChildren (Visitor visitor);
		}

		class ConfigurationEntry : AbstractReportEntry
		{
			public string Configuration {
				get;
			}

			public List<ProfileEntry> ProfileEntries {
				get;
			}

			public override string ElementName => "configuration";

			public ConfigurationEntry (string configuration)
			{
				Configuration = configuration;
				ProfileEntries = new List<ProfileEntry> ();
			}

			public override void WriteElement (XmlWriter xml)
			{
				if (!string.IsNullOrEmpty (Configuration))
					xml.WriteAttributeString ("name", Configuration);
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (Visitor visitor)
			{
				ProfileEntries.ForEach (profile => profile.Visit (visitor));
			}
		}

		abstract class AssemblyListEntry : AbstractReportEntry
		{
			public List<AssemblyEntry> Assemblies { get; } = new List<AssemblyEntry> ();

			public sealed override void VisitChildren (Visitor visitor)
			{
				Assemblies.ForEach (assembly => assembly.Visit (visitor));
			}
		}

		class SizeReportEntry : AssemblyListEntry
		{
			public override string ElementName => "size-report";

			public override void WriteElement (XmlWriter xml)
			{
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}
		}

		class ProfileEntry : AssemblyListEntry
		{
			public string Profile {
				get;
			}

			public override string ElementName => "profile";

			public ProfileEntry (string profile)
			{
				Profile = profile;
			}

			public override void WriteElement (XmlWriter xml)
			{
				if (!string.IsNullOrEmpty (Profile))
					xml.WriteAttributeString ("name", Profile);
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}
		}

		abstract class ReportEntry : AbstractReportEntry, IComparable<ReportEntry>
		{
			public ReportEntry Parent {
				get;
			}

			public string Name {
				get;
			}

			public int Size {
				get;
				protected set;
			}

			public bool Marked {
				get; set;
			}

			void AddSize (int size)
			{
				Size += size;
				if (Parent is AbstractTypeEntry parent)
					parent.AddSize (size);
			}

			protected ReportEntry (ReportEntry parent, string name, int size)
			{
				Parent = parent;
				Name = name;

				AddSize (size);
			}

			public int CompareTo (ReportEntry obj)
			{
				return Size.CompareTo (obj.Size);
			}

			public override void WriteElement (XmlWriter xml)
			{
				xml.WriteAttributeString ("name", Name);
				if (Size != 0)
					xml.WriteAttributeString ("size", Size.ToString ());
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size}]";
			}
		}

		class AssemblyEntry : ReportEntry
		{
			public string Tolerance {
				get;
			}

			public override string ElementName => "assembly";

			internal void SetSize (int size)
			{
				Size = size;
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

			public List<NamespaceEntry> GetNamespaces ()
			{
				var list = new List<NamespaceEntry> ();
				if (namespaces != null) {
					foreach (var ns in namespaces.Values)
						list.Add (ns);
					list.Sort ();
				}
				return list;
			}

			public override void WriteElement (XmlWriter xml)
			{
				if (!string.IsNullOrEmpty (Tolerance))
					xml.WriteAttributeString ("tolerance", Tolerance);
				base.WriteElement (xml);
			}

			public AssemblyEntry (string name, int size, string tolerance)
				: base (null, name, size)
			{
				Tolerance = tolerance;
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (Visitor visitor)
			{
				GetNamespaces ().ForEach (ns => ns.Visit (visitor));
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name}: {Name} {Size} {Tolerance}]";
			}
		}

		abstract class AbstractTypeEntry : ReportEntry
		{
			protected AbstractTypeEntry (ReportEntry parent, string name)
				: base (parent, name, 0)
			{
			}

			Dictionary<string, TypeEntry> types;

			public bool HasTypes => types != null && types.Count > 0;

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

			public List<TypeEntry> GetTypes ()
			{
				var list = new List<TypeEntry> ();
				if (types != null) {
					foreach (var type in types.Values)
						list.Add (type);
					list.Sort ();
				}
				return list;
			}

			public override void VisitChildren (Visitor visitor)
			{
				GetTypes ().ForEach (ns => ns.Visit (visitor));
			}
		}

		class NamespaceEntry : AbstractTypeEntry
		{
			public override string ElementName => "namespace";

			public NamespaceEntry (AssemblyEntry parent, string name)
				: base (parent, name)
			{
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}
		}

		class TypeEntry : AbstractTypeEntry
		{
			public override string ElementName => "type";

			public bool AddMethod (MethodDefinition method)
			{
				LazyInitializer.EnsureInitialized (ref methods);

				var name = method.Name + CecilHelper.GetMethodSignature (method);
				if (methods.ContainsKey (name))
					return false;

				methods.Add (name, new MethodEntry (this, name, method.Body.CodeSize));
				return true;
			}

			public MethodEntry GetMethod (MethodDefinition method, bool add)
			{
				LazyInitializer.EnsureInitialized (ref methods);

				var name = method.Name + CecilHelper.GetMethodSignature (method);
				if (methods.TryGetValue (name, out var entry))
					return entry;
				if (!add)
					return null;
				entry = new MethodEntry (this, name, method.Body.CodeSize);
				methods.Add (name, entry);
				return entry;
			}

			public List<MethodEntry> GetMethods ()
			{
				var list = new List<MethodEntry> ();
				if (methods != null) {
					foreach (var method in methods.Values)
						list.Add (method);
					list.Sort ();
				}
				return list;
			}

			Dictionary<string, MethodEntry> methods;

			public string FullName {
				get;
			}

			public TypeEntry (ReportEntry parent, string name, string fullName)
				: base (parent, name)
			{
				FullName = fullName;
			}

			public override void WriteElement (XmlWriter xml)
			{
				base.WriteElement (xml);
				if (!string.IsNullOrEmpty (FullName))
					xml.WriteAttributeString ("full-name", FullName);
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (Visitor visitor)
			{
				base.VisitChildren (visitor);
				GetMethods ().ForEach (method => method.Visit (visitor));
			}
		}

		class MethodEntry : ReportEntry
		{
			public override string ElementName => "method";

			public MethodEntry (TypeEntry parent, string name, int size)
				: base (parent, name, size)
			{
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (Visitor visitor)
			{
			}
		}

		class FailList : AbstractReportEntry
		{
			public List<FailListEntry> FailListEntries { get; } = new List<FailListEntry> ();

			public override string ElementName => "fail-list";

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (Visitor visitor)
			{
				FailListEntries.ForEach (entry => entry.Visit (visitor));
			}

			public override void WriteElement (XmlWriter xml)
			{
			}
		}

		class FailListEntry : AbstractReportEntry
		{
			public readonly string Name;
			public string Original;
			public readonly List<string> EntryStack;
			public readonly List<string> TracerStack;

			public override string ElementName => "fail";

			public FailListEntry (string name)
			{
				Name = name;
				EntryStack = new List<string> ();
				TracerStack = new List<string> ();
			}

			public override void WriteElement (XmlWriter xml)
			{
				xml.WriteAttributeString ("name", Name);
				if (Original != null)
					xml.WriteAttributeString ("full-name", Original);
				foreach (var entry in EntryStack) {
					xml.WriteStartElement ("entry");
					xml.WriteAttributeString ("name", entry);
					xml.WriteEndElement ();
				}
				foreach (var entry in TracerStack) {
					xml.WriteStartElement ("stack");
					xml.WriteAttributeString ("name", entry);
					xml.WriteEndElement ();
				}
			}

			public override void Visit (Visitor visitor)
			{
				visitor.Visit (this);
			}

			public override void VisitChildren (Visitor visitor)
			{
			}
		}
	}
}
