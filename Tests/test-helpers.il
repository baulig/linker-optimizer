.namespace Martin.LinkerTest
{
	.class private auto ansi beforefieldinit AssertionException
		extends [mscorlib]System.Exception
	{
		.method public hidebysig specialname rtspecialname
			instance default void '.ctor' () cil managed
		{
			.maxstack 8
			ldarg.0
			call instance void class [mscorlib]System.Exception::'.ctor'()
			ret
		}

		.method public hidebysig specialname rtspecialname
			instance default void '.ctor' (string message) cil managed
		{
			.maxstack 8
			ldarg.0
			ldarg.1
			call instance void class [mscorlib]System.Exception::'.ctor'(string)
			ret
		}
	}
}

.namespace Martin.LinkerTest
{
	.class private auto ansi abstract sealed beforefieldinit TestHelpers
		extends [mscorlib]System.Object
	{
		.method public static hidebysig
			default void Assert (bool condition, [opt] string caller) cil managed
		{
			.param [2] = nullref
			.custom instance void class [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::'.ctor'() =  (01 00 00 00 ) // ....

			.maxstack 8
			ldarg.0
			brfalse.s label1
			ret

		label1:
			ldarg.1
			call bool string::IsNullOrEmpty(string)
			brtrue.s label2

			ldstr "Assertion failed at {0}"
			ldarg.1
			call string string::Format(string, object)
			newobj instance void class Martin.LinkerTest.AssertionException::'.ctor'(string)
			throw

		label2:
			ldstr "Assertion failed"
			newobj instance void class Martin.LinkerTest.AssertionException::'.ctor'(string)
			throw
		}

		.method public static hidebysig
			default void AssertFail (string message, [opt] string caller) cil managed
		{
			.param [2] = nullref
			.custom instance void class [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::'.ctor'() =  (01 00 00 00 ) // ....

			.maxstack 8
			ldarg.1
			call bool string::IsNullOrEmpty(string)
			brtrue.s label1

			ldstr "Assertion failed at {0}: {1}"
			ldarg.1
			ldarg.0
			call string string::Format(string, object, object)
			newobj instance void class Martin.LinkerTest.AssertionException::'.ctor'(string)
			throw

		label1:
			ldstr "Assertion failed: {0}"
			ldarg.0
			call string string::Format(string, object)
			newobj instance void class Martin.LinkerTest.AssertionException::'.ctor'(string)
			throw
		}

		.method public static hidebysig
			default class [mscorlib]System.Exception AssertRemoved () cil managed
		{
			.maxstack 8
			ldstr "This code should have been removed."
			newobj instance void class Martin.LinkerTest.AssertionException::'.ctor'(string)
			throw
		}

		.method public static hidebysig
			default void Debug ([opt] string caller) cil managed
		{
			.param [1] = nullref
			.custom instance void class [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::'.ctor'() =  (01 00 00 00 ) // ....

			.maxstack 8
			ldarg.0
			call bool string::IsNullOrEmpty(string)
			brtrue.s label1

			call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
			ldstr "TEST DEBUG AT {0}"
			ldarg.0
			call string string::Format(string, object)
			callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(string)
			ret

		label1:
			call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
			ldstr "TEST DEBUG"
			callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(string)
			ret
		}

		.method public static hidebysig
			default void Debug (string message, [opt] string caller) cil managed
		{
			.param [2] = nullref
			.custom instance void class [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::'.ctor'() =  (01 00 00 00 ) // ....

			.maxstack 8
			ldarg.1
			call bool string::IsNullOrEmpty(string)
			brtrue.s label1

			call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
			ldstr "TEST DEBUG AT {0}: {1}"
			ldarg.1
			ldarg.0
			call string string::Format(string, object, object)
			callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(string)
			ret

		label1:
			call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
			ldstr "TEST DEBUG: {0}"
			ldarg.0
			call string string::Format(string, object)
			callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(string)
			ret
		}
	}
}
