namespace System.Runtime.CompilerServices
{
#if !INSIDE_CORLIB
	public
#endif
	enum MonoLinkerFeature
	{
		Unknown, // keep this in first position, this is always false.
		Martin,
		ReflectionEmit,
		Serialization,
		Remoting,
		Globalization,
		Encoding,
		Security,
	}
}
