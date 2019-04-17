﻿//
// CompareWithReportStep.cs
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
using System.Xml;
using System.Xml.XPath;

namespace Mono.Linker.Optimizer
{
	using Configuration;

	public class CompareWithReportStep : OptimizerBaseStep
	{
		public OptimizerConfiguration Configuration {
			get;
		}

		public CompareWithReportStep (OptimizerContext context)
			: base (context)
		{
			Configuration = new OptimizerConfiguration ();
		}

		protected override void Process ()
		{
			var settings = new XmlReaderSettings ();
			using (var xml = XmlReader.Create (Options.CompareWith, settings)) {
				var document = new XPathDocument (xml);
				var nav = document.CreateNavigator ();

				var root = nav.SelectSingleNode ("/optimizer-report");
				if (root == null) {
					Context.LogWarning ($"Cannot find root node in `{Options.CompareWith}`.");
					return;
				}

				var reader = new ConfigurationReader (Configuration);
				reader.Read (root);
			}

			ProcessSizeReport ();
		}

		void ProcessSizeReport ()
		{
			Context.LogDebug ($"TEST");
		}
	}
}
