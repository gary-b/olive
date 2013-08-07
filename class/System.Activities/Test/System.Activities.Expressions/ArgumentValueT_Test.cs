using System;
using NUnit.Framework;
using System.Activities.Expressions;
using System.Activities;

namespace MonoTests.System.Activities.Expressions {
	[TestFixture]
	public class ArgumentValueT_Test : WFTestHelper {
		[Test]
		public void Ctor ()
		{
			var avStr = new ArgumentValue<string> ();
			Assert.IsNull (avStr.ArgumentName);
			Assert.AreSame (typeof (string), avStr.ResultType);

			var avInt = new ArgumentValue<int> ();
			Assert.AreSame (typeof (int), avInt.ResultType);
		}
		[Test]
		public void Ctor_string ()
		{
			var avStr = new ArgumentValue<string> ("arg1");
			Assert.AreEqual ("arg1", avStr.ArgumentName);
			Assert.AreSame (typeof (string), avStr.ResultType);

			var avInt = new ArgumentValue<int> ("arg2");
			Assert.AreSame (typeof (int), avInt.ResultType);

			var avStr2 = new ArgumentValue<int> (null);
			Assert.IsNull (avStr2.ArgumentName);
		}
		[Test]
		public void ArgumentName ()
		{
			var avStr = new ArgumentValue<string> ();
			Assert.IsNull (avStr.ArgumentName);
			avStr.ArgumentName = "arg1";
			Assert.AreEqual ("arg1", avStr.ArgumentName);
			avStr.ArgumentName = null;
			Assert.IsNull (avStr.ArgumentName);
		}
		[Test]
		[Ignore ("ToString fails on generics issue")]
		public void ToStringTest ()
		{
			var avStr = new ArgumentValue<string> ();
			Assert.AreEqual (": ArgumentValue<String>", avStr.ToString ());

			var avStr2 = new ArgumentValue<string> ("avStr2");
			Assert.AreEqual ("avStr2", avStr2.ToString ());

			avStr2.ArgumentName = null;
			Assert.AreEqual (": ArgumentValue<String>", avStr2.ToString ());
			avStr2.ArgumentName = String.Empty;
			Assert.AreEqual (": ArgumentValue<String>", avStr2.ToString ());
			avStr2.ArgumentName = "  ";
			Assert.AreEqual ("  ", avStr2.ToString ());
			avStr2.ArgumentName = "Bob";
			Assert.AreEqual ("Bob", avStr2.ToString ());

			//demonstrate no relation to DisplayName when argName set
			Assert.AreEqual ("ArgumentValue<String>", avStr2.DisplayName); 
			avStr2.DisplayName = "Disp";
			Assert.AreEqual ("Bob", avStr2.ToString ());
			//but there is when its not
			avStr2.ArgumentName = null;
			Assert.AreEqual (": Disp", avStr2.ToString ());
		}
		/* Tested in ArgumentHandlingRuntimeTest
		public void Execute ()
		*/
	}
}

