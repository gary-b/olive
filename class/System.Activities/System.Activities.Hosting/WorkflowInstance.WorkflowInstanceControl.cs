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
		protected struct WorkflowInstanceControl
		{
			public static bool operator == (WorkflowInstanceControl left, WorkflowInstanceControl right)
			{
				throw new NotImplementedException ();
			}
			public static bool operator != (WorkflowInstanceControl left, WorkflowInstanceControl right)
			{
				throw new NotImplementedException ();
			}

			public bool HasPendingTrackingRecords { get { throw new NotImplementedException (); } }
			public bool IsPersistable { get { throw new NotImplementedException (); } }
			public WorkflowInstanceState State { 
				get { 
					switch (Instance.Runtime.RuntimeState) {
					case RuntimeState.Terminated:
					case RuntimeState.CompletedSuccessfully:
						return WorkflowInstanceState.Complete;
					case RuntimeState.Aborted:
						return WorkflowInstanceState.Aborted;
					case RuntimeState.Ready:
					case RuntimeState.Executing:
					case RuntimeState.UnhandledException:
						//FIXME: Controller.State shows Idle if root exceptions, Runnable if child
						return WorkflowInstanceState.Runnable;
					default:
						throw new NotImplementedException ("New RuntimeState");
					}
				}
			}
			public bool TrackingEnabled { get { throw new NotImplementedException (); } }

			WorkflowInstance Instance { get; set; }

			internal WorkflowInstanceControl (WorkflowInstance instance) : this ()
			{
				if (instance == null)
					throw new ArgumentNullException ("runtime");
				Instance = instance;
			}

			public void Abort ()
			{
				Instance.Runtime.Abort ();
			}
			public void Abort (Exception reason)
			{
				Instance.Runtime.Abort (reason);
			}
			public IAsyncResult BeginFlushTrackingRecords (TimeSpan timeout,AsyncCallback callback,object state)
			{
				throw new NotImplementedException ();
			}
			public void EndFlushTrackingRecords (IAsyncResult result)
			{
				throw new NotImplementedException ();
			}
			public override bool Equals (object obj)
			{
				throw new NotImplementedException ();
			}
			public void FlushTrackingRecords (TimeSpan timeout)
			{
				throw new NotImplementedException ();
			}
			public Exception GetAbortReason ()
			{
				return Instance.Runtime.GetAbortReason ();
			}
			public ReadOnlyCollection<BookmarkInfo> GetBookmarks ()
			{
				throw new NotImplementedException ();
			}
			public ReadOnlyCollection<BookmarkInfo> GetBookmarks (BookmarkScope scope)
			{
				throw new NotImplementedException ();
			}
			public ActivityInstanceState GetCompletionState ()
			{
				return Instance.Runtime.GetCompletionState ();
			}
			public ActivityInstanceState GetCompletionState (out Exception terminationException)
			{
				throw new NotImplementedException ();
			}
			public ActivityInstanceState GetCompletionState (out IDictionary<string, object> outputs,out Exception terminationException)
			{
				return Instance.Runtime.GetCompletionState (out outputs, out terminationException);
			}
			public override int GetHashCode ()
			{
				throw new NotImplementedException ();
			}
			public IDictionary<string, LocationInfo> GetMappedVariables ()
			{
				throw new NotImplementedException ();
			}
			public void PauseWhenPersistable ()
			{
				throw new NotImplementedException ();
			}
			public object PrepareForSerialization ()
			{
				throw new NotImplementedException ();
			}
			public void RequestPause ()
			{
				throw new NotImplementedException ();
			}
			public void Run ()
			{
				var t = new Thread (new ThreadStart (Instance.Runtime.Run));
				t.Start ();
			}
			public BookmarkResumptionResult ScheduleBookmarkResumption (Bookmark bookmark,object value)
			{
				throw new NotImplementedException ();
			}
			public BookmarkResumptionResult ScheduleBookmarkResumption (Bookmark bookmark,object value,BookmarkScope scope)
			{
				throw new NotImplementedException ();
			}
			public void ScheduleCancel ()
			{
				throw new NotImplementedException ();
			}
			public void Terminate (Exception reason)
			{
				Instance.Runtime.Terminate (reason);
			}
			public void Track (WorkflowInstanceRecord instanceRecord)
			{
				throw new NotImplementedException ();
			}
		}
	}
}
