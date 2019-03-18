using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestWeakInstances
	{
		public static void Main ()
		{
			RunWeakInstance1 (null);
			RunWeakInstance2 ("I am not Bar!");
		}

		static void Assert (bool condition, [CallerMemberName] string caller = null)
		{
			if (condition)
				return;
			if (!string.IsNullOrEmpty (caller))
				throw new ApplicationException ($"Assertion failed: {caller}");
			throw new ApplicationException ("Assertion failed");
		}

		public static void RunWeakInstance1 (object instance)
		{
			if (MonoLinkerSupport.AsWeakInstanceOf<Foo> (instance, out var foo)) {
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				foo.Hello ();
				Assert (false);
			}

			Console.Error.WriteLine ("DONE");
		}

		public static void RunWeakInstance2 (object instance)
		{
			if (MonoLinkerSupport.AsWeakInstanceOf<Bar> (instance, out var bar)) {
				Console.Error.WriteLine ("I LIVE ON THE MOON!");
				bar.Hello ();
				Assert (false);
			}

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
