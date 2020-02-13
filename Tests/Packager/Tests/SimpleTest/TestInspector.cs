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
	public class TestInspector : InspectorTestBase
	{
		[Fact]
		public async Task TestScripts ()
		{
			await Ready ();

			Debug.WriteLine ($"SERVER READY: {ScriptsIdToUrl}");
			Debug.WriteLine ($"SERVER READY");

			Assert.True (ScriptsIdToUrl.ContainsValue ($"dotnet://{Settings.DevServer_Assembly}/Hello.cs"));
		}

		[Fact]
		public async Task CreateGoodBreakpoint ()
		{
			await Ready (async (cli, token) =>
			{
				var bp1_req = JObject.FromObject (new
				{
					lineNumber = 8,
					columnNumber = 3,
					url = FileToUrl[$"dotnet://{Settings.DevServer_Assembly}/Hello.cs"],
				});

				var bp1_res = await cli.SendCommand ("Debugger.setBreakpointByUrl", bp1_req, token);
				Assert.True (bp1_res.IsOk);
				Assert.Equal ("dotnet:0", bp1_res.Value["breakpointId"]);
				Assert.Equal (1, bp1_res.Value["locations"]?.Value<JArray> ()?.Count);

				var loc = bp1_res.Value["locations"]?.Value<JArray> ()[0];

				Assert.NotNull (loc["scriptId"]);
				Assert.Equal ($"dotnet://{Settings.DevServer_Assembly}/Hello.cs", ScriptsIdToUrl[loc["scriptId"]?.Value<string> ()]);
				Assert.Equal (8, loc["lineNumber"]);
				Assert.Equal (3, loc["columnNumber"]);
			});
		}
	}
}
