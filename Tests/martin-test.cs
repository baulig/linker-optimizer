using System;
using Martin.LinkerTest;

public class Foo
{
	public static void Hello ()
	{
		Console.WriteLine ("Hello World!");
	}
}

class X
{
	static int Main()
	{
		SimpleTests.RunAll ();
		return 0;
	}
}
