//
// MartinOptions.cs
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

namespace Mono.Linker.Conditionals
{
	public class MartinOptions
	{
		public bool ScanAllModules {
			get; set;
		}

		public bool AnalyzeAll {
			get; set;
		}

		public bool Preprocess {
			get; set;
		}

		public bool NoConditionalRedefinition {
			get; set;
		}

		public bool IgnoreResolutionErrors {
			get; set;
		}

		public bool ReportSize {
			get; set;
		}

		public bool AutoDebugMain {
			get; set;
		}

		readonly List<TypeEntry> _type_actions;
		readonly List<MethodEntry> _method_actions;
		readonly Dictionary<MonoLinkerFeature, bool> _enabled_features;

		public MartinOptions ()
		{
			AutoDebugMain = true;
			NoConditionalRedefinition = true;
			_type_actions = new List<TypeEntry> ();
			_method_actions = new List<MethodEntry> ();
			_enabled_features = new Dictionary<MonoLinkerFeature, bool> {
				[MonoLinkerFeature.Security] = false,
				[MonoLinkerFeature.Martin] = false
			};
		}

		public bool IsFeatureEnabled (int index)
		{
			var feature = FeatureByIndex (index);
			if (_enabled_features.TryGetValue (feature, out var value))
				return value;
			return false;
		}

		public void SetFeatureEnabled (int index, bool enabled)
		{
			var feature = FeatureByIndex (index);
			_enabled_features [feature] = enabled;
		}

		public void SetFeatureEnabled (string name, bool enabled)
		{
			var feature = FeatureByName (name);
			_enabled_features [feature] = enabled;
		}

		static MonoLinkerFeature FeatureByName (string name)
		{
			switch (name.ToLowerInvariant ()) {
			case "sre":
				return MonoLinkerFeature.ReflectionEmit;
			case "remoting":
				return MonoLinkerFeature.Remoting;
			case "globalization":
				return MonoLinkerFeature.Globalization;
			case "encoding":
				return MonoLinkerFeature.Encoding;
			case "security":
				return MonoLinkerFeature.Security;
			case "martin":
				return MonoLinkerFeature.Martin;
			default:
				throw new NotSupportedException ($"Unknown linker feature `{name}`.");
			}
		}

		static MonoLinkerFeature FeatureByIndex (int index)
		{
			if (index < 0 || index > (int)MonoLinkerFeature.Martin)
				throw new NotSupportedException ($"Unknown feature {index}.");
			return (MonoLinkerFeature)index;
		}

		bool DontDebugThis (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return DontDebugThis (type.DeclaringType);

			switch (type.FullName) {
			case "Martin.LinkerTest.TestHelpers":
			case "Martin.LinkerTest.AssertionException":
				return true;
			default:
				return false;
			}
		}

		public bool EnableDebugging (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return EnableDebugging (type.DeclaringType);

			if (DontDebugThis (type))
				return false;

			if (AutoDebugMain) {
				if (type.Namespace == "Martin.LinkerTest")
					return true;
				if (type.Module.Assembly.Name.Name.ToLowerInvariant ().Contains ("martin"))
					return true;
			}

			return _type_actions.Any (t => t.Matches (type, TypeAction.Debug));
		}

		public bool EnableDebugging (MethodDefinition method)
		{
			if (DontDebugThis (method.DeclaringType))
				return false;
			if (EnableDebugging (method.DeclaringType))
				return true;

			if (AutoDebugMain) {
				if (method.Name == "Main")
					return true;
				if (method.FullName.Contains ("Martin"))
					return true;
			}

			return _method_actions.Any (e => e.Matches (method, MethodAction.Debug));
		}

		public bool FailOnMethod (MethodDefinition method)
		{
			if (HasTypeEntry (method.DeclaringType, TypeAction.Fail))
				return true;

			return _method_actions.Any (e => e.Matches (method, MethodAction.Fail));
		}

