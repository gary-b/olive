using System;
using NUnit.Framework;
using System.Activities;

namespace Tests.System.Activities {
	[TestFixture]
	public class RuntimeDelegateArgumentTest {
		[Test]
		[Ignore ("DelegateOutArgument")]
		public void Ctor_name_type_direction_boundArgument ()
		{
			var da = new DelegateInArgument<string> ();
			var rda = new RuntimeDelegateArgument ("aname", typeof (string), 
								ArgumentDirection.In, da);
			Assert.AreEqual ("aname", rda.Name);
			Assert.AreEqual (typeof (string), rda.Type);
			Assert.AreEqual (ArgumentDirection.In, rda.Direction);
			Assert.AreEqual (da, rda.BoundArgument);
			// null bound arg ok
			var noBoundArg = new RuntimeDelegateArgument ("aname", typeof (string), 
			                                       ArgumentDirection.In, null);
			//DelegateInArgument different name than RuntimeDelegateArgument 
			var daName = new DelegateInArgument<string> ("first");
			var rdaName = new RuntimeDelegateArgument ("second", typeof (string), 
			                                            ArgumentDirection.In, daName);
			Assert.AreEqual ("second", rdaName.Name);
			//DelegateInArgument different type than RuntimeDelegateArgument
			var daType = new DelegateInArgument<int> ();
			var rdaType = new RuntimeDelegateArgument ("aname", typeof (string), 
			                                            ArgumentDirection.In, daType);
			Assert.AreEqual (typeof (string), rdaType.Type);
			//DelegateInArgument different direction than RuntimeDelegateArgument 
			var daDir = new DelegateOutArgument<string> ();
			var rdaDir = new RuntimeDelegateArgument ("aname", typeof (string), 
								ArgumentDirection.In, daDir);
			Assert.AreEqual (ArgumentDirection.In, rdaDir.Direction);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void Ctor_name_type_direction_boundArgument_NameNullEx ()
		{
			var da = new DelegateInArgument<string> ();
			var rda = new RuntimeDelegateArgument (null, typeof (string), 
								ArgumentDirection.In, da);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void Ctor_name_type_direction_boundArgument_NameEmptyEx ()
		{
			var da = new DelegateInArgument<string> ();
			var rda = new RuntimeDelegateArgument (String.Empty, typeof (string), 
								ArgumentDirection.In, da);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor_name_type_direction_boundArgument_TypeNullEx ()
		{
			var da = new DelegateInArgument<string> ();
			var rda = new RuntimeDelegateArgument ("aname", null, 
								ArgumentDirection.In, da);
		}
		/* readonly properties tested in ctor
		public void BoundArgument ()
		public void Direction ()
		public void Name ()
		public void Type ()
		*/
	}
}

