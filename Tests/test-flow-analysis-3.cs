using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFlowAnalysis
	{
		public static void Main ()
		{
			TryCatchMethod ();
			TestWithFinally ();
		}

		public static bool TryCatchMethod ()
		{
			try
			{
				throw new NotSupportedException ();
			}
			catch
			{
				return false;
			}
		}

		public static void TestWithFinally ()
		{
			try {
				Console.WriteLine ("Hello!");
			} finally {
				Console.WriteLine ("Finally!");
			}
		}
	}
}

