using System;
using NUnit.Framework;
using System.IO;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;

namespace Tests.System.Activities.Expressions {
	[TestFixture]
	public class DelegateArgumentValueT_Test : WFTestHelper {

		#region Ctors
		[Test]
		public void Ctor ()
		{
			var dav = new DelegateArgumentValue<string> ();
			Assert.IsNull (dav.DelegateArgument);
		}
		[Test]
		public void Ctor_DelegateArgument ()
		{
			var da = new DelegateInArgument<string> ();
			var dav = new DelegateArgumentValue<string> (da);
			Assert.AreSame (da, dav.DelegateArgument);
			
			// .NET does not throw error when type param of DelegateInArgument clashes with that of DAV
			//FIXME: does WF validate during wf execution / what would happen?
			var daInt = new DelegateInArgument<int> ();
			var davStr = new DelegateArgumentValue<string> (daInt);
			Assert.AreSame (daInt, davStr.DelegateArgument);
			// .NET doesnt throw error on null param
			var dav3 = new DelegateArgumentValue<string> (null);
		}
		#endregion
		
		#region Properties
		[Test]
		public void DelegateArgument ()
		{
			var daStr = new DelegateInArgument<string> ();
			var daInt = new DelegateInArgument<int> ();
			var dav = new DelegateArgumentValue<string> (daStr);
			Assert.AreSame (daStr, dav.DelegateArgument);
			
			dav.DelegateArgument = daInt;
			Assert.AreSame (daInt, dav.DelegateArgument);
			
			dav.DelegateArgument = null;
			Assert.IsNull (dav.DelegateArgument);
		}
		#endregion
		
		#region Methods
		[Test]
		[Ignore ("ToString fails on generics issue")]
		public void ToStringTest ()
		{
			var daStr = new DelegateInArgument<string> ("aStr");
			var daInt = new DelegateInArgument<int> ("anInt");

			var dav = new DelegateArgumentValue<string> (daStr);
			Assert.AreEqual (": DelegateArgumentValue<String>", dav.ToString ());
			
			dav.DelegateArgument = daInt;
			Assert.AreEqual (": DelegateArgumentValue<String>", dav.ToString ());
			
			var davNoArg = new DelegateArgumentValue<string> ();
			Assert.AreEqual (": DelegateArgumentValue<String>", davNoArg.ToString ());

		}
		[Test]
		public void Execute () //protected
		{
			//FIXME: convoluted test for this, dupe of ActivityActionT_Test

			var argStr = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string> {
				Argument = argStr,
				Handler = new WriteLine { 
					Text = new InArgument<string> (new DelegateArgumentValue<string> (argStr))
				}
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1");
			});
			RunAndCompare (wf, "1" + Environment.NewLine);
		}
		#endregion
	}
}

