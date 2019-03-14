﻿//
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
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Conditionals
{
	public class MartinContext
	{
		public LinkContext Context {
			get;
		}

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

		void Initialize ()
		{
			LogMessage ("Initializing Martin's Playground");

			var support = Context.GetType ("Martin.LinkerSupport.MonoLinkerSupport");
			if (support == null)
				throw new NotSupportedException ("Cannot find `Martin.LinkerSupport.MonoLinkerSupport`.");
		}

		public bool IsEnabled (TypeDefinition type)
		{
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
