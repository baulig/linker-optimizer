using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			RunWeakInstance1 ();
			RunWeakInstance2 ();
			RunWeakInstance3 ();
			RunWeakInstance4 ();

			var supported = RunWeakInstance5 ();
			Assert (!supported);

			RunWeakInstance6 (out supported);
			Assert (!supported);

			RunWeakInstance7 (ref supported);
			Assert (!supported);

			RunWeakInstance8 ();

			supported = RunWeakInstance9 (null);
			Assert (!supported);

			RunWeakInstance10 ();
			RunWeakInstance11 ();
			RunWeakInstance12 ();
			RunWeakInstance13 ();
		}

		static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (condition)
				return;
			if (!string.IsNullOrEmpty (caller))
				throw new ApplicationException ($"Assertion failed: {caller}");
			throw new ApplicationException ("Assertion failed");
		}

		public static void RunWeakInstance1 ()
		{
			UnsupportedTryCatch ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
			{
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				Assert (false);
			}

			Console.Error.WriteLine ("DONE");
		}

		public static void RunWeakInstance2 ()
		{
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");

			Console.Error.WriteLine ("DONE");
		}

		public static void RunWeakInstance3 ()
		{
			Console.Error.WriteLine ("HELLO");
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");

			Console.Error.WriteLine ("DONE");
		}

		public static void RunWeakInstance4 ()
		{
			var supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
			Assert (!supported);
		}

		public static bool RunWeakInstance5 ()
		{
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void RunWeakInstance6 (out bool supported)
		{
			supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void RunWeakInstance7 (ref bool supported)
		{
			supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
		}

		public static void RunWeakInstance8 ()
		{
			var instance = new InstanceTest ();
			instance.Run ();
			Console.WriteLine (instance.Supported);
			Assert (!instance.Supported);
		}

		public static bool RunWeakInstance9 (object instance)
		{
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance);
		}

		public static bool RunWeakInstance10 ()
		{
			var instance = new InstanceTest ();
			Console.WriteLine (instance);
			if (UnsupportedTryCatch ())
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
			return MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Field);
		}

		public static void RunWeakInstance11 ()
		{
			Console.WriteLine (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null));
			Console.WriteLine ();
		}

		public static void RunWeakInstance12 ()
		{
			var instance = new InstanceTest ();
			Console.WriteLine (MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Instance));
			Console.WriteLine ();
		}

		public static void RunWeakInstance13 ()
		{
			var instance = new InstanceTest ();
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (instance.Instance))
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");

			Console.Error.WriteLine ("DONE");
		}

		public static void RunFeature1 ()
		{
			if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Remoting))
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
		}

		public static bool RunFeature2 ()
		{
			return MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Remoting);
		}

		public static bool RunFeature3 ()
		{
			return MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin);
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

			public object Field = null;

			public object Instance => this;

			public void Run ()
			{
				supported = MonoLinkerSupport.IsWeakInstanceOf<Foo> (null);
			}
		}
	}

	public class Foo
	{
		public static void Hello ()
		{
			Console.WriteLine ("World");
		}
	}
}
