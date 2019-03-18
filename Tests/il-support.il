.assembly extern mscorlib
{
	.ver 4:0:0:0
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.namespace System.Runtime.CompilerServices
{
	.class private auto ansi sealed MonoLinkerFeature
		extends [mscorlib]System.Enum
	{
		.field  public specialname rtspecialname  int32 value__
		.field public static literal  valuetype System.Runtime.CompilerServices.MonoLinkerFeature ReflectionEmit = int32(0x00000000)
		.field public static literal  valuetype System.Runtime.CompilerServices.MonoLinkerFeature Remoting = int32(0x00000001)
		.field public static literal  valuetype System.Runtime.CompilerServices.MonoLinkerFeature Martin = int32(0x00000002)
	} // end of class System.Runtime.CompilerServices.MonoLinkerFeature
}

.namespace System.Runtime.CompilerServices
{
	.class private auto ansi abstract sealed beforefieldinit MonoLinkerSupport
		extends [mscorlib]System.Object
	{
		.method public static hidebysig 
			default bool IsWeakInstanceOf<T> (object obj)  cil managed noinlining 
		{
			.maxstack 8
			ldarg.0 
			isinst !!0
			ldnull 
			cgt.un 
			ret 
		}

		.method public static hidebysig 
			default bool IsFeatureSupported (valuetype System.Runtime.CompilerServices.MonoLinkerFeature feature)  cil managed noinlining 
		{
			.maxstack 8
			ldc.i4.1 
			ret 
		}

		.method public static hidebysig 
			default bool IsTypeAvailable<T> ()  cil managed noinlining 
		{
			.maxstack 8
			ldc.i4.1 
			ret 
		}

		.method public static hidebysig 
			default bool IsTypeAvailable (string 'type')  cil managed noinlining 
		{
			.maxstack 8
			ldc.i4.1 
			ret 
		}

		.method public static hidebysig 
			default bool AsWeakInstanceOf<class T> (object obj, [out] !!T& 'instance')  cil managed noinlining 
		{
			.maxstack 8
			ldarg.1 
			ldarg.0 
			isinst !!0
			unbox.any !!0
			stobj !!0
			ldarg.1 
			ldobj !!0
			box !!0
			ldnull 
			cgt.un 
			ret 
		}
	}
}
