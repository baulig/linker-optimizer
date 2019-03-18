using System;

namespace Martin.LinkerTest
{
	class AssertionException : Exception
	{
		public AssertionException ()
		{ }

		public AssertionException (string message)
		 : base (message)
		{ }
	}
}
