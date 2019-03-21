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
			var value = TestWithFinally ();
			TestHelpers.AssertEqual (2, value, "TestWithFinally");
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

		public static int TestWithFinally ()
		{
			int value = 0;
			try {
				Console.WriteLine ("Hello!");
				value++;
				return value;
			} finally {
				Console.WriteLine ("Finally!");
				value++;
			}
			return value;
		}
	}
}
