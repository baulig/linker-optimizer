using System;
using System.Runtime.CompilerServices;
using Martin.LinkerSupport;

namespace Martin.LinkerTest
{
	static class SimpleTests
	{
		public static void Run ()
		{
			UnsupportedTryCatch ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
			{
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				// throw new InvalidTimeZoneException("I LIVE ON THE MOON!");
			}

			Console.Error.WriteLine ("DONE");
		}

		public static bool UnsupportedTryCatch ()
		{
			try
			{
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON");
			}
			catch
			{
				return false;
			}
		}
	}
}
