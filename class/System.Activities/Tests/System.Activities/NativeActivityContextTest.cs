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
		[Ignore ("GetChildren")]
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

		[Test]
		public void SetValue_Variable_GetValue_Variable ()
		{
			var vStr = new Variable<string> ();
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddImplementationVariable (vStr);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				Assert.AreEqual (null, context.GetValue ((Variable) vStr));
				context.SetValue ((Variable) vStr, "newVal");
				Assert.AreEqual ("newVal", context.GetValue ((Variable) vStr));
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (metadataAction, executeAction));
		}

		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void SetValue_Variable_NullEx ()
		{
			Action<NativeActivityContext> executeAction = (context) => {
				context.SetValue ((Variable) null, "newVal");
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
		}

		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValue_Variable_NullEx ()
		{
			Action<NativeActivityContext> executeAction = (context) => {
				context.GetValue ((Variable) null);
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValue_Variable_NotDeclaredEx ()
		{
			//Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();
			Action<NativeActivityContext> executeAction = (context) => {
				context.SetValue ((Variable)vStr, "newVal");
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValue_Variable_NotDeclaredEx ()
		{
			// Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();
			Action<NativeActivityContext> executeAction = (context) => {
				context.GetValue ((Variable) vStr);
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
		}

		[Test]
		public void SetValueT_VariableT_GetValueT_VariableT ()
		{
			var vStr = new Variable<string> ();
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddImplementationVariable (vStr);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				Assert.AreEqual (null, context.GetValue<string> (vStr));
				context.SetValue<string> (vStr, "newVal");
				Assert.AreEqual ("newVal", context.GetValue<string> (vStr));
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (metadataAction, executeAction));
		}

		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void SetValueT_VariableT_NullEx ()
		{
			Action<NativeActivityContext> executeAction = (context) => {
				context.SetValue<string> ((Variable<string>) null, "newVal");
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
		}

		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValueT_VariableT_NullEx ()
		{
			Action<NativeActivityContext> executeAction = (context) => {
				context.GetValue<string> ((Variable<string>) null);
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValueT_VariableT_NotDeclaredEx ()
		{
			//Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();
			Action<NativeActivityContext> executeAction = (context) => {
				context.SetValue<string> (vStr, "newVal");
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValueT_VariableT_NotDeclaredEx ()
		{
			//Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();
			Action<NativeActivityContext> executeAction = (context) => {
				context.GetValue<string> (vStr);
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (null, executeAction));
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


		public void Track ()
		{
			throw new NotImplementedException ();
		}
	}

}
