using System;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestCrypto
	{
		public static void Main ()
		{
			var sha = HashAlgorithm.Create ("SHA1");
			var md5 = HashAlgorithm.Create ("MD5");
			Console.WriteLine ($"TEST: {sha != null} {md5 != null}");

			Test ();
		}

		public static void Test ()
		{
			Console.Error.WriteLine ("MARTIN TEST!");
			var asm = typeof (int).Assembly;

			Console.Error.WriteLine ($"MARTIN TEST #1: {asm.Location}");

			var support = asm.GetType ("System.Globalization.GlobalizationSupport");
			if (support == null)
				throw new NotSupportedException ("Type `System.Globalization.GlobalizationSupport` not found.");

			var method = support.GetMethod ("MartinTest");
			if (method == null)
				throw new NotSupportedException ("Method not found.");

			method.Invoke (null, null);
			Console.Error.WriteLine ($"MARTIN TEST DONE");
		}
	}
}
