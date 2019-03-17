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

    // method line 1
    .method public static hidebysig 
           default bool IsWeakInstanceOf<T> (object obj)  cil managed noinlining 
    {
        // Method begins at RVA 0x2050
	// Code size 10 (0xa)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  isinst !!0
	IL_0006:  ldnull 
	IL_0007:  cgt.un 
	IL_0009:  ret 
    } // end of method MonoLinkerSupport::IsWeakInstanceOf

    // method line 2
    .method public static hidebysig 
           default bool IsFeatureSupported (valuetype System.Runtime.CompilerServices.MonoLinkerFeature feature)  cil managed noinlining 
    {
        // Method begins at RVA 0x205b
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.1 
	IL_0001:  ret 
    } // end of method MonoLinkerSupport::IsFeatureSupported

    // method line 3
    .method public static hidebysig 
           default void MarkFeature (valuetype System.Runtime.CompilerServices.MonoLinkerFeature feature)  cil managed noinlining 
    {
        // Method begins at RVA 0x205e
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method MonoLinkerSupport::MarkFeature

  } // end of class System.Runtime.CompilerServices.MonoLinkerSupport
}
