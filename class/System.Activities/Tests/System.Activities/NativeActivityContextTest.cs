using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	class NativeActivityContextTest {
		// cant instantiate NativeActivityContext, has internal ctor

		void Run (Action<NativeActivityMetadata> metadata, Action<NativeActivityContext> execute)
		{
			var testBed = new NativeRunnerMock (metadata, execute);
			WorkflowInvoker.Invoke (testBed);
		}

		[Test]
		public void GetChildren ()
		{
			var writeLine1 = new WriteLine ();
			var writeLine2 = new WriteLine ();
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddChild (writeLine1);
				metadata.AddImplementationChild (writeLine2);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				var children = context.GetChildren ();
				Assert.IsNotNull (children);
				Assert.AreEqual (0, children.Count);
				context.ScheduleActivity (writeLine1);
				children = context.GetChildren ();
				Assert.AreEqual (1, children.Count);
				Assert.AreSame (writeLine1, children [0].Activity);
			};
			Run (metadataAction, executeAction);
		}

	}

	class NativeActivityContextTestSuite {

		public BookmarkScope DefaultBookmarkScope { get { throw new NotImplementedException (); } }
		public bool IsCancellationRequested { get { throw new NotImplementedException (); } }
		public ExecutionProperties Properties { get { throw new NotImplementedException (); } }

		public void Abort ()
		{
			throw new NotImplementedException ();
		}
		public void AbortChildInstance ()
		{
			throw new NotImplementedException ();
		}
		public void CancelChild ()
		{
			throw new NotImplementedException ();
		}
		public void CancelChildren ()
		{
			throw new NotImplementedException ();
		}

		// lots of Bookmark related functions

		public void GetChildren ()
		{
			throw new NotImplementedException ();
		}
		public void GetValue_Variable ()
		{
			throw new NotImplementedException ();
		}
		public void GetValueT_VariableT ()
		{
			throw new NotImplementedException ();
		}
		public void MarkCanceled ()
		{
			throw new NotImplementedException ();
		}
		
		// lots more ScheduleAction overrides

		public void ScheduleActivity ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivity_CompletionCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivity_FaultCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivity_CompletionCallback_FaultCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivityT_CompletionCallbackT_FaultCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleDelegate_IDictionary_DelegateCompletionCallback_FaultCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleFuncT_CompletionCallback_FaultCallback ()
		{
			throw new NotImplementedException ();
		}

		// lots more Schedule overrides

		public void SetValue_Variable ()
		{
			throw new NotImplementedException ();
		}
		public void SetValueT_VariableT ()
		{
			throw new NotImplementedException ();
		}
		public void Track ()
		{
			throw new NotImplementedException ();
		}
	}

}
