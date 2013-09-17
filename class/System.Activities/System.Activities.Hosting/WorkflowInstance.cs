using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Threading;
using System.Activities;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Tracking;

namespace System.Activities.Hosting
{
	public abstract partial class WorkflowInstance
	{
		protected WorkflowInstance (Activity workflowDefinition)
		{
			if (workflowDefinition == null)
				throw new ArgumentNullException ("workflowDefinition");
			WorkflowDefinition = workflowDefinition;
			Runtime = new WorkflowRuntime (WorkflowDefinition);
			Initialized = false;
		}

		protected WorkflowInstanceControl Controller { 
			get { 
				if (Initialized)
					return controller;
				throw new InvalidOperationException ("WorkflowInstance.Controller is only valid after "
									+ "Initialize has been called.");
			}
		}
		public LocationReferenceEnvironment HostEnvironment { get; set; }
		public abstract Guid Id { get; }
		protected bool IsReadOnly { get { throw new NotImplementedException (); } }
		protected internal abstract bool SupportsInstanceKeys { get; }
		public SynchronizationContext SynchronizationContext { get; set; }
		public Activity WorkflowDefinition { get; private set; }

		WorkflowRuntime Runtime { get; set; }
		WorkflowInstanceControl controller;
		bool Initialized { get; set; }
		protected void DisposeExtensions ()
		{
			Runtime.DiposeExtensions ();
		}
		protected internal T GetExtension<T> () where T : class
		{
			return Runtime.GetExtension<T> ();
		}
		protected internal IEnumerable<T> GetExtensions<T> () where T : class
		{
			return Runtime.GetExtensions<T> ();
		}
		protected void Initialize (object deserializedRuntimeState)
		{
			throw new NotImplementedException ();
		}
		protected void Initialize (IDictionary<string, object> workflowArgumentValues,IList<Handle> workflowExecutionProperties)
		{
			Runtime.WorkflowInstanceProxy = new WorkflowInstanceProxy (
				OnBeginResumeBookmark, OnEndResumeBookmark, Id, WorkflowDefinition);
			Runtime.Initialize (workflowArgumentValues, workflowExecutionProperties);
			controller = new WorkflowInstanceControl (this);
			Runtime.NotifyPaused = OnNotifyPaused;
			Runtime.UnhandledException = OnNotifyUnhandledException;
			Runtime.RequestAbort = OnRequestAbort;
			Initialized = true;
		}
		protected internal abstract IAsyncResult OnBeginAssociateKeys (ICollection<InstanceKey> keys,AsyncCallback callback,object state);

		protected virtual IAsyncResult OnBeginFlushTrackingRecords (AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		protected internal abstract IAsyncResult OnBeginPersist (AsyncCallback callback,object state);

		protected internal abstract IAsyncResult OnBeginResumeBookmark (Bookmark bookmark,object value,TimeSpan timeout,AsyncCallback callback,object state);

		protected internal abstract void OnDisassociateKeys (ICollection<InstanceKey> keys);

		protected internal abstract void OnEndAssociateKeys (IAsyncResult result);

		protected virtual void OnEndFlushTrackingRecords (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}

		protected internal abstract void OnEndPersist (IAsyncResult result);

		protected internal abstract BookmarkResumptionResult OnEndResumeBookmark (IAsyncResult result);

		protected abstract void OnNotifyPaused ();

		protected abstract void OnNotifyUnhandledException (Exception exception,Activity source,string sourceInstanceId);

		protected internal abstract void OnRequestAbort (Exception reason);

		protected void RegisterExtensionManager (WorkflowInstanceExtensionManager extensionManager)
		{
			Runtime.RegisterExtensionManager (extensionManager);
		}
		protected void ThrowIfReadOnly ()
		{
			throw new NotImplementedException ();
		}
	}
}
