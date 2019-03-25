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
		}
	}
}
