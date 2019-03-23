namespace System.Runtime.CompilerServices
{
#if !INSIDE_CORLIB
	public
#endif
	enum MonoLinkerFeature
	{
		Unknown, // keep this in first position, this is always false.
		ReflectionEmit,
		Remoting,
		Globalization,
		Encoding,
		Security,
		Martin
	}
}
