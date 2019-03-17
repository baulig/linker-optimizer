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
			call void class Martin.LinkerTest.TestConditionals::RunFeature3 ()
			call void class Martin.LinkerTest.TestConditionals::RunFeature4 ()
			call void class Martin.LinkerTest.TestConditionals::RunFeature5 ()
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

		.method private static hidebysig default void RunFeature3 ()  cil managed 
		{
			.locals init (int32 V_0, int32 V_1)

			ldc.i4.3
			stloc.1

			ldc.i4.2
			call bool class System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype System.Runtime.CompilerServices.MonoLinkerFeature)
			brfalse return

		conditional:
			ldc.i4.4
			stloc.0
		
		loop:
			ldc.i4.0
			stloc.1

		loop1:
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop

		return:
			ldloc.1
			brtrue loop

			ret
		}

		.method private static hidebysig default void RunFeature4 ()  cil managed 
		{
			.locals init (int32 V_0, int32 V_1)

			ldc.i4.3
			stloc.1

			ldc.i4.2
			call bool class System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype System.Runtime.CompilerServices.MonoLinkerFeature)
			brtrue return

		conditional:
			br start
		
		loop:
			ldc.i4.0
			stloc.1
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop
			ret

		start:
			ldc.i4.4
			stloc.0
			br loop

		return:
			ret
		}

		.method private static hidebysig default void RunFeature5 ()  cil managed 
		{
			.locals init (int32 V_0, int32 V_1)

			ldc.i4.3
			stloc.1

			ldc.i4.2
			call bool class System.Runtime.CompilerServices.MonoLinkerSupport::IsFeatureSupported (valuetype System.Runtime.CompilerServices.MonoLinkerFeature)
			brfalse return

		conditional:
			br start
		
		loop:
			ldc.i4.0
			stloc.1
			ldloc.0
			ldc.i4.1
			sub
			dup
			stloc.0
			brtrue loop
			ret

		start:
			ldc.i4.4
			stloc.0
			br loop

		return:
			ret
		}			
	}
}
