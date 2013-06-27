using System;
using NUnit.Framework;
using System.Threading;
using System.Activities;
using System.Collections.Generic;
using System.Activities.Hosting;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	public class WorkflowApplicationTest {
		[Test]
		public void Ctor_Activity ()
		{
			var syncEvent = new AutoResetEvent (false);
			var wf = new Concat { String1 = "Hello\n", String2 = "World" };
			var app = new WorkflowApplication (wf);
			String msg = null;
			app.Completed = (WorkflowApplicationCompletedEventArgs e) => {
				if (e.CompletionState == ActivityInstanceState.Closed)
					msg = (string) e.Outputs ["Result"];
				syncEvent.Set();
			};

			Assert.IsNull (app.HostEnvironment);
			Assert.AreNotEqual (Guid.Empty, app.Id);
			Assert.AreSame (wf, app.WorkflowDefinition);
			app.Run ();
			syncEvent.WaitOne ();
			Assert.AreEqual ("Hello\nWorld", msg);
			Assert.IsNull (app.HostEnvironment);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor_Activity_NullEx ()
		{
			new WorkflowApplication (null);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor_Activity_IDictionary_NullActivityEx ()
		{
			new WorkflowApplication (null, new Dictionary<string, object> ());
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor_Activity_IDictionary_NullDictionaryEx ()
		{
			new WorkflowApplication (new Concat (), null);
		}
		[Test]
		public void Ctor_Activity_IDictionary_InvalidOK ()
		{
			var wf = new WriteLine ();
			new WorkflowApplication (wf, new Dictionary<string, object> () { {"Text", "Hello\nWorld"},
											 {"Text2", "Hello\nWorld"} });
		}
		//FIXME: the same testing that is done for WorkflowInvoker.Invoke(Activity. IDictionary) could be applicable here
		[Test, ExpectedException (typeof (ArgumentException))]
		public void InvalidInputArgs_OnRunEx ()
		{
			/*System.ArgumentException : The values provided for the root activity's arguments did not satisfy the root activity's requirements:
			 *'WriteLine': The following keys from the input dictionary do not map to arguments and must be removed: Text2.  Please note that 
			 *argument names are case sensitive.
			 *Parameter name: rootArgumentValues
			*/
			var wf = new WriteLine ();
			var app = new WorkflowApplication (wf, new Dictionary<string, object> () { {"Text", "Hello\nWorld"},
												   {"Text2", "Hello\nWorld"} });
			app.Run ();
		}
		[Test]
		public void Ctor_Activity_IDictionary ()
		{
			var syncEvent = new AutoResetEvent (false);
			var wf = new Concat ();
			var app = new WorkflowApplication (wf, 
			                                   new Dictionary<string, object> {{"String1", "Hello\n"},
											   {"String2", "World"}});
			String msg = null;
			app.Completed = (WorkflowApplicationCompletedEventArgs e) => {
				if (e.CompletionState == ActivityInstanceState.Closed)
					msg = (string) e.Outputs ["Result"];

				syncEvent.Set();
			};

			Assert.IsNull (app.HostEnvironment);
			Assert.AreNotEqual (Guid.Empty, app.Id);
			Assert.AreSame (wf, app.WorkflowDefinition);
			app.Run ();
			syncEvent.WaitOne ();
			Assert.AreEqual ("Hello\nWorld", msg);
			Assert.IsNull (app.HostEnvironment);
		}
		[Test]
		public void Completed ()
		{
			var syncEvent = new AutoResetEvent (false);
			var wf = new Concat { String1 = "Hello\n", String2 = "World" };
			var app = new WorkflowApplication (wf);
			WorkflowApplicationCompletedEventArgs args = null;
			app.Completed = (e) => {
				args = e;
				syncEvent.Set();
			};

			app.Run ();
			syncEvent.WaitOne ();
			Assert.AreEqual (1, args.Outputs.Count);
			Assert.AreEqual ("Hello\nWorld", args.Outputs ["Result"]);
			Assert.AreEqual (ActivityInstanceState.Closed, args.CompletionState);
			Assert.AreEqual (app.Id, args.InstanceId);
			Assert.IsNull (args.TerminationException);
		}
		//FIXME: also test returning UnhandledExceptionAction.Cancel, UnhandledExceptionAction.Abort
		[Test]
		public void OnUnhandledException ()
		{
			var ex = new InvalidOperationException ("Hello\nException");
			var exActivity = new CodeActivityRunner (null, (context) => {
				throw ex;
			});
			var wf = new Sequence { Activities = {
					exActivity
				}
			};
			var reset = new AutoResetEvent (false);
			WorkflowApplicationUnhandledExceptionEventArgs args = null;
			var app = new WorkflowApplication (wf);
			app.OnUnhandledException = (e) => {
				args = e;
				reset.Set ();
				return UnhandledExceptionAction.Terminate;
			};
			app.Run ();
			reset.WaitOne ();
			//FIXME: test WorkflowApplicationEventArgs?.GetInstanceExtensions()?
			Assert.AreEqual (ex, args.UnhandledException);
			Assert.AreEqual (exActivity, args.ExceptionSource);
			Assert.AreEqual ("2", args.ExceptionSourceInstanceId);
			Assert.AreEqual (app.Id, args.InstanceId);
		}
		[Test]
		public void PassingArgsBySymbolResolver_DoesntWork ()
		{
			var syncEvent = new AutoResetEvent (false);

			var sr = new SymbolResolver ();
			sr.Add ("String1", "Hello\n");
			sr.Add ("String2", "World");

			String msg = null;

			var app = new WorkflowApplication (new Concat ());

			app.Completed = (WorkflowApplicationCompletedEventArgs e) => {
				if (e.CompletionState == ActivityInstanceState.Faulted)
					msg = "WF Faulted";
				else if (e.CompletionState == ActivityInstanceState.Canceled)
					msg = "WF was cancelled";
				else
					msg = (string) e.Outputs ["Result"];

				syncEvent.Set();
			};
			app.Aborted = (WorkflowApplicationAbortedEventArgs e) => {
				msg = "I Aborted! " + e.Reason.Message;
				syncEvent.Set();
			};

			Assert.IsNull (app.HostEnvironment);
			app.HostEnvironment = sr.AsLocationReferenceEnvironment ();
			app.Run ();
			syncEvent.WaitOne();
			Assert.AreEqual ("Hello\nWorld", msg);
		}
	}
}

