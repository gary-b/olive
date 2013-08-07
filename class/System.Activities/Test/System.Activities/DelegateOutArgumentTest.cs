using System;
using NUnit.Framework;
using System.Activities;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class DelegateOutArgumentT_Test : WFTestHelper {
		//DelegateArgument members tested in DelegateInArgumentT_Test
		[Test]
		public void Ctor ()
		{
			var da = new DelegateOutArgument<string> ();
			Assert.AreEqual (ArgumentDirection.Out, da.Direction);
			Assert.IsNull (da.Name);
			Assert.AreEqual (typeof (string), da.Type);
		}
		[Test]
		public void Ctor_Name ()
		{
			var da = new DelegateOutArgument<string> ("Bob");
			Assert.AreEqual (ArgumentDirection.Out, da.Direction);
			Assert.AreEqual ("Bob", da.Name);
			Assert.AreSame (da.Name, ((LocationReference) da).Name);
			Assert.AreEqual (typeof (string), da.Type);
		}
		[Test]
		[Ignore ("Not Implemeneted")]
		public void TGet ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemeneted")]
		public void Set ()
		{
			throw new NotImplementedException ();
		}
		//TypeCore is protected
	}
}

