//
// Program.cs
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
using System.Xml.XPath;
using System.Collections.Generic;

namespace Mono.Linker.Optimizer
{
	public class Program
	{
		public static int Main (string[] args)
		{
			if (args.Length == 0) {
				Console.Error.WriteLine ("No parameters specified");
				return 1;
			}

			var arguments = ProcessResponseFile (args);
			var program = new Program (arguments);

			Driver.Execute (arguments.ToArray ());

			return 0;
		}

		static List<string> ProcessResponseFile (string[] args)
		{
			var result = new Queue<string> ();
			foreach (string arg in args) {
				if (arg.StartsWith ("@", StringComparison.Ordinal)) {
					try {
						var responseFileName = arg.Substring (1);
						var responseFileLines = File.ReadLines (responseFileName);
						Driver.ParseResponseFileLines (responseFileLines, result);
					} catch (Exception e) {
						Console.Error.WriteLine ("Cannot read response file with exception " + e.Message);
						Environment.Exit (1);
					}
				} else {
					result.Enqueue (arg);
				}
			}
			return result.ToList ();
		}

		Program (List<string> arguments)
		{
			ParseArguments (arguments);
		}

		void ParseArguments (List<string> arguments)
		{
			var martinsPlayground = false;
			var documents = new List<XPathDocument> ();
			var options = new List<string> ();

			while (arguments.Count > 0) {
				var token = arguments[0];
				if (!token.StartsWith ("--martin", StringComparison.Ordinal))
					break;

				arguments.RemoveAt (0);
				switch (token) {
				case "--martin":
					martinsPlayground = true;
					continue;
				case "--martin-xml":
					var filename = arguments[0];
					arguments.RemoveAt (0);
					documents.Insert (0, new XPathDocument (filename));
					break;
				case "--martin-args":
					options.Add (arguments[0]);
					arguments.RemoveAt (0);
					break;
				}
			}

			Console.Error.WriteLine ($"PARSE ARGS: {martinsPlayground} {arguments.Count}");

			foreach (var arg in arguments) {
				Console.Error.WriteLine ($"ARGUMENT: {arg}");
			}
		}
	}
}

