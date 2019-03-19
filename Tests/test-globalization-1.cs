using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestGlobalization
	{
		public static void Main ()
		{
			if (MonoLinkerSupport.IsTypeAvailable ("Mono.Globalization.Unicode.SimpleCollator"))
				throw new AssertionException ("Mono.Globalization.Unicode.SimpleCollator");
			if (MonoLinkerSupport.IsTypeAvailable ("System.Globalization.JapaneseCalendar"))
				throw new AssertionException ("System.Globalization.JapaneseCalendar");

			var now = DateTime.Now;
			Console.WriteLine (DateTime.Now);
			var parsed = DateTime.Parse (now.ToString ());
			if (now.ToString () != parsed.ToString ())
				throw new AssertionException ();
		}
	}
}
