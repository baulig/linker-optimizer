using System;
using System.Runtime.CompilerServices;

namespace Martin.LinkerSupport
{
	enum MonoLinkerFeature
	{
		ReflectionEmit,
		Remoting,
		Martin
	}

	static class MonoLinkerSupport
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsWeakInstanceOf<T> (object obj) => obj is T;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsFeatureSupported (MonoLinkerFeature feature) => true;
	}
}
