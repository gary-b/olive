using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;

namespace Tests.System.Activities {
	public class DelegateInArgumentTest {
		// presumably sets ArgumentDirection Direction property
	}
	[TestFixture]
	public class DelegateInArgumentT_Test : WFTest {
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
		public void Direction ()
		*/
		//FIXME: check there are tests for all hidden props
		[Test]
		public void New_Name ()
		{
			var da = new DelegateInArgument<string> ();
			Assert.IsNull (da.Name);
			Assert.AreSame (da.Name, ((LocationReference) da).Name);
			da.Name = "Bob";
			Assert.AreEqual ("Bob", da.Name);
			Assert.AreSame (da.Name, ((LocationReference) da).Name);
			da.Name = null;
			Assert.IsNull (da.Name);
			Assert.AreSame (da.Name, ((LocationReference) da).Name);
		}
		[Test]
		[Ignore ("Not Implemeneted")]
		public void NameCore ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemeneted")]
		public void DelegateArgument_OGet ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemeneted")]
		public void GetLocation ()
		{
			throw new NotImplementedException ();
		}
		#endregion

		[Test]
		[Ignore ("Expressions")]
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
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1");
			});
			RunAndCompare (wf, "1" + Environment.NewLine);
		}
		[Test]
		[Ignore ("Not Implemeneted")]
		public void Set ()
		{
			throw new NotImplementedException ();
		}
		// Type from LocationReference not tested
		[Test]
		[Ignore ("Not Implemeneted")]
		public void TypeCore()
		{
			throw new NotImplementedException ();
		}
	}
}

