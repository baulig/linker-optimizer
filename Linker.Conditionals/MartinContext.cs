//
// MartinContext.cs
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
using Mono.Linker.Steps;

namespace Mono.Linker.Conditionals
{
	public class MartinContext
	{
		public LinkContext Context {
			get;
		}

		public bool NoConditionalRedefinition {
			get; set;
		}

		public AnnotationStore Annotations => Context.Annotations;

		MartinContext (LinkContext context)
		{
			Context = context;
			NoConditionalRedefinition = true;
		}

		public void LogMessage (MessageImportance importance, string message)
		{
			Context.Logger.LogMessage (importance, message);
		}

		public static void InitializePipeline (LinkContext context)
		{
			context.Logger.LogMessage (MessageImportance.Normal, "Enabling Martin's Playground");

			context.MartinContext = new MartinContext (context);

			context.Pipeline.AddStepAfter (typeof (TypeMapStep), new InitializeStep ());
			context.Pipeline.AddStepBefore (typeof (MarkStep), new MartinTestStep ());
			context.Pipeline.ReplaceStep (typeof (MarkStep), new ConditionalMarkStep ());
		}

		const string LinkerSupportType = "System.Runtime.CompilerServices.MonoLinkerSupport";
		const string IsTypeAvailableName = "System.Boolean " + LinkerSupportType + "::IsTypeAvailable()";
		const string IsTypeNameAvailableName = "System.Boolean " + LinkerSupportType + "::IsTypeAvailable(System.String)";

		void Initialize ()
		{
			LogMessage (MessageImportance.High, "Initializing Martin's Playground");

			MonoLinkerSupportType = Context.GetType (LinkerSupportType);
			if (MonoLinkerSupportType == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}`.");

			IsWeakInstanceOfMethod = MonoLinkerSupportType.Methods.First (m => m.Name == "IsWeakInstanceOf");
			if (IsWeakInstanceOfMethod == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}.IsWeakInstanceOf<T>()`.");

			AsWeakInstanceOfMethod = MonoLinkerSupportType.Methods.First (m => m.Name == "AsWeakInstanceOf");
			if (AsWeakInstanceOfMethod == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}.AsWeakInstanceOf<T>()`.");

			IsFeatureSupportedMethod = MonoLinkerSupportType.Methods.First (m => m.Name == "IsFeatureSupported");
			if (IsFeatureSupportedMethod == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}.IsFeatureSupported()`.");

			IsTypeAvailableMethod = MonoLinkerSupportType.Methods.First (m => m.FullName == IsTypeAvailableName);
			if (IsTypeAvailableMethod == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}.IsTypeAvailable()`.");

			IsTypeNameAvailableMethod = MonoLinkerSupportType.Methods.First (m => m.FullName == IsTypeNameAvailableName);
			if (IsTypeNameAvailableMethod == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}.IsTypeAvailable(string)`.");
		}

		public TypeDefinition MonoLinkerSupportType {
			get;
			private set;
		}

		public MethodDefinition IsWeakInstanceOfMethod {
			get;
			private set;
		}

		public MethodDefinition IsTypeAvailableMethod {
			get;
			private set;
		}

		public MethodDefinition IsTypeNameAvailableMethod {
			get;
			private set;
		}

		public MethodDefinition AsWeakInstanceOfMethod {
			get;
			private set;
		}

		public MethodDefinition IsFeatureSupportedMethod {
			get;
			private set;
		}

		internal bool EnableDebugging (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return EnableDebugging (type.DeclaringType);
			switch (type.Namespace) {
			case "Martin.LinkerTest":
				return true;
			default:
				return type.Module.Assembly.Name.Name.ToLowerInvariant ().Contains ("martin");
			}
		}

		public bool IsEnabled (MethodDefinition method)
		{
			return EnableDebugging (method.DeclaringType);
		}

		internal int GetDebugLevel (MethodDefinition method)
		{
			return EnableDebugging (method.DeclaringType) ? 5 : 0;
		}

		readonly Dictionary<MonoLinkerFeature, bool> enabled_features = new Dictionary<MonoLinkerFeature, bool> ();
		readonly HashSet<TypeDefinition> conditional_types = new HashSet<TypeDefinition> ();

		static MonoLinkerFeature FeatureByName (string name)
		{
			switch (name.ToLowerInvariant ()) {
			case "sre":
				return MonoLinkerFeature.ReflectionEmit;
			case "remoting":
				return MonoLinkerFeature.Remoting;
			case "globalization":
				return MonoLinkerFeature.Globalization;
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

		public bool IsFeatureEnabled (int index)
		{
			var feature = FeatureByIndex (index);
			if (enabled_features.TryGetValue (feature, out var value))
				return value;
			return false;
		}

		public void SetFeatureEnabled (int index, bool enabled)
		{
			var feature = FeatureByIndex (index);
			enabled_features [feature] = enabled;
		}

		public void SetFeatureEnabled (string name, bool enabled)
		{
			var feature = FeatureByName (name);
			enabled_features [feature] = enabled;
		}

		public bool IsConditionalTypeMarked (TypeDefinition type)
		{
			return conditional_types.Contains (type);
		}

		public void MarkConditionalType (TypeDefinition type)
		{
			conditional_types.Add (type);
		}

		internal void AttemptingToRedefineConditional (TypeDefinition type)
		{
			var message = $"Attempting to mark type `{type}` after it's already been used in a conditional!";
			LogMessage (MessageImportance.High, message);
			if (NoConditionalRedefinition)
				throw new NotSupportedException (message);
		}

		class InitializeStep : BaseStep
		{
			protected override void Process ()
			{
				Context.MartinContext.Initialize ();
				base.Process ();
			}
		}
	}
}
