using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestGlobalization
	{
		public static void Main ()
		{
			Console.WriteLine ("Hello!");

			var now = DateTime.Now;
			Console.WriteLine (DateTime.Now);
			var parsed = DateTime.Parse (now.ToString ());
			if (now.ToString () != parsed.ToString ())
				throw new AssertionException ();

			var dtfi = new DateTimeFormatInfo ();
			Console.WriteLine ($"DTFI: |{dtfi.DateSeparator}|{dtfi.TimeSeparator}| - {dtfi.DateSeparator == dtfi.TimeSeparator}");

			foreach (var pattern in dtfi.GetAllDateTimePatterns ('y'))
				Console.WriteLine ($"DTFI PATTERN: |{pattern}|");

			Test ();
		}

		static void Test ()
		{
			if (MonoLinkerSupport.IsTypeAvailable ("Mono.Globalization.Unicode.SimpleCollator"))
				throw new AssertionException ("Mono.Globalization.Unicode.SimpleCollator");
			if (MonoLinkerSupport.IsTypeAvailable ("System.Globalization.JapaneseCalendar"))
				throw new AssertionException ("System.Globalization.JapaneseCalendar");
			if (MonoLinkerSupport.IsTypeAvailable ("System.Globalization.TaiwanCalendar"))
				throw new AssertionException ("System.Globalization.TaiwanCalendar");
			if (MonoLinkerSupport.IsTypeAvailable ("System.Globalization.HebrewNumber"))
				throw new AssertionException ("System.Globalization.HebrewNumber");
		}
	}
}
