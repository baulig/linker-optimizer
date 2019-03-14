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
			Run4 ();
			Run5 ();
		}

		public static void Run ()
		{
			UnsupportedTryCatch ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
			{
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
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

		public static void Run4 ()
		{
			var supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
		}

		public static bool Run5 ()
		{
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
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
