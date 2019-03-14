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
			Run6 (out var supported);
			Run7 (ref supported);
			Run8 ();
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

		public static void Run6 (out bool supported)
		{
			supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void Run7 (ref bool supported)
		{
			supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void Run8 ()
		{
			var instance = new InstanceTest ();
			instance.Run ();
			Console.WriteLine (instance.Supported);
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

		class InstanceTest
		{
			bool supported;

			public bool Supported => supported;

			public void Run ()
			{
				supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
			}
		}
	}
}
