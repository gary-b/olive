using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	[TestFixture]
	public class ActivityDelegateTest {
		class ActivityDelegateMock : ActivityDelegate {
			public new DelegateOutArgument GetResultArgument ()
			{
				return base.GetResultArgument ();
			}
			public new void OnGetRuntimeDelegateArguments (IList<RuntimeDelegateArgument> runtimeDelegateArguments)
			{
				base.OnGetRuntimeDelegateArguments (runtimeDelegateArguments);
			}
		}
		class ActivityDelegateWithOutArgMock : ActivityDelegateMock {
			public DelegateOutArgument Result { get; set; }
		}
		class ActivityDelegateWithInOutArgMock : ActivityDelegateWithOutArgMock {
			public DelegateInArgument InArg { get; set; }
			public DelegateInArgument<string> InArgString { get; set; }
			public DelegateOutArgument<string> OutArgString { get; set; }
		}
		//FIXME: add test to check IOE is thrown on changing DisplayName  / Handler at runtime
		[Test]
		public void DisplayName ()
		{
			var del = new ActivityDelegateMock ();
			Assert.AreEqual ("ActivityDelegateMock", del.DisplayName);
			del.DisplayName = "Geronimo";
			Assert.AreEqual ("Geronimo", del.DisplayName);
		}
		[Test]
		public void Handler ()
		{
			var del = new ActivityDelegateMock ();
			Assert.IsNull (del.Handler);
			var writeLine = new WriteLine ();
			del.Handler = writeLine;
			Assert.AreSame (writeLine, del.Handler);
		}
		[Test]
		public void GetResultArgument ()
		{
			var del = new ActivityDelegateMock ();
			Assert.IsNull (del.GetResultArgument ());
			// check reflection isnt use to detect arg
			var delOut = new ActivityDelegateWithOutArgMock ();
			delOut.Result = new DelegateOutArgument<int> ();
			Assert.IsNull (delOut.GetResultArgument ());
		}
		[Test]
		public void OnGetRuntimeDelegateArguments ()
		{
			var del = new ActivityDelegateMock ();
			var runDelArgs = new List<RuntimeDelegateArgument> ();
			del.OnGetRuntimeDelegateArguments (runDelArgs);
			Assert.AreEqual (0, runDelArgs.Count);
			// arguments detected automatically
			// uninitialised delegateInArg
			var delOutUnInit = new ActivityDelegateWithOutArgMock ();
			var runDelArgsUnInit = new List<RuntimeDelegateArgument> ();
			delOutUnInit.OnGetRuntimeDelegateArguments (runDelArgsUnInit);
			Assert.AreEqual (1, runDelArgsUnInit.Count);
			Assert.AreEqual ("Result", runDelArgsUnInit [0].Name);
			Assert.IsNull (runDelArgsUnInit [0].BoundArgument);
			Assert.AreEqual (ArgumentDirection.Out, runDelArgsUnInit [0].Direction);
			Assert.AreEqual (typeof (Object), runDelArgsUnInit [0].Type);
			// delegateInArg initialised with delegateInArg<T>
			var delOutInit = new ActivityDelegateWithOutArgMock ();
			var runDelArgsInit = new List<RuntimeDelegateArgument> ();
			delOutInit.Result = new DelegateOutArgument<int> ();
			delOutInit.OnGetRuntimeDelegateArguments (runDelArgsInit);
			Assert.AreEqual (1, runDelArgsInit.Count); 
			Assert.AreEqual ("Result", runDelArgsInit [0].Name);
			Assert.AreSame (delOutInit.Result, runDelArgsInit [0].BoundArgument);
			Assert.AreEqual (ArgumentDirection.Out, runDelArgsInit [0].Direction);
			Assert.AreEqual (typeof (Object), runDelArgsInit [0].Type); // still object
			// appends to collection with existing RuntimeDelegateArguments
			var runDelArgsExist = new List<RuntimeDelegateArgument> ();
			runDelArgsExist.Add (new RuntimeDelegateArgument("bob", typeof (object), ArgumentDirection.In, null));
			delOutInit.OnGetRuntimeDelegateArguments (runDelArgsExist);
			Assert.AreEqual (2, runDelArgsExist.Count);
		}
		[Test]
		public void OnGetRuntimeDelegateArguments_Detection ()
		{
			var delInOutArg = new ActivityDelegateWithInOutArgMock ();
			var runDelArgs = new List<RuntimeDelegateArgument> ();
			delInOutArg.OnGetRuntimeDelegateArguments (runDelArgs);
			Assert.AreEqual (4, runDelArgs.Count); // Parent activities result arg ignored?
			Assert.AreEqual ("InArg", runDelArgs [0].Name);
			Assert.AreEqual (ArgumentDirection.In, runDelArgs [0].Direction);
			Assert.AreEqual (typeof (object), runDelArgs [0].Type);
			Assert.AreEqual ("InArgString", runDelArgs [1].Name);
			Assert.AreEqual (ArgumentDirection.In, runDelArgs [1].Direction);
			Assert.AreEqual (typeof (string), runDelArgs [1].Type);
			Assert.AreEqual ("OutArgString", runDelArgs [2].Name);
			Assert.AreEqual (ArgumentDirection.Out, runDelArgs [2].Direction);
			Assert.AreEqual (typeof (string), runDelArgs [2].Type);
			Assert.AreEqual ("Result", runDelArgs [3].Name);
			Assert.AreEqual (ArgumentDirection.Out, runDelArgs [3].Direction);
			Assert.AreEqual (typeof (Object),  runDelArgs [3].Type);
		}
		[Test]
		public void ShouldSerializeDisplayName ()
		{
			var del = new ActivityDelegateMock ();
			string name = del.DisplayName;
			Assert.IsFalse (del.ShouldSerializeDisplayName ());
			del.DisplayName = "Geronimo";
			Assert.IsTrue (del.ShouldSerializeDisplayName ());
			del.DisplayName = name;
			Assert.IsTrue (del.ShouldSerializeDisplayName ());
		}
		[Test]
		public void ToStringTest ()
		{
			var del = new ActivityDelegateMock ();
			Assert.AreEqual (del.DisplayName, del.ToString ());
			del.DisplayName = "Geronimo";
			Assert.AreEqual (del.DisplayName, del.ToString ());
		}
	}
}

