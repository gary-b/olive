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
using System.Runtime.Remoting.Messaging;

namespace System.Activities
{
	public sealed class WorkflowInvoker
	{
		public WorkflowInvoker (Activity workflow)
		{
			if (workflow == null)
				throw new ArgumentNullException ("workflow");

			WorkflowDefinition = workflow;
		}
		Activity WorkflowDefinition { get; set; }

		public event EventHandler<InvokeCompletedEventArgs> InvokeCompleted;
		
		public WorkflowInstanceExtensionManager Extensions { get { throw new NotImplementedException (); } }

		public IAsyncResult BeginInvoke (AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginInvoke (IDictionary<string, Object> inputs,AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginInvoke (TimeSpan timeout,AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginInvoke (IDictionary<string, Object> inputs,TimeSpan timeout,AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, Object> EndInvoke (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow)
		{
			return InitialiseRunAndGetResults (workflow);
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow)
		{
			return (TResult) InitialiseRunAndGetResults (workflow) ["Result"];
		}
		public IDictionary<string, Object> Invoke (IDictionary<string, Object> inputs)
		{
			return InitialiseRunAndGetResults (WorkflowDefinition, inputs);;
		}
		public IDictionary<string, Object> Invoke (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,IDictionary<string, Object> inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException ("inputs");
			//if workflow null exception thrown from WorkflowRuntime ctor
			return InitialiseRunAndGetResults (workflow, inputs);
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow,IDictionary<string, Object> inputs)
		{
			return (TResult) (InitialiseRunAndGetResults (workflow, inputs) ["Result"]);
		}
		public IDictionary<string, Object> Invoke (IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow,IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow,IDictionary<string, Object> inputs,out IDictionary<string, Object> additionalOutputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (object userState)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs,object userState)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (TimeSpan timeout,object userState)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs,TimeSpan timeout,object userState)
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, Object> Invoke ()
		{
			return InitialiseRunAndGetResults (WorkflowDefinition);
		}
		static IDictionary<string, Object> InitialiseRunAndGetResults (Activity wf)
		{
			var wr = GetWorkflowRuntime (wf, null);
			return RunAndGetResults (wr);
		}
		static IDictionary<string, Object> InitialiseRunAndGetResults (Activity wf, IDictionary<string, object> inputs)
		{
			var wr = GetWorkflowRuntime (wf, inputs);
			return RunAndGetResults (wr);
		}
		static IDictionary<string, Object> RunAndGetResults (WorkflowRuntime wr)
		{
			//FIXME: test how the Invoke calls should handle Aborts and Terminations
			wr.Run ();
			IDictionary<string, object> outputs;
			Exception ex;
			wr.GetCompletionState (out outputs, out ex);
			return outputs;
		}
		static WorkflowRuntime GetWorkflowRuntime (Activity wf, IDictionary<string, object> inputs) 
		{
			//FIXME: hack bypassing use of WorkflowInstance while SynchronisationContext not implemented
			//WorkflowInvoker must run the WF on the same thread as itself, i havent implemented
			//thread control in WorkflowInstance however

			WorkflowRuntime wr = new WorkflowRuntime (wf);;
			Func<Bookmark, object, TimeSpan, AsyncCallback, object, IAsyncResult> onBegin = 
				(bookmark, value, timeout, callback, state) => {
				var del = new Func<BookmarkResumptionResult> (() => wr.ScheduleHostBookmarkResumption (bookmark, value));
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			Func<IAsyncResult, BookmarkResumptionResult> onEnd = (result) => {
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				result.AsyncWaitHandle.Close ();
				wr.Run ();
				return retValue;
			};
			wr.WorkflowInstanceProxy = new WorkflowInstanceProxy (onBegin, onEnd, Guid.NewGuid (), wf);

			wr.UnhandledException = (ex, sourceActivity, sourceId)=> {
				throw ex;
			};
			wr.Initialize (inputs, null);
			return wr;
		}
	}
}
