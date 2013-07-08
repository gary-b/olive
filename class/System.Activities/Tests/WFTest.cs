using System;
using System.Activities;
using System.IO;
using NUnit.Framework;
using System.Activities.Statements;
using System.Collections.ObjectModel;
using System.Activities.Hosting;
using System.Threading;

namespace Tests.System.Activities
{
	public class WFTest {
		protected void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
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
	enum WFAppStatus {
		Unset,
		CompletedSuccessfully,
		Terminated,
		Idle,
		UnhandledException,
		Aborted,
		Cancelled
	}
	class WFAppWrapper {
		WorkflowApplication app { get; set; }
		AutoResetEvent reset { get; set; }
		StringWriter cOut { get; set; }
		public WFAppStatus Status { get; private set; }
		public Exception UnhandledException { get; private set; }
		public String ConsoleOut { 
			get { return cOut.ToString (); }
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
}

