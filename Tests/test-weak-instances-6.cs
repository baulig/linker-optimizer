using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			TestAvailable1 ();
			TestAvailable2 ();
		}

		public static void TestAvailable1 ()
		{
			var supported = MonoLinkerSupport.IsTypeAvailable<Foo> ();
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
			if (supported)
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
		}

		public static void TestAvailable2 ()
		{
			var supported = MonoLinkerSupport.IsTypeAvailable ("Martin.LinkerTest.Foo");
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
			if (supported)
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");

			if (MonoLinkerSupport.IsTypeAvailable ("Martin.LinkerTest.Undefined"))
				throw new InvalidOperationException ("Undefined type!");
		}
	}

	class Foo
	{
		public void Hello ()
		{
			Console.WriteLine ("Hello World!");
		}
	}
}
