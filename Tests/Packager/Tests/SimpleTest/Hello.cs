using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.DevServer;

namespace SimpleTest
{
	public class Hello : DebuggerTestBase
	{
		[Fact]
		public void World ()
		{
			Console.Error.WriteLine ($"Hello World!");
		}

		[Fact]
		public async Task StartServer ()
		{
			await Ready ();

			Debug.WriteLine ($"SERVER READY");

			await Task.Delay (-1);
		}
	}
}
