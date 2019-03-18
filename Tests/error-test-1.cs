using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class ErrorTest
	{
		public static void Main ()
		{
			RunErrorTest (null);
		}

		public static void RunErrorTest (object instance)
		{
			if (MonoLinkerSupport.AsWeakInstanceOf<Bar> (instance, out var bar)) {
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				bar.Hello ();
				TestHelpers.Assert (false);
			}

			/*
			 * This line will produce an error message:
			 *
			 * Attempting to mark type `Martin.LinkerTest.Bar` after it's already been used in a conditional!
			*/
			Console.Error.WriteLine ($"DONE: {bar != null}");
		}
	}

	public class Foo
	{
		public void Hello ()
		{
			Console.WriteLine ("World");
		}
	}

	public class Bar
	{
		public void Hello ()
		{
			Console.WriteLine ("I am not Foo!");
		}
	}
}
