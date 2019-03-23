using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	class TestConditionals
	{
		public static void Main ()
		{
			if (Property)
				Foo.Hello ();
			if (!Property2)
				Foo.Hello ();
		}

		static bool value;

		internal static bool Property {
			get {
				return MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin) && ReturnFalse ();
			}
		}

		internal static bool Property2 {
			get {
				return !MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin) || value;
			}
		}

		static bool ReturnFalse ()
		{
			return false;
		}
	}

	class Foo
	{
		public static void Hello ()
		{
			TestHelpers.AssertRemoved ();
		}
	}
}
