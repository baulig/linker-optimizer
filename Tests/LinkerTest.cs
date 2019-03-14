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
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
		}

		public static void UnsupportedTryCatch ()
		{
			try
			{
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON");
			}
			catch
			{
				throw;
			}
		}
	}
}
