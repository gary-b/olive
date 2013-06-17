using System;
using NUnit.Framework;
using System.Activities.Expressions;
using System.Activities;

namespace Tests.System.Activities.Expressions {
	[TestFixture]
	public class ArgumentReferenceT_Test {
		[Test]
		public void Ctor ()
		{
			var avStr = new ArgumentReference<string> ();
			Assert.IsNull (avStr.ArgumentName);
			Assert.AreSame (typeof (Location<string>), avStr.ResultType);

			var avInt = new ArgumentReference<int> ();
			Assert.AreSame (typeof (Location<int>), avInt.ResultType);
		}
		[Test]
		public void Ctor_string ()
		{
			var avStr = new ArgumentReference<string> ("arg1");
			Assert.AreEqual ("arg1", avStr.ArgumentName);
			Assert.AreSame (typeof (Location<string>), avStr.ResultType);

			var avInt = new ArgumentReference<int> ("arg2");
			Assert.AreSame (typeof (Location<int>), avInt.ResultType);
		}
		[Test]
		public void ArgumentName ()
		{
			var avStr = new ArgumentReference<string> ();
			Assert.IsNull (avStr.ArgumentName);
			avStr.ArgumentName = "arg1";
			Assert.AreEqual ("arg1", avStr.ArgumentName);
			avStr.ArgumentName = null;
			Assert.IsNull (avStr.ArgumentName);
		}
		[Test]
		public void ToStringTest ()
		{
			var avStr = new ArgumentReference<string> ();
			Assert.AreEqual (": ArgumentReference<String>", avStr.ToString ());

			var avStr2 = new ArgumentReference<string> ("avStr2");
			Assert.AreEqual ("avStr2", avStr2.ToString ());

			avStr2.ArgumentName = null;
			Assert.AreEqual (": ArgumentReference<String>", avStr2.ToString ());
			avStr2.ArgumentName = "Bob";
			Assert.AreEqual ("Bob", avStr2.ToString ());

			//demonstrate no relation to DisplayName
			Assert.AreEqual ("ArgumentReference<String>", avStr2.DisplayName); 
			avStr2.DisplayName = "Disp";
			Assert.AreEqual ("Bob", avStr2.ToString ());
		}
		/* Tested in ArgumentHandlingRuntimeTest
		public void Execute ()
		*/
	}
}

