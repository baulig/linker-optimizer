//
// XApiReader.cs
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
using System.Xml.XPath;
using Mono.Linker.Conditionals;

namespace Mono.Linker
{
	partial class XApiReader
	{
		partial void DoAdditionalProcessing ()
		{
			if (_context.MartinContext == null)
				return;

			var nav = _document.CreateNavigator ();

			var root = nav.SelectSingleNode ("/linker/martin");
			if (root == null)
				return;

			var options = root.SelectSingleNode ("options");
			if (options != null)
				OnOptions (_context.MartinContext.Options, options);

			ProcessChildren (root, "features/feature", OnFeature);

			ProcessChildren (root, "debug/type", OnDebugType);
			ProcessChildren (root, "debug/method", OnDebugMethod);
		}

		void OnOptions (MartinOptions options, XPathNavigator nav)
		{
			if (GetBoolAttribute (nav, "all-modules", out var value))
				options.ScanAllModules = value;

			if (GetBoolAttribute (nav, "analyze-all", out value))
				options.AnalyzeAll = value;

			if (GetBoolAttribute (nav, "preprocess", out value))
				options.Preprocess = value;

			if (GetBoolAttribute (nav, "no-conditional-redefinition", out value))
				options.NoConditionalRedefinition = value;

			if (GetBoolAttribute (nav, "ignore-resolution-errors", out value))
				options.IgnoreResolutionErrors = value;

			if (GetBoolAttribute (nav, "report-size", out value))
				options.ReportSize = value;
		}

		void OnFeature (XPathNavigator nav)
		{
			var name = GetAttribute (nav, "name");
			var value = GetAttribute (nav, "enabled");

			if (string.IsNullOrEmpty (value) || !bool.TryParse (value, out var enabled))
				enabled = true;

			_context.MartinContext.LogMessage (MessageImportance.Low, $"FEATURE FROM XML: {name} {enabled}");
			_context.MartinContext.SetFeatureEnabled (name, enabled);
		}

		bool GetBoolAttribute (XPathNavigator nav, string name, out bool value)
		{
			var attr = GetAttribute (nav, name);
			if (!string.IsNullOrEmpty (attr) && bool.TryParse (attr, out value))
				return true;
			value = false;
			return false;
		}

		bool GetBoolAttribute (XPathNavigator nav, string name)
		{
			if (!GetBoolAttribute (nav, name, out var value))
				return false;
			return value;
		}

		bool GetName (XPathNavigator nav, out string name, out MartinOptions.MatchKind match)
		{
			name = GetAttribute (nav, "name");
			var fullname = GetAttribute (nav, "fullname");
			var substring = GetAttribute (nav, "substring");

			if (!string.IsNullOrEmpty (fullname)) {
				match = MartinOptions.MatchKind.FullName;
				if (!string.IsNullOrEmpty (name) || !string.IsNullOrEmpty (substring))
					return false;
				name = fullname;
			} else if (!string.IsNullOrEmpty (name)) {
				match = MartinOptions.MatchKind.Name;
				if (!string.IsNullOrEmpty (fullname) || !string.IsNullOrEmpty (substring))
					return false;
			} else if (!string.IsNullOrEmpty (substring)) {
				match = MartinOptions.MatchKind.Substring;
				if (!string.IsNullOrEmpty (name) || !string.IsNullOrEmpty (fullname))
					return false;
				name = substring;
			} else {
				match = MartinOptions.MatchKind.Name;
				return false;
			}

			return true;
		}

		Exception ThrowMissingActionAttribute (XPathNavigator nav)
		{
			_context.MartinContext.LogMessage (MessageImportance.High, $"Missing `action` attribute in {nav.OuterXml}.");
			throw new NotSupportedException ($"Missing `action` attribute in {nav.OuterXml}.");
		}

		void OnDebugType (XPathNavigator nav)
		{
			if (!GetName (nav, out var name, out var match)) {
				_context.MartinContext.LogMessage (MessageImportance.High, $"Ambiguous name in type entry `{nav.OuterXml}`.");
				throw new NotSupportedException ($"Ambiguous name in type entry `{nav.OuterXml}`.");
			}

			var action = GetAttribute (nav, "action");
			if (string.IsNullOrEmpty (action)) {
				_context.MartinContext.LogMessage (MessageImportance.High, $"Missing `action` attribute in {nav.OuterXml}.");
				throw new NotSupportedException ($"Missing `action` attribute in {nav.OuterXml}.");
			}

			if (!Enum.TryParse<MartinOptions.TypeAction> (action, true, out var typeAction)) {
				_context.MartinContext.LogMessage (MessageImportance.High, $"Invalid `action` attribute in {nav.OuterXml}.");
				throw new NotSupportedException ($"Invalid `action` attribute in {nav.OuterXml}.");
			}

			_context.MartinContext.LogMessage (MessageImportance.Low, $"PREPROCESS FROM XML: {name} {match} {typeAction}");

			_context.MartinContext.Options.AddTypeEntry (name, match, typeAction);
		}

		void OnDebugMethod (XPathNavigator nav)
		{
			if (!GetName (nav, out var name, out var match)) {
				_context.MartinContext.LogMessage (MessageImportance.High, $"Ambiguous name in method entry `{nav.OuterXml}`.");
				throw new NotSupportedException ($"Ambiguous name in method entry `{nav.OuterXml}`.");
			}

			var action = GetAttribute (nav, "action");
			if (string.IsNullOrEmpty (action)) {
				_context.MartinContext.LogMessage (MessageImportance.High, $"Missing `action` attribute in {nav.OuterXml}.");
				throw new NotSupportedException ($"Missing `action` attribute in {nav.OuterXml}.");
			}

			if (!Enum.TryParse<MartinOptions.MethodAction> (action, true, out var methodAction)) {
				_context.MartinContext.LogMessage (MessageImportance.High, $"Invalid `action` attribute in {nav.OuterXml}.");
				throw new NotSupportedException ($"Invalid `action` attribute in {nav.OuterXml}.");
			}

			_context.MartinContext.LogMessage (MessageImportance.Low, $"PREPROCESS FROM XML: {nav} {match} {methodAction}");

			_context.MartinContext.Options.AddMethodEntry (name, match, methodAction);
		}
	}
}
