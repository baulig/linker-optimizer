.assembly 'iltest-conditionals-2'
{
}

.assembly extern TestHelpers
{
	.ver 0:0:0:0
}

.namespace Martin.LinkerTest
{
	.class private auto ansi abstract sealed beforefieldinit TestConditionals
		extends [mscorlib]System.Object
	{
		.method public static hidebysig default void Main ()  cil managed 
		{
			.entrypoint
			call void class Martin.LinkerTest.TestConditionals::RunFeature1 ()
			ret
		}

		.method private static hidebysig default void RunFeature1 ()  cil managed noinlining
		{
			ldc.i4.0
			call void class [TestHelpers]System.Runtime.CompilerServices.MonoLinkerSupport::RequireFeature(valuetype [TestHelpers]System.Runtime.CompilerServices.MonoLinkerFeature)
			ret
		}
	}
}
