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
			call void class Martin.LinkerTest.TestConditionals::RunFeature ()
			ret 
		}

		.method private static hidebysig default void RunFeature ()  cil managed 
		{
			ret
		}
	}
}
