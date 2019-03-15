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

			var supported = Run5 ();
			Assert (!supported);

			Run6 (out supported);
			Assert (!supported);

			Run7 (ref supported);
			Assert (!supported);

			Run8 ();

			supported = Run9 (null);
			Assert (!supported);

			Run10 ();
			Run11 ();
			Run12 ();
			Run13 ();
		}

		static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (condition)
				return;
			if (!string.IsNullOrEmpty (caller))
				throw new ApplicationException ($"Assertion failed: {caller}");
			throw new ApplicationException ("Assertion failed");
		}

		public static void Run ()
		{
			UnsupportedTryCatch ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
			{
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				Assert (false);
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
			Assert (!supported);
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
			Assert (!instance.Supported);
		}

		public static bool Run9 (object instance)
		{
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance);
		}

		public static bool Run10 ()
		{
			var instance = new InstanceTest ();
			Console.WriteLine (instance);
			if (UnsupportedTryCatch ())
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Field);
		}

		public static void Run11 ()
		{
			Console.WriteLine (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null));
			Console.WriteLine ();
		}

		public static void Run12 ()
		{
			var instance = new InstanceTest ();
			Console.WriteLine (MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Instance));
			Console.WriteLine ();
		}

		public static void Run13 ()
		{
			var instance = new InstanceTest ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Instance))
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

		class InstanceTest
		{
			bool supported;

			public bool Supported => supported;

			public object Field;

			public object Instance => this;

			public void Run ()
			{
				supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
			}
		}
	}
}
