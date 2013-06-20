using System;
using NUnit.Framework;
using System.Threading;
using System.Activities;
using System.Collections.Generic;
using System.Activities.Hosting;

namespace Tests.System.Activities {
	[TestFixture]
	public class WorkflowApplicationTest {
		[Test]
		[Ignore ("WorkflowApplication")]
		public void WorkflowApplication_Ctor_Activity_IDictionary ()
		{
			var syncEvent = new AutoResetEvent (false);

			var app = new WorkflowApplication (new Concat (), 
			                                   new Dictionary<string, object> {{"String1", "Hello\n"},
											   {"String2", "World"}});
			String msg = null;
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
			app.Run ();
			syncEvent.WaitOne();
			Assert.AreEqual ("Hello\nWorld", msg);
		}
		[Test]
		[Ignore ("WorkflowApplication")]
		public void WorkflowApplication_PassingArgsBySymbolResolver_DoesntWork ()
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

