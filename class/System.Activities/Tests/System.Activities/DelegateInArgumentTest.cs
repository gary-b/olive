using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;

namespace Tests.System.Activities {
	[TestFixture]
	public class DelegateInArgumentTest {
		// presumably sets ArgumentDirection Direction property
	}
	[TestFixture]
	public class DelegateInArgumentT_Test {
		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}
		[Test]
		public void Ctor ()
		{
			var da = new DelegateInArgument<string> ();
			Assert.AreEqual (ArgumentDirection.In, da.Direction);
			Assert.IsNull (da.Name);
			Assert.AreEqual (typeof (string), da.Type);
		}
		[Test]
		public void Ctor_Name ()
		{
			var da = new DelegateInArgument<string> ("Bob");
			Assert.AreEqual (ArgumentDirection.In, da.Direction);
			Assert.AreEqual ("Bob", da.Name);
			Assert.AreEqual (typeof (string), da.Type);
		}
		#region From DelegateArgument
		/*tested in ctor
		[Test]
		public void Direction ()
		{
		}*/
		[Test]
		public void New_Name ()
		{
			var da = new DelegateInArgument<string> ();
			Assert.IsNull (da.Name);
			da.Name = "Bob";
			Assert.AreEqual ("Bob", da.Name);
			da.Name = null;
			Assert.IsNull (da.Name);
		}
		//FIXME: have i tests for other hidden props?
		[Test]
		public void LocationReference_Name () 
		{
			var da = new DelegateInArgument<string> ();
			Assert.IsNull (((LocationReference) da).Name);
			da.Name = "Bob";
			Assert.AreEqual ("Bob", ((LocationReference) da).Name);
			da.Name = null;
			Assert.IsNull (((LocationReference) da).Name);
		}
		/*protected
		[Test]
		public void NameCore ()
		{
			throw new NotImplementedException ();
		}*/
		[Test]
		public void DelegateArgument_OGet ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void GetLocation ()
		{
			throw new NotImplementedException ();
		}
		#endregion
		[Test]
		public void TGet ()
		{
			// FIXME: complicated and requires Expressions args to be implemented
			var argStr = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string> {
				Argument = argStr,
				Handler = new WriteLine { 
					Text = new InArgument<string> ((context) => argStr.Get (context)),
				}
			};
			
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1");
			});
			RunAndCompare (wf, "1" + Environment.NewLine);
		}
		[Test]
		public void Set ()
		{
			throw new NotImplementedException ();
		}
		// Type from LocationReference not tested
		/*protected
		[Test]
		public void TypeCore()
		{
			throw new NotImplementedException ();
		}*/
	}
}

