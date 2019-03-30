//
// FastResolver.cs
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
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Optimizer
{
	using BasicBlocks;

	public class FastResolver
	{
		public OptimizerContext Context {
			get;
		}

		public FastResolver (OptimizerContext context)
		{
			Context = context;

			type_hash = new HashSet<TypeDefinition> ();
			method_hash = new HashSet<MethodDefinition> ();
			module_registration = new Dictionary<ModuleDefinition, ModuleRegistration> ();
		}

		readonly HashSet<TypeDefinition> type_hash;
		readonly HashSet<MethodDefinition> method_hash;
		readonly Dictionary<ModuleDefinition, ModuleRegistration> module_registration;

		ModuleRegistration GetModuleRegistration (ModuleDefinition module)
		{
			if (!module_registration.TryGetValue (module, out var registration)) {
				registration = new ModuleRegistration (module);
				module_registration.Add (module, registration);
			}
			return registration;
		}

		internal void RegisterSupportType (TypeDefinition type)
		{
			GetModuleRegistration (type.Module).RegisterType (type);
			type_hash.Add (type);
		}

		internal void RegisterSupportMethod (MethodDefinition method)
		{
			var registration = GetModuleRegistration (method.Module);
			registration.RegisterType (method.DeclaringType);
			registration.RegisterMethod (method);
			type_hash.Add (method.DeclaringType);
			method_hash.Add (method);
		}

		internal void RegisterConstantMethod (MethodDefinition method, ConstantValue value)
		{
			Context.LogDebug ($"REGISTER CONSTANT: {method} {value}");
			RegisterSupportMethod (method);
		}

		internal bool TryFastResolve (MethodReference reference, out MethodDefinition resolved)
		{
			Context.LogDebug ($"TRY FAST RESOLVE: {reference.GetType ().Name} {reference.Module} {reference.MetadataToken} {reference}");

//			resolved = reference.Resolve ();
//			if (method_hash.Contains (resolved)) {
//				Context.LogDebug ($"TRY FAST RESOLVE #1: {reference.GetType ().Name} {reference.Module} {reference.MetadataToken} {reference}");
//				Context.Debug ();
//				if (!(reference is MethodDefinition))
//					throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
//			}

			if (reference.FullName.Contains ("Martin") || reference.FullName.Contains ("MonoLinker"))
				Context.Debug ();

			if (reference is MethodDefinition method) {
				resolved = method;
				return true;
			}

			resolved = null;
			if (reference.MetadataToken.TokenType != TokenType.MemberRef)
				return false;

			if (reference.DeclaringType.IsNested || reference.DeclaringType.HasGenericParameters)
				return false;

			var type = reference.DeclaringType.Resolve ();

			if (type_hash.Contains (type)) {
				resolved = reference.Resolve ();
				return true;
			}

			if (!module_registration.TryGetValue (type.Module, out var module)) {
				resolved = null;
				return false;
			}

			var result = module.TryGetMethod (reference.MetadataToken, out resolved);
			Context.LogDebug ($"TRY FAST RESOVLE #1: {result} {resolved}");

			resolved = null;
			return false;
		}

		class ModuleRegistration
		{
			public ModuleDefinition Module {
				get;
			}

			readonly HashSet<TypeDefinition> type_hash;
			readonly Dictionary<uint, MethodDefinition> method_hash;

			public ModuleRegistration (ModuleDefinition module)
			{
				Module = module;
				type_hash = new HashSet<TypeDefinition> ();
				method_hash = new Dictionary<uint, MethodDefinition> ();
			}

			public void RegisterType (TypeDefinition type)
			{
				DebugHelpers.Assert (type.Module == Module);
				type_hash.Add (type);
			}

			public void RegisterMethod (MethodDefinition method)
			{
				DebugHelpers.Assert (method.Module == Module);
				method_hash.Add (method.MetadataToken.RID, method);
			}

			public bool TryGetMethod (MetadataToken token, out MethodDefinition method)
			{
				DebugHelpers.Assert (token.TokenType == TokenType.MemberRef);

				if (method_hash.TryGetValue (token.RID, out method)) {
					return true;
				}

				method = null;
				return false;
			}
		}
	}
}
