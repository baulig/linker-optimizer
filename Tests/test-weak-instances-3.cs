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
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
			{
				Foo.Hello ();
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				TestHelpers.Assert (false);
			}

			Console.Error.WriteLine ("DONE");
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
