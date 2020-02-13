using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Net.WebSockets;
using System.Collections.Generic;

using WebAssembly.Net.Debugging;
using Mono.WasmPackager.DevServer;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	public class InspectorTestBase : DebuggerTestBase
	{
		protected Inspector Inspector {
			get;
		}

		protected Dictionary<string, string> ScriptsIdToUrl {
			get;
		}

		protected Dictionary<string, string> FileToUrl {
			get;
		}

		protected InspectorTestBase (Assembly caller = null)
			: base (caller ?? Assembly.GetCallingAssembly ())
		{
			Inspector = new Inspector (Server);
			ScriptsIdToUrl = new Dictionary<string, string> ();
			FileToUrl = new Dictionary<string, string> ();
			SubscribeToScripts ();
		}

		public override async Task Ready ()
		{
			await base.Ready ();
			await Inspector.Ready ();
		}

		public async Task Ready (Func<InspectorClient, CancellationToken, Task> cb = null, TimeSpan? span = null)
		{
			await base.Ready ();
			await Inspector.Ready (cb, span);
		}

		void SubscribeToScripts ()
		{
			Inspector.On ("Debugger.scriptParsed", async (args, c) =>
			{
				var script_id = args?["scriptId"]?.Value<string> ();
				var url = args["url"]?.Value<string> ();
				if (script_id.StartsWith ("dotnet://")) {
					var dbgUrl = args["dotNetUrl"]?.Value<string> ();
					var arrStr = dbgUrl.Split ("/");
					dbgUrl = arrStr[0] + "/" + arrStr[1] + "/" + arrStr[2] + "/" + arrStr[arrStr.Length - 1];
					ScriptsIdToUrl[script_id] = dbgUrl;
					FileToUrl[dbgUrl] = args["url"]?.Value<string> ();
				}
				await Task.FromResult (0);
			});
		}
	}
}