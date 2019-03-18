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
				throw new AssertionException ($"Assertion failed: {caller}");
			throw new AssertionException ("Assertion failed");
		}


	}
}
