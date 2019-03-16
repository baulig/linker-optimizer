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

		static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (condition)
				return;
			if (!string.IsNullOrEmpty (caller))
				throw new ApplicationException ($"Assertion failed: {caller}");
			throw new ApplicationException ("Assertion failed");
		}

		public static void RunWeakInstance ()
		{
			if (MonoLinkerSupport.IsWeakInstanceOf<Foo> (null))
			{
				Foo.Hello ();
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				Assert (false);
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
