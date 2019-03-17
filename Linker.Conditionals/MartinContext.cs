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

		public AnnotationStore Annotations => Context.Annotations;

		MartinContext (LinkContext context)
		{
			Context = context;
		}

		public void LogMessage (string message)
		{
			LogMessage (MessageImportance.Normal, message);
		}

		public void LogDebug (string message)
		{
			LogMessage (MessageImportance.Low, message);
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

		void Initialize ()
		{
			LogMessage ("Initializing Martin's Playground");

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

			MarkFeatureMethod = MonoLinkerSupportType.Methods.First (m => m.Name == "MarkFeature");
			if (MarkFeatureMethod == null)
				throw new NotSupportedException ($"Cannot find `{LinkerSupportType}.MarkFeature()`.");
		}

		public TypeDefinition MonoLinkerSupportType {
			get;
			private set;
		}

		public MethodDefinition IsWeakInstanceOfMethod {
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

		public MethodDefinition MarkFeatureMethod {
			get;
			private set;
		}

		public bool IsEnabled (TypeDefinition type)
		{
			if (type.DeclaringType != null)
				return IsEnabled (type.DeclaringType);
			switch (type.Namespace) {
			case "Martin.LinkerTest":
				return true;
			case "Martin.LinkerSupport":
				return false;
			default:
				return type.Module.Assembly.Name.Name.ToLowerInvariant ().Contains ("martin");
			}
		}

		public bool IsEnabled (MethodDefinition method)
		{
			return IsEnabled (method.DeclaringType);
		}

		readonly Dictionary<int, bool> enabledFeatures = new Dictionary<int, bool> ();

		public bool IsFeatureEnabled (int feature)
		{
			if (enabledFeatures.TryGetValue (feature, out var value))
				return value;
			return false;
		}

		public void SetFeatureEnabled (int feature, bool enabled)
		{
			enabledFeatures [feature] = enabled;
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
