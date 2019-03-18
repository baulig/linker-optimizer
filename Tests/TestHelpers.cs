using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestHelpers
	{
		public static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (condition)
				return;
			if (!string.IsNullOrEmpty (caller))
				throw new AssertionException ($"Assertion failed at {caller}");
			throw new AssertionException ("Assertion failed");
		}

		public static void AssertFail (string message, [CallerMemberName] string caller = null)
		{
			if (!string.IsNullOrEmpty (caller))
				throw new AssertionException ($"Assertion failed at {caller}: {message}");
			throw new AssertionException ($"Assertion failed: {message}");
		}

		/*
		 * We scan the generated output for any references to this method and make the test fail.
		 */
		public static Exception AssertRemoved ()
		{
			throw new AssertionException ("This code should have been removed.");
		}

		public static void Debug ([CallerMemberName] string caller = null)
		{
			if (!string.IsNullOrEmpty (caller))
				Console.Error.WriteLine ($"TEST DEBUG AT {caller}");
			else
				Console.Error.WriteLine ($"TEST DEBUG");
		}

		public static void Debug (string message, [CallerMemberName] string caller = null)
		{
			if (!string.IsNullOrEmpty (caller))
				Console.Error.WriteLine ($"TEST DEBUG AT {caller}: {message}");
			else
				Console.Error.WriteLine ($"TEST DEBUG: {message}");
		}
	}
}
