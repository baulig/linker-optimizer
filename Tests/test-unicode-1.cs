using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestUnicode
	{
		public static void Main ()
		{
			if (MonoLinkerSupport.IsTypeAvailable ("Mono.Globalization.Unicode.SimpleCollator"))
				throw new AssertionException ("SimpleCollator should have been removed.");
		}
	}
}
