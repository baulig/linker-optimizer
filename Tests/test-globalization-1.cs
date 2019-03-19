using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestGlobalization
	{
		public static void Main ()
		{
			if (MonoLinkerSupport.IsTypeAvailable ("System.Globalization.JapaneseCalendar"))
				throw new AssertionException ("System.Globalization.JapaneseCalendar.");
		}
	}
}
