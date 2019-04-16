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
using Mono.Cecil;

namespace Mono.Linker.Optimizer.Configuration
{
	using BasicBlocks;

	public class OptimizerReport : Node
	{
		public SizeReport SizeReport { get; } = new SizeReport ();

		public ActionList ActionList { get; } = new ActionList ();

		public FailList FailList { get; } = new FailList ();

		public OptimizerOptions Options {
			get;
		}

		public ReportMode Mode => Options.ReportMode;

		public bool IsEnabled (ReportMode mode) => (Mode & mode) != 0;

		public SizeCheck SizeCheck => Options.OptimizerConfiguration.SizeCheck;

		public OptimizerReport (OptimizerOptions options)
		{
			Options = options;
		}

		public void MarkAsContainingConditionals (MethodDefinition method)
		{
			if (IsEnabled (ReportMode.Actions))
				ActionList.GetMethod (method, true, MethodAction.Scan);
		}

		public void MarkAsConstantMethod (MethodDefinition method, ConstantValue value)
		{
			if (!IsEnabled (ReportMode.Actions))
				return;
			switch (value) {
			case ConstantValue.False:
				ActionList.GetMethod (method, true, MethodAction.ReturnFalse);
				break;
			case ConstantValue.True:
				ActionList.GetMethod (method, true, MethodAction.ReturnTrue);
				break;
			case ConstantValue.Null:
				ActionList.GetMethod (method, true, MethodAction.ReturnNull);
				break;
			case ConstantValue.Throw:
				ActionList.GetMethod (method, true, MethodAction.Throw);
				break;
			default:
				throw DebugHelpers.AssertFail ($"Invalid constant value: `{value}`.");
			}
		}

		public void RemovedDeadBlocks (MethodDefinition method)
		{
			if (IsEnabled (ReportMode.DeadCode))
				ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedDeadBlocks;
		}

		public void RemovedDeadExceptionBlocks (MethodDefinition method)
		{
			if (IsEnabled (ReportMode.DeadCode))
				ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedExceptionBlocks;
		}

		public void RemovedDeadJumps (MethodDefinition method)
		{
			if (IsEnabled (ReportMode.DeadCode))
				ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedDeadJumps;
		}

		public void RemovedDeadConstantJumps (MethodDefinition method)
		{
			if (IsEnabled (ReportMode.DeadCode))
				ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedConstantJumps;
		}

		public void RemovedDeadVariables (MethodDefinition method)
		{
			if (IsEnabled (ReportMode.DeadCode))
				ActionList.GetMethod (method).DeadCodeMode |= DeadCodeMode.RemovedDeadVariables;
		}

		public bool CheckAndReportAssemblySize (OptimizerContext context, AssemblyDefinition assembly, int size)
		{
			if (!Options.CheckSize)
				return true;

			var profile = SizeCheck.GetProfile (Options.ReportConfiguration, Options.ReportProfile, false);
			if (profile == null) {
				context.LogMessage (MessageImportance.High, $"Cannot find size check profile for configuration `{((Options.ReportConfiguration ?? "<null>"))}`, profile `{((Options.ReportProfile ?? "<null>"))}`.");
				return true;
			}

			var configEntry = profile.Assemblies.GetAssembly (assembly, true);
			var sucess = configEntry.Size == null || CheckAssemblySize (context, configEntry, size);

			var reportEntry = SizeReport.Assemblies.GetAssembly (assembly, true);
			reportEntry.Size = size;

			if (IsEnabled (ReportMode.Detailed)) {
				GenerateDetailedReport (assembly, reportEntry);
				CleanupAndPurgeSizeReport (reportEntry);
			}

			return sucess;
		}

		bool CheckAssemblySize (OptimizerContext context, Assembly assembly, int size)
		{
			int tolerance;
			string toleranceValue = assembly.Tolerance ?? Options.SizeCheckTolerance ?? "0.05%";

			if (toleranceValue.EndsWith ("%", StringComparison.Ordinal)) {
				var percent = float.Parse (toleranceValue.Substring (0, toleranceValue.Length - 1));
				tolerance = (int)(assembly.Size * percent / 100.0f);
			} else {
				tolerance = int.Parse (toleranceValue);
			}

			context.LogDebug ($"Size check: {assembly.Name}, actual={size}, expected={assembly.Size} (tolerance {toleranceValue})");

			if (size < assembly.Size - tolerance) {
				context.LogWarning ($"Assembly `{assembly.Name}` size below minimum: expected {assembly.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}
			if (size > assembly.Size + tolerance) {
				context.LogWarning ($"Assembly `{assembly.Name}` size above maximum: expected {assembly.Size} (tolerance {toleranceValue}), got {size}.");
				return false;
			}

			return true;
		}

		void GenerateDetailedReport (AssemblyDefinition assembly, Assembly entry)
		{
			foreach (var type in assembly.MainModule.Types) {
				if (type.Name == "<Module>" || string.IsNullOrEmpty (type.Namespace))
					continue;
				var ns = entry.Namespaces.GetNamespace (type.Namespace, true);
				var typeEntry = ns.Types.GetType (ns, type, true);
				GenerateDetailedReport (type, typeEntry);
				ns.Size = (ns.Size ?? 0) + typeEntry.Size;
			}
		}

		void GenerateDetailedReport (TypeDefinition type, Type entry)
		{
			int size = 0;
			foreach (var method in type.Methods) {
				if (!method.HasBody)
					continue;
				var methodEntry = entry.Methods.GetChild (m => m.Matches (method), () => new Method (entry, method));
				methodEntry.Size = method.Body.CodeSize;
				size += methodEntry.Size.Value;
			}
			entry.Size = size;
		}

		void CleanupAndPurgeSizeReport (Assembly assembly)
		{
			foreach (var type in assembly.Namespaces.Children)
				CleanupAndPurgeSizeReport (type);
		}

		bool CleanupAndPurgeSizeReport (Type type)
		{
			var marked = false;
			if (!type.Types.IsEmpty) {
				foreach (var nested in type.Types.Children)
					CleanupAndPurgeSizeReport (nested);
			}

			if (!type.Types.IsEmpty)
				;

			if (type.Methods.IsEmpty)
				return marked;

			CleanupSizeList (type.Methods);

			return marked;
		}

		void CleanupSizeList (NodeList<Method> list)
		{
			if (list == null || list.IsEmpty)
				return;

			for (int i = 0; i < list.Count; i++) {
				if (list[i].Size != null || list[i].Action != null)
					continue;
				list.Children.RemoveAt (i--);
			}

			list.Children.Sort (Compare);
		}

		static int Compare (Method x, Method y)
		{
			return y.Size.Value.CompareTo (x.Size.Value);
		}

		public override void Visit (IVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void VisitChildren (IVisitor visitor)
		{
			SizeReport.Visit (visitor);
			ActionList.Visit (visitor);
			FailList.Visit (visitor);
		}
	}
}
