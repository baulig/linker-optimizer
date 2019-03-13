using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class LinkerSupport
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsWeakInstanceOf<T> (object obj) => obj is T;

		public static void SimpleTest ()
		{
			if (IsWeakInstanceOf<Foo> (null))
				throw new InvalidTimeZoneException ("I LIVE ON THE MOON!");
		}
	}
}
