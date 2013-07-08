using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Transactions;
using System.Windows.Markup;
using System.Xaml;
using System.Xml.Linq;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Activities.Tracking;
using System.Activities.Validation;

namespace System.Activities
{
	// BE CAREFUL TO RAISE APPROPRIATE EVENTS WHEN WORKFLOW CONTROL METHODS INVOKED
	public sealed class WorkflowApplication : WorkflowInstance
	{
		public WorkflowApplication (Activity workflowDefinition)
			: base (workflowDefinition)
		{
			id = Guid.NewGuid ();
		}

		public WorkflowApplication (Activity workflowDefinition, IDictionary<string, Object> inputs)
			: this (workflowDefinition)
		{
			if (inputs == null)
				throw new ArgumentNullException ("inputs");
			Inputs = inputs;
		}

		Guid id;
		public override Guid Id { 
			get { 
				return id; 
			} 
		}
		public Action<WorkflowApplicationAbortedEventArgs> Aborted { get; set; }
		public Action<WorkflowApplicationCompletedEventArgs> Completed { get; set; }
		public WorkflowInstanceExtensionManager Extensions { get { throw new NotImplementedException (); } }
		public Action<WorkflowApplicationIdleEventArgs> Idle { get; set; }
		public InstanceStore InstanceStore { get; set; }
		public Func<WorkflowApplicationUnhandledExceptionEventArgs, UnhandledExceptionAction> OnUnhandledException { get; set; }
		public Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction> PersistableIdle { get; set; }
		public Action<WorkflowApplicationEventArgs> Unloaded { get; set; }
		IDictionary<string, object> Inputs { get; set; }

		protected internal override bool SupportsInstanceKeys {
			get { throw new NotImplementedException (); }
		}

		public void Abort (string reason)
		{
			throw new NotImplementedException ();
		}
		public void AddInitialInstanceValues (IDictionary<XName, Object> writeOnlyValues)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginCancel (AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginCancel (TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginLoad (Guid instanceId, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginLoad (Guid instanceId, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginLoadRunnableInstance (AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginLoadRunnableInstance (TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginPersist (AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginPersist (TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginResumeBookmark (Bookmark bookmark, object value, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginResumeBookmark (string bookmarkName, object value, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginResumeBookmark (Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginResumeBookmark (string bookmarkName, object value, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginRun (AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginRun (TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginTerminate (Exception reason, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginTerminate (string reason, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginTerminate (Exception reason, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginTerminate (string reason, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginUnload (AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginUnload (TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		public void Cancel (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void EndCancel (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public void EndLoad (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public void EndLoadRunnableInstance (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public void EndPersist (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public BookmarkResumptionResult EndResumeBookmark (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public void EndRun (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public void EndTerminate (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public void EndUnload (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public ReadOnlyCollection<BookmarkInfo> GetBookmarks ()
		{
			throw new NotImplementedException ();
		}
		public ReadOnlyCollection<BookmarkInfo> GetBookmarks (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void Load (Guid instanceId)
		{
			throw new NotImplementedException ();
		}
		public void Load (Guid instanceId, TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void LoadRunnableInstance (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void Persist (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public BookmarkResumptionResult ResumeBookmark (Bookmark bookmark, object value)
		{
			throw new NotImplementedException ();
		}
		public BookmarkResumptionResult ResumeBookmark (string bookmarkName, object value)
		{
			throw new NotImplementedException ();
		}
		public BookmarkResumptionResult ResumeBookmark (Bookmark bookmark, object value, TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public BookmarkResumptionResult ResumeBookmark (string bookmarkName, object value, TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void Run ()
		{
			Initialize (Inputs, null);
			Controller.Run ();
		}
		public void Run (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void Terminate (Exception reason)
		{
			throw new NotImplementedException ();
		}
		public void Terminate (string reason)
		{
			throw new NotImplementedException ();
		}
		public void Terminate (Exception reason, TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void Terminate (string reason, TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void Unload ()
		{
			throw new NotImplementedException ();
		}
		public void Unload (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}

		protected internal override IAsyncResult OnBeginAssociateKeys (ICollection<InstanceKey> keys, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}

		protected internal override IAsyncResult OnBeginPersist (AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}

		protected internal override IAsyncResult OnBeginResumeBookmark (Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}

		protected internal override void OnDisassociateKeys (ICollection<InstanceKey> keys)
		{
			throw new NotImplementedException ();
		}

		protected internal override void OnEndAssociateKeys (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}

		protected internal override void OnEndPersist (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}

		protected internal override BookmarkResumptionResult OnEndResumeBookmark (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}

		protected override void OnNotifyPaused ()
		{
			if (Controller.State == WorkflowInstanceState.Complete) {
				RaiseCompleted ();
			} else 
				throw new NotImplementedException ();
		}
		void RaiseCompleted ()
		{
			if (Completed == null)
				return;
			IDictionary<string, object> outputs;
			Exception ex;
			var state = Controller.GetCompletionState (out outputs,out ex);
			var comArgs = new WorkflowApplicationCompletedEventArgs (this, state, outputs, ex);
			Completed (comArgs);
		}
		void RaiseAborted ()
		{
			if (Aborted == null)
				return;
			var abArgs = new WorkflowApplicationAbortedEventArgs (this, Controller.GetAbortReason ());
			Aborted (abArgs);
		}
		protected override void OnNotifyUnhandledException (Exception exception, Activity source, string sourceInstanceId)
		{
			if (OnUnhandledException == null)
				return;

			var exArgs = new WorkflowApplicationUnhandledExceptionEventArgs (this, source,
			                                                                 sourceInstanceId,
			                                                                 exception);
			var action = OnUnhandledException (exArgs);

			switch (action) {
			case UnhandledExceptionAction.Abort:
				Controller.Abort (exception);
				RaiseAborted ();
				break;
			case UnhandledExceptionAction.Terminate:
				Controller.Terminate (exception);
				RaiseCompleted ();
				break;
			case UnhandledExceptionAction.Cancel:
				throw new NotImplementedException ();
			}
		}
		protected internal override void OnRequestAbort (Exception reason)
		{
			//FIXME: when does this get called? Call RaiseAborted when it does?
			throw new NotImplementedException ();
		}

	}
}
