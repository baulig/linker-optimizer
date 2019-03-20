.assembly 'iltest-flowanalysis-1'
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
			call int32 class Martin.LinkerTest.TestConditionals::FlowAnalysis1 ()
			ldc.i4.3
			beq test2

			ldstr "FlowAnalysis1() should have returned 3"
			newobj instance void class [mscorlib]System.InvalidOperationException::'.ctor'(string)
			throw

		test2:
			ret 
		}

		.method private static hidebysig default int32 FlowAnalysis1 ()  cil managed 
		{
			.locals init (int32 test)

			br end

		unreachable:
			call [mscorlib]System.Exception class Martin.LinkerTest.TestHelpers::AssertRemoved ()
		
		loop:
			ldloc.0
			ldc.i4.2
			add
			stloc.0
			br return
		
		end:
			ldloc.0
			ldc.i4.1
			add
			stloc.0
			br loop

		return:
			ldloc.0
			ret
		}
	
	}
}
