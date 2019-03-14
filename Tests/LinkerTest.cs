using System;
using System.Runtime.CompilerServices;
using Martin.LinkerSupport;

namespace Martin.LinkerTest
{
	static class SimpleTests
	{
		public static void RunAll ()
		{
			Run ();
			Run2 ();
			Run3 ();
		}

		public static void Run ()
		{
			UnsupportedTryCatch ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
			{
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				// throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
			}

			Console.Error.WriteLine ("DONE");
		}

		public static void Run2 ()
		{
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");

			Console.Error.WriteLine ("DONE");
		}

		public static void Run3 ()
		{
			Console.Error.WriteLine ("HELLO");
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");

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
