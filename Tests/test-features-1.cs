using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature1 ();

			var supported = RunFeature2 ();
			Assert (!supported);

			supported = RunFeature3 ();
			Assert (supported);
		}

		static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (condition)
				return;
			if (!string.IsNullOrEmpty (caller))
				throw new ApplicationException ($"Assertion failed: {caller}");
			throw new ApplicationException ("Assertion failed");
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
	}
}
