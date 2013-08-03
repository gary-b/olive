using System;
using System.Activities;
using System.IO;
using NUnit.Framework;
using System.Activities.Statements;
using System.Collections.ObjectModel;
using System.Activities.Hosting;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.DurableInstancing;

namespace Tests.System.Activities {
	public class WFTestHelper {
		protected void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			//tests calling this method presume wf will run on same thread as nunit
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}
		protected WFAppWrapper GetWFAppWrapperAndRun (Activity wf, WFAppStatus expectedStatus)
		{
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (expectedStatus, app.Status);
			return app;
		}
	}
	public class NativeActivityRunner : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext> executeAction;
		public new int CacheId { get { return base.CacheId; } }
		public bool InduceIdle { get; set; }
		protected override bool CanInduceIdle {
			get {
				return InduceIdle;
			}
		}
		public NativeActivityRunner (Action<NativeActivityMetadata> cacheMetadata, Action<NativeActivityContext> execute)
		{
			InduceIdle = false;
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (cacheMetadataAction != null)
				cacheMetadataAction (metadata);
		}
		protected override void Execute (NativeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context);
		}
	}
	public class NativeActWithCBRunner : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, CompletionCallback> executeAction;
		Action<NativeActivityContext, ActivityInstance, CompletionCallback> callbackAction;
		public bool InduceIdle { get; set; }
		protected override bool CanInduceIdle {
			get {
				return InduceIdle;
			}
		}
		public NativeActWithCBRunner (Action<NativeActivityMetadata> cacheMetadata, 
					      Action<NativeActivityContext, CompletionCallback> execute,
					      Action<NativeActivityContext, ActivityInstance, CompletionCallback> callback)
		{
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
			callbackAction = callback;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (cacheMetadataAction != null)
				cacheMetadataAction (metadata);
		}
		protected override void Execute (NativeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context, Callback);
		}
		void Callback (NativeActivityContext context, ActivityInstance completedInstance)
		{
			if (callbackAction != null)
				callbackAction (context, completedInstance, Callback);
		}
	}
	public class NativeActWithCBRunner<CallbackType> : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, CompletionCallback<CallbackType>> executeAction;
		Action<NativeActivityContext, ActivityInstance, CompletionCallback<CallbackType>, CallbackType> callbackAction;

		public NativeActWithCBRunner (Action<NativeActivityMetadata> cacheMetadata, 
					      Action<NativeActivityContext, CompletionCallback<CallbackType>> execute,
					      Action<NativeActivityContext, ActivityInstance, 
					      CompletionCallback<CallbackType>, CallbackType> callback)
		{
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
			callbackAction = callback;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (cacheMetadataAction != null)
				cacheMetadataAction (metadata);
		}
		protected override void Execute (NativeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context, Callback);
		}
		void Callback (NativeActivityContext context, ActivityInstance completedInstance, CallbackType result)
		{
			if (callbackAction != null)
				callbackAction (context, completedInstance, Callback, result);
		}

	}
	public class NativeActWithFaultCBRunner : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, FaultCallback> executeAction;
		Action<NativeActivityFaultContext, Exception, ActivityInstance> callbackAction;
		public bool InduceIdle { get; set; }
		protected override bool CanInduceIdle {
			get {
				return InduceIdle;
			}
		}
		public NativeActWithFaultCBRunner (Action<NativeActivityMetadata> cacheMetadata,
						   Action<NativeActivityContext, FaultCallback> execute,
						   Action<NativeActivityFaultContext, Exception, ActivityInstance> callback)
		{
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
			callbackAction = callback;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (cacheMetadataAction != null)
				cacheMetadataAction (metadata);
		}
		protected override void Execute (NativeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context, Callback);
		}
		void Callback (NativeActivityFaultContext faultContext, Exception propagatedException,
			       ActivityInstance propagatedFrom)
		{
			if (callbackAction != null)
				callbackAction (faultContext, propagatedException, propagatedFrom);
		}
	}
	public class NativeActWithBookmarkRunner : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, BookmarkCallback> executeAction;
		Action<NativeActivityContext, Bookmark, object, BookmarkCallback> bookmarkAction;

		protected override bool CanInduceIdle {
			get {
				return true;
			}
		}
		public NativeActWithBookmarkRunner (Action<NativeActivityMetadata> cacheMetadata, 
						    Action<NativeActivityContext, BookmarkCallback> execute,
						    Action<NativeActivityContext, Bookmark, object, BookmarkCallback> bookmark)
		{
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
			bookmarkAction = bookmark;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (cacheMetadataAction != null)
				cacheMetadataAction (metadata);
		}
		protected override void Execute (NativeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context, Callback);
		}
		void Callback (NativeActivityContext context, Bookmark bookmark, object value)
		{
			if (bookmarkAction != null)
				bookmarkAction (context, bookmark, value, Callback);
		}
	}
	class CodeActivityRunner : CodeActivity {
		Action<CodeActivityMetadata> cacheMetaDataAction;
		Action<CodeActivityContext> executeAction;
		public CodeActivityRunner (Action<CodeActivityMetadata> action, Action<CodeActivityContext> execute)
		{
			cacheMetaDataAction = action;
			executeAction = execute;
		}
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			if (cacheMetaDataAction != null)
				cacheMetaDataAction (metadata);
		}
		protected override void Execute (CodeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context);
		}
	}
	class CodeActivityTRunner<T> : CodeActivity<T> {
		Action<CodeActivityMetadata> cacheMetadataAction;
		Func<CodeActivityContext, T> executeAction;
		public new int CacheId { get { return base.CacheId; } }
		public CodeActivityTRunner (Action<CodeActivityMetadata> cacheMetadata, Func<CodeActivityContext, T> execute)
		{
			if (execute == null)
				throw new ArgumentNullException ("executeAction");
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
		}
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			var rtResult = new RuntimeArgument ("Result", typeof (T), ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			metadata.Bind (Result, rtResult);

			if (cacheMetadataAction != null)
				cacheMetadataAction (metadata);
		}
		protected override T Execute (CodeActivityContext context)
		{
			return executeAction (context);
		}
	}
	class ActivityRunner : Activity {
		new public int CacheId {
			get { return base.CacheId; }
		}
		public ActivityRunner (Func<Activity> implementation)
		{
			this.Implementation = implementation;
		}
	}
	class TrackIdWrite : CodeActivity {
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
		}
		protected override void Execute (CodeActivityContext context)
		{
			Console.WriteLine ("CacheId: {0} ActivityInstanceId: {1} Id: {2}",
					   this.CacheId, context.ActivityInstanceId, this.Id);

		}
	}
	class WriteLineHolder : NativeActivity {
		public WriteLine ImplementationWriteLine { get; set; }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			metadata.AddImplementationChild (ImplementationWriteLine);
		}
		protected override void Execute (NativeActivityContext context)
		{
			context.ScheduleActivity (ImplementationWriteLine);
		}
	}
	class Concat : CodeActivity<string> {
		public InArgument<string> String1 { get; set; }
		public InArgument<string> String2 { get; set; }
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			RuntimeArgument rtString1 = new RuntimeArgument ("String1", typeof (string), ArgumentDirection.In);
			metadata.AddArgument (rtString1);
			metadata.Bind (String1, rtString1);
			RuntimeArgument rtString2 = new RuntimeArgument ("String2", typeof (string), ArgumentDirection.In);
			metadata.AddArgument (rtString2);
			metadata.Bind (String2, rtString2);
			RuntimeArgument rtResult = new RuntimeArgument ("Result", typeof (string), ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			metadata.Bind (Result, rtResult);
		}
		protected override string Execute (CodeActivityContext context)
		{
			return String1.Get (context) + String2.Get (context);
		}
	}
	class ConcatMany : CodeActivity<string> {
		public InArgument<string> String1 { get; set; }
		public InArgument<string> String2 { get; set; }
		public InArgument<string> String3 { get; set; }
		public InArgument<string> String4 { get; set; }
		public InArgument<string> String5 { get; set; }
		public InArgument<string> String6 { get; set; }
		public InArgument<string> String7 { get; set; }
		public InArgument<string> String8 { get; set; }
		public InArgument<string> String9 { get; set; }
		public InArgument<string> String10 { get; set; }
		public InArgument<string> String11 { get; set; }
		public InArgument<string> String12 { get; set; }
		public InArgument<string> String13 { get; set; }
		public InArgument<string> String14 { get; set; }
		public InArgument<string> String15 { get; set; }
		public InArgument<string> String16 { get; set; }
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			for (int i = 1; i < 17; i++) {
				//relaying on auto initialisation and binding feature of AddArgument which is also implemented in mono
				//(in .NET the default implementation of CacheMetadata would take care of all this)
				var rtArg = new RuntimeArgument ("String" + i, typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg);
			}
		}
		protected override string Execute (CodeActivityContext context)
		{
			return context.GetValue (String1) +
				context.GetValue (String2) +
					context.GetValue (String3) +
					context.GetValue (String4) +
					context.GetValue (String5) +
					context.GetValue (String6) +
					context.GetValue (String7) +
					context.GetValue (String8) +
					context.GetValue (String9) +
					context.GetValue (String10) +
					context.GetValue (String11) +
					context.GetValue (String12) +
					context.GetValue (String13) +
					context.GetValue (String14) +
					context.GetValue (String15) +
					context.GetValue (String16);
		}
	}
	class HelloWorldEx : CodeActivity<string> {
		public Exception IThrow { get; set; }
		public HelloWorldEx ()
		{
			IThrow = new InvalidOperationException ();
		}
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
		}
		protected override string Execute (CodeActivityContext context)
		{
			Result.Set (context, "Hello\nWorld");
			throw IThrow;
		}
	}
	public enum WFAppStatus {
		Unset,
		CompletedSuccessfully,
		Terminated,
		Idle,
		UnhandledException,
		Aborted,
		Cancelled
	}
	public class WFAppWrapper {
		WorkflowApplication app { get; set; }
		AutoResetEvent reset { get; set; }
		StringWriter cOut { get; set; }
		public WFAppStatus Status { get; private set; }
		public Exception UnhandledException { get; private set; }
		public string ExceptionSourceIndtanceId { get; set; }
		public Activity ExceptionSource { get; set; }
		public String ConsoleOut { 
			get { return cOut.ToString (); }
		}
		public WorkflowInstanceExtensionManager Extensions {
			get { return app.Extensions; }
		}
		public WFAppWrapper (Activity workflow) 
		{
			app = new WorkflowApplication (workflow);
			reset = new AutoResetEvent (false);
			cOut = new StringWriter ();
			app.Completed = (args) => {
				if (args.CompletionState == ActivityInstanceState.Closed) {
					Status = WFAppStatus.CompletedSuccessfully; 
				} else if (args.CompletionState == ActivityInstanceState.Faulted) {
					// dont do anything as it will wipe out OnUnhandledException status
					/*Status = AppStatus.Terminated; 
						TerminatedException = args.TerminationException;*/
				} else if (args.CompletionState == ActivityInstanceState.Canceled) {
					Status = WFAppStatus.Cancelled;
				}

				reset.Set ();
			};
			app.Idle = (args) => {
				Status = WFAppStatus.Idle;
				reset.Set ();
			};
			app.OnUnhandledException = (args) => {
				Status = WFAppStatus.UnhandledException;
				UnhandledException = args.UnhandledException;
				ExceptionSourceIndtanceId = args.ExceptionSourceInstanceId;
				ExceptionSource = args.ExceptionSource;
				reset.Set ();
				return UnhandledExceptionAction.Terminate;
			};
			//Aborted not implemented
		}
		public void Run ()
		{
			Console.SetOut (cOut);
			app.Run ();
			reset.WaitOne ();
		}
		public BookmarkResumptionResult ResumeBookmark (Bookmark bookmark, object value)
		{
			reset.Reset ();
			Console.SetOut (cOut);
			var result = app.ResumeBookmark (bookmark, value);
			if (result == BookmarkResumptionResult.Success)
				reset.WaitOne ();
			return result;
		}
		public BookmarkResumptionResult ResumeBookmark (string bookmarkName, object value)
		{
			reset.Reset ();
			Console.SetOut (cOut);
			var result = app.ResumeBookmark (bookmarkName, value);
			if (result == BookmarkResumptionResult.Success)
				reset.WaitOne ();
			return result;
		}
		public ReadOnlyCollection<BookmarkInfo> GetBookmarks ()
		{
			return app.GetBookmarks ();
		}
	}
	public class WorkflowInstanceHost : WorkflowInstance {
		TextWriter consoleOut;
		AutoResetEvent autoResetEvent;

		public String ConsoleOut { 
			get { return consoleOut.ToString (); } 
		}
		public AutoResetEvent AutoResetEvent {
			get { return autoResetEvent; }
		}
		public WorkflowInstanceHost (Activity workflowDefinition) : base (workflowDefinition)
		{
			consoleOut = new StringWriter ();
			Console.SetOut (consoleOut);
			autoResetEvent = new AutoResetEvent (false);
		}
		new public void Initialize (IDictionary<string, object> workflowArgumentValues, 
						IList<Handle> workflowExecutionProperties)
		{
			base.Initialize (workflowArgumentValues, workflowExecutionProperties);
		}
		new public void RegisterExtensionManager (WorkflowInstanceExtensionManager extensionManager)
		{
			base.RegisterExtensionManager (extensionManager);
		}
		new public T GetExtension<T> () where T : class
		{
			return base.GetExtension<T> ();
		}
		new public IEnumerable<T> GetExtensions<T> () where T : class
		{
			return base.GetExtensions<T> ();
		}
		public string Controller_ToString()
		{
			return Controller.ToString ();
		}
		public void Controller_Run ()
		{
			Controller.Run ();
		}
		public ActivityInstanceState Controller_GetCompletionState ()
		{
			return Controller.GetCompletionState ();
		}
		public ActivityInstanceState Controller_GetCompletionState (out IDictionary<string, object> outputs, 
										out Exception terminationException)
		{
			return Controller.GetCompletionState (out outputs, out terminationException);
		}
		public void Controller_Terminate (Exception ex)
		{
			Controller.Terminate (ex);
		}
		public void Controller_Abort (Exception ex)
		{
			Controller.Abort (ex);
		}
		public void Controller_Abort ()
		{
			Controller.Abort ();
		}
		new public void DisposeExtensions ()
		{
			base.DisposeExtensions ();
		}
		public Exception Controller_GetAbortReason ()
		{
			return Controller.GetAbortReason ();
		}
		public ReadOnlyCollection<BookmarkInfo> Controller_GetBookmarks ()
		{
			return Controller.GetBookmarks ();
		}
		public ReadOnlyCollection<BookmarkInfo> Controller_GetBookmarks (BookmarkScope scope)
		{
			return Controller.GetBookmarks (scope);
		}
		public void Controller_ScheduleCancel ()
		{
			Controller.ScheduleCancel ();
		}
		public BookmarkResumptionResult Controller_ScheduleBookmarkResumption (Bookmark bookmark, object value)
		{
			return Controller.ScheduleBookmarkResumption (bookmark, value);
		}
		public BookmarkResumptionResult Controller_ScheduleBookmarkResumption (Bookmark bookmark, object value, 
											BookmarkScope scope)
		{
			return Controller.ScheduleBookmarkResumption (bookmark, value, scope);
		}
		public WorkflowInstanceState Controller_State {
			get { return Controller.State; }
		}
		#region implemented abstract members of WorkflowInstance
		Guid id = Guid.Empty;
		public override Guid Id {
			get {
				if (id == Guid.Empty)
					id = Guid.NewGuid ();
				return id;
			}
		}
		protected override bool SupportsInstanceKeys {
			get {
				throw new NotImplementedException ();
			}
		}
		public Func<Bookmark, object, TimeSpan, AsyncCallback, object, IAsyncResult> BeginResumeBookmark { get; set; }
		protected override IAsyncResult OnBeginResumeBookmark (Bookmark bookmark, object value, 
									TimeSpan timeout, AsyncCallback callback, 
									object state)
		{
			if (BeginResumeBookmark != null)
				return BeginResumeBookmark (bookmark, value, timeout, callback, state);
			else
				throw new NotImplementedException ();
		}
		public Func<IAsyncResult, BookmarkResumptionResult> EndResumeBookmark { get; set; }
		protected override BookmarkResumptionResult OnEndResumeBookmark (IAsyncResult result)
		{
			if (EndResumeBookmark != null)
				return EndResumeBookmark (result);
			else
				throw new NotImplementedException ();
		}
		protected override IAsyncResult OnBeginPersist (AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		protected override void OnEndPersist (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		protected override void OnDisassociateKeys (ICollection<InstanceKey> keys)
		{
			throw new NotImplementedException ();
		}
		protected override IAsyncResult OnBeginAssociateKeys (ICollection<InstanceKey> keys, 
									AsyncCallback callback, object state)
		{
			throw new NotImplementedException ();
		}
		protected override void OnEndAssociateKeys (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public Action NotifyPaused { get; set; }
		protected override void OnNotifyPaused ()
		{
			//this is run on a different thread than ctor and Run
			if (NotifyPaused != null)
				NotifyPaused ();
		}
		public Action<Exception, Activity, string> NotifyUnhandledException { get; set; }
		protected override void OnNotifyUnhandledException (Exception exception, Activity source, 
									string sourceInstanceId)
		{
			if (NotifyUnhandledException != null)
				NotifyUnhandledException (exception, source, sourceInstanceId);
		}
		protected override void OnRequestAbort (Exception reason)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

