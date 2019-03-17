using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature ();
		}

		public static void RunFeature ()
		{
			for (int i = 0; i < 3; i++) {
				if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin))
					break;
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
			}

			Console.WriteLine ($"DONE!");
		}
	}
}
