namespace System.Runtime.CompilerServices
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
		public static bool IsTypeAvailable<T> () => true;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool AsWeakInstanceOf<T> (object obj, out T instance) where T : class
		{
			instance = obj as T;
			return instance != null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsFeatureSupported (MonoLinkerFeature feature) => true;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void MarkFeature (MonoLinkerFeature feature)
		{ }	
	}
}
