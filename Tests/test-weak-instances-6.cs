using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			RunWeakInstance ();
		}

		public static void RunWeakInstance ()
		{
			var supported = MonoLinkerSupport.IsTypeAvailable<Foo> ();
			Console.Error.WriteLine ($"SUPPORTED: {supported}");
			if (supported)
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
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
