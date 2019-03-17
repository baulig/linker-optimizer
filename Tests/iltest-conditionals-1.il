.assembly 'iltest-conditionals-1'
{
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
			call void class Martin.LinkerTest.TestConditionals::RunFeature2 ()
			ret 
		}

		.method private static hidebysig default void RunFeature1 ()  cil managed 
		{
			.locals init (int32 test)

			ldc.i4.2
			call bool class System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype System.Runtime.CompilerServices.MonoLinkerFeature)
			brtrue return

		conditional:
			ldc.i4.4
			stloc.0
		
		loop:
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop

		return:
			ret
		}

		.method private static hidebysig default void RunFeature2 ()  cil managed 
		{
			.locals init (int32 test)

			ldc.i4.2
			call bool class System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype System.Runtime.CompilerServices.MonoLinkerFeature)
			brfalse return

		conditional:
			ldc.i4.4
			stloc.0
		
		loop:
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop

		return:
			ret
		}

	}
}