		public void CheckFailList (MartinContext context, TypeDefinition type, string original = null)
		{
			if (type.DeclaringType != null) {
				CheckFailList (context, type.DeclaringType, original ?? type.FullName);
				return;
			}

			var fail = _type_actions.FirstOrDefault (e => e.Matches (type, TypeAction.Fail));
			if (fail == null)
				return;

			var original_message = original != null ? $" while parsing `{original}`" : string.Empty;
			var message = $"Found type `{type.FullName}`{original_message}, which matches fail-list entry `{fail}.";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message);
			context.Context.Tracer.Dump ();
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			throw new NotSupportedException (message);
		}

		public void CheckFailList (MartinContext context, MethodDefinition method)
		{
			CheckFailList (context, method.DeclaringType, method.FullName);

			var fail = _method_actions.FirstOrDefault (e => e.Matches (method, MethodAction.Fail));
			if (fail == null)
				return;

			var message = $"Found method `{method.FullName}`, which matches fail-list entry `{fail}.";
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			context.LogMessage (MessageImportance.High, message);
			context.Context.Tracer.Dump ();
			context.LogMessage (MessageImportance.High, Environment.NewLine);
			throw new NotSupportedException (message);
		}

		public void AddTypeEntry (string name, MatchKind match, TypeAction action)
		{
			_type_actions.Add (new TypeEntry (name, match, action));
		}

		public void AddMethodEntry (string name, MatchKind match, MethodAction action)
		{
			_method_actions.Add (new MethodEntry (name, match, action));
		}

		public bool HasTypeEntry (TypeDefinition type, TypeAction action)
		{
			if (type.DeclaringType != null)
				return HasTypeEntry (type.DeclaringType, action);
			return _type_actions.Any (e => e.Matches (type, action));
		}

		public void ProcessTypeEntries (TypeDefinition type, Action<TypeAction> action)
		{
			if (type.DeclaringType != null) {
				ProcessTypeEntries (type.DeclaringType, action);
				return;
			}
			foreach (var entry in _type_actions) {
				if (entry.Matches (type))
					action (entry.Action);
			}
		}

		public void ProcessTypeEntries (TypeDefinition type, TypeAction filter, Action action)
		{
			if (type.DeclaringType != null) {
				ProcessTypeEntries (type.DeclaringType, filter, action);
				return;
			}
			foreach (var entry in _type_actions) {
				if (entry.Action == filter && entry.Matches (type))
					action ();
			}
		}

		public enum TypeAction
		{
			None,
			Debug,
			Fail,
			Mark,
			Size
		}

		public enum MethodAction
		{
			None,
			Debug,
			Fail,
			Mark
		}

		public enum MatchKind
		{
			Name,
			FullName,
			Substring
		}

		public class TypeEntry
		{
			public string Name {
				get;
			}

			public MatchKind Match {
				get;
			}

			public TypeAction Action {
				get;
			}

			public bool Matches (TypeDefinition type)
			{
				switch (Match) {
				case MatchKind.FullName:
					return type.FullName == Name;
				case MatchKind.Substring:
					return type.FullName.Contains (Name);
				default:
					return type.Name == Name;
				}
			}

			public bool Matches (TypeDefinition type, TypeAction action) => Action == action && Matches (type);

			public TypeEntry (string name, MatchKind match, TypeAction action)
			{
				Name = name;
				Match = match;
				Action = action;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name} {Name}:{Match}:{Action}]";
			}
		}

		public class MethodEntry
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

			public bool Matches (MethodDefinition method)
			{
				switch (Match) {
				case MatchKind.FullName:
					return method.FullName == Name;
				case MatchKind.Substring:
					return method.FullName.Contains (Name);
				default:
					return method.Name == Name;
				}
			}

			public bool Matches (MethodDefinition method, MethodAction action) => Action == action && Matches (method);

			public MethodEntry (string name, MatchKind match, MethodAction action)
			{
				Name = name;
				Match = match;
				Action = action;
			}

			public override string ToString ()
			{
				return $"[{GetType ().Name} {Name}:{Match}:{Action}]";
			}
		}
	}
}
