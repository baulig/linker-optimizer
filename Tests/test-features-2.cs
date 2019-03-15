using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestFeatures
	{
		public static void Main ()
		{
			RunFeature1 ();
			RunFeature2 ();
			RunFeature3 (false);
			RunFeature3 (true);

			MonoLinkerSupport.MarkFeature (MonoLinkerFeature.Martin);
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
			if (!MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin))
				Assert (false);
		}

		public static void RunFeature2 ()
		{
			if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Remoting))
				Assert (false);
		}

		public static void RunFeature3 (bool test)
		{
			if (test)
				return;
			MonoLinkerSupport.MarkFeature (MonoLinkerFeature.Martin);
		}

	}
}
