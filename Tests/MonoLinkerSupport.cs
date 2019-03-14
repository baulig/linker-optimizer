using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerSupport
{
	static class MonoLinkerSupport
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsWeakInstanceOf<T> (object obj) => obj is T;
	}
}
