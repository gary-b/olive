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

namespace MonoTests.System.Activities {
	public class WFTestHelper {
		protected void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			//tests calling this method presume wf will run on same thread as nunit
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}
		protected void RunAndCompare (Activity workflow, string expectedOnConsole, string failMsg)
		{
			//tests calling this method presume wf will run on same thread as nunit
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString (), failMsg);
		}
		protected WFAppWrapper GetWFAppWrapperAndRun (Activity wf, WFAppStatus expectedStatus)
		{
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (expectedStatus, app.Status);
			return app;
		}
		protected Activity GetActSchedulesPubExpAndWrites (Activity<string> exp)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddChild (exp);
			}, (context) => {
				context.ScheduleActivity (exp, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
		}
		protected Activity GetActSchedulesImpExpAndWrites (Activity<string> exp)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (exp);
			}, (context) => {
				context.ScheduleActivity (exp, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
		}
		protected NativeActivityRunner GetActSchedulesImpChild (Activity child)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		protected NativeActivityRunner GetActSchedulesPubChild (Activity child)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		protected Activity<T> GetActReturningResultOfPubChild<T> (Activity<T> child)
		{
			return new NativeActWithCBResultSetter<T> ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			});
		}
		protected Activity<T> GetActReturningResultOfImpChild<T> (Activity<T> child)
		{
			return new NativeActWithCBResultSetter<T> ((metadata) => {
				metadata.AddImplementationChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			});
		}
		protected Activity GetActWithPubVarWrites (Variable<string> varToWrite)
		{
			var writeLine = new WriteLine { Text = varToWrite };
			return new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (varToWrite);
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
		}
		protected Activity GetActWithImpVarWrites (Variable<string> varToWrite)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (varToWrite);
			}, (context) => {
				Console.WriteLine (varToWrite.Get (context));
			});
		}
		protected Activity GetActWithArgWrites (InArgument<string> arg)
		{
			return new NativeActivityRunnerTakesArg (null, (context) => {
				Console.WriteLine (arg.Get (context));
			}, arg);
		}
		protected WriteLine GetWriteLine (InArgument<string> arg)
		{
			return new WriteLine { Text = arg };
		}
		protected Concat GetConcat (InArgument<string> string1, InArgument<string> string2)
		{
			return new Concat { String1 = string1, String2 = string2 };
		}
	}
	public class NativeActivityRunner : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		public Action<NativeActivityContext> executeAction;
		Action<NativeActivityContext> cancelAction;
		string cancelMsg;
		public new int CacheId { get { return base.CacheId; } }
		public bool InduceIdle { get; set; }
		protected override bool CanInduceIdle {
			get {
				return InduceIdle;
			}
		}
		public NativeActivityRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                             Action<NativeActivityContext> execute)
		{
			InduceIdle = false;
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
		}
		public NativeActivityRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                             Action<NativeActivityContext> execute,
		                             Action<NativeActivityContext> cancel)
			:this (cacheMetadata, execute)
		{
			cancelAction = cancel;
		}
		public NativeActivityRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                             Action<NativeActivityContext> execute,
		                             string cancelMsg)
			:this (cacheMetadata, execute)
		{
			this.cancelMsg = cancelMsg;
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
		protected override void Cancel (NativeActivityContext context)
		{
			if (cancelAction != null) {
				cancelAction (context);
			} else if (cancelMsg != null) {
				Console.WriteLine (cancelMsg);
				base.Cancel (context);
			} else {
				base.Cancel (context);
			}
		}
	}
	public class NativeActivityRunner<T> : NativeActivity<T>	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, OutArgument<T>> executeAction;
		public new int CacheId { get { return base.CacheId; } }
		public bool InduceIdle { get; set; }
		protected override bool CanInduceIdle {
			get {
				return InduceIdle;
			}
		}
		public NativeActivityRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                             Action<NativeActivityContext, OutArgument<T>> execute)
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
				executeAction (context, Result);
		}
	}
	public class NativeActWithCBResultSetter<T> : NativeActivity<T> {
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, CompletionCallback<T>> executeAction;
		public new int CacheId { get { return base.CacheId; } }
		public bool InduceIdle { get; set; }
		protected override bool CanInduceIdle {
			get {
				return InduceIdle;
			}
		}
		public NativeActWithCBResultSetter (Action<NativeActivityMetadata> cacheMetadata, 
		                                   Action<NativeActivityContext, CompletionCallback<T>> execute)
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
				executeAction (context, Callback);
		}
		void Callback (NativeActivityContext context, ActivityInstance compAI, T value)
		{
			Result.Set (context, value);
		}
	}
	public class NativeRunnerWithArgStr : NativeActivityRunner {
		InArgument<string> ArgStr = new InArgument<string> ("Hello\nWorld");
		public NativeRunnerWithArgStr (Action<NativeActivityMetadata> cacheMetadata, Action<NativeActivityContext> execute)
			:base (cacheMetadata, execute)
		{
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			var rtStr = new RuntimeArgument ("ArgStr", typeof (string), ArgumentDirection.In);
			metadata.AddArgument (rtStr);
			metadata.Bind (ArgStr, rtStr);
			base.CacheMetadata (metadata); //allow cacheMetadata delegate provided by user to be run
		}
	}
	public class NativeActivityRunnerTakesArg : NativeActivityRunner {
		InArgument<string> arg;
		public NativeActivityRunnerTakesArg (Action<NativeActivityMetadata> cacheMetadata, 
		                                     Action<NativeActivityContext> execute,
		                                     InArgument<string> arg) : base (cacheMetadata, execute)
		{
			this.arg = arg;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (arg != null) {
				var rtArg = new RuntimeArgument ("arg", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg);
				metadata.Bind (arg, rtArg);
			}
			base.CacheMetadata (metadata);
		}
	}
	public class NativeActWithCBRunner : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, CompletionCallback> executeAction;
		Action<NativeActivityContext, ActivityInstance, CompletionCallback> callbackAction;
		Action<NativeActivityContext> cancelAction;
		string cancelMsg;
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
		public NativeActWithCBRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                              Action<NativeActivityContext, CompletionCallback> execute,
		                              Action<NativeActivityContext, ActivityInstance, CompletionCallback> callback,
		                              Action<NativeActivityContext> cancel)
			: this (cacheMetadata, execute, callback)
		{
			cancelAction = cancel;
		}
		public NativeActWithCBRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                              Action<NativeActivityContext, CompletionCallback> execute,
		                              Action<NativeActivityContext, ActivityInstance, CompletionCallback> callback,
		                              string cancelMsg)
			: this (cacheMetadata, execute, callback)
		{
			this.cancelMsg = cancelMsg;
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
		protected override void Cancel (NativeActivityContext context)
		{
			if (cancelAction != null) {
				cancelAction (context);
			} else if (cancelMsg != null) {
				Console.WriteLine (cancelMsg);
				base.Cancel (context);
			} else {
				base.Cancel (context);
			}
		}
	}
	public class VarDefAndArgEvalOrder : NativeActivity {
		public InArgument<string> InArg1 { get; set; }
		public Variable<string> PubVar1 { get;set; }
		public InOutArgument<string> InOutArg1 { get; set; }
		public Variable<string> ImpVar1 { get;set; }
		public OutArgument<string> OutArg1 { get; set; }
		public Variable<string> PubVar2 { get;set; }
		public InArgument<string> InArg2 { get; set; }
		public Variable<string> ImpVar2 { get;set; }
		public InOutArgument<string> InOutArg2 { get; set; }
		public OutArgument<string> OutArg2 { get; set; }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			var rtInArg1 = new RuntimeArgument ("InArg1", typeof (string), ArgumentDirection.In);
			metadata.AddArgument (rtInArg1);
			metadata.Bind (InArg1, rtInArg1);
			metadata.AddVariable (PubVar1);
			var rtInOutArg1 = new RuntimeArgument ("InOutArg1", typeof (string), ArgumentDirection.InOut);
			metadata.AddArgument (rtInOutArg1);
			metadata.Bind (InOutArg1, rtInOutArg1);
			metadata.AddImplementationVariable (ImpVar1);
			var rtOutArg1 = new RuntimeArgument ("OutArg1", typeof (string), ArgumentDirection.Out);
			metadata.AddArgument (rtOutArg1);
			metadata.Bind (OutArg1, rtOutArg1);
			var rtInArg2 = new RuntimeArgument ("InArg2", typeof (string), ArgumentDirection.In);
			metadata.AddArgument (rtInArg2);
			metadata.Bind (InArg2, rtInArg2);
			metadata.AddVariable (PubVar2);
			var rtInOutArg2 = new RuntimeArgument ("InOutArg2", typeof (string), ArgumentDirection.InOut);
			metadata.AddArgument (rtInOutArg2);
			metadata.Bind (InOutArg2, rtInOutArg2);
			metadata.AddImplementationVariable (ImpVar2);
			var rtOutArg2 = new RuntimeArgument ("OutArg2", typeof (string), ArgumentDirection.Out);
			metadata.AddArgument (rtOutArg2);
			metadata.Bind (OutArg2, rtOutArg2);
		}
		protected override void Execute (NativeActivityContext context)
		{
			Console.WriteLine ("ExEvExecute");
		}
		public class GetString : CodeActivity<string> {
			string msg;
			public GetString (string msg)
			{
				this.msg = msg;
			}
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
			}
			protected override string Execute (CodeActivityContext context)
			{
				Console.WriteLine (msg);
				return msg;
			}
		}
		public class GetLocationString : CodeActivity<Location<string>> {
			string msg;
			public GetLocationString (string msg)
			{
				this.msg = msg;
			}
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
			}
			protected override Location<string> Execute (CodeActivityContext context)
			{
				Console.WriteLine (msg);
				var loc = new Location<string> ();
				loc.Value = msg;
				return loc;
			}
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
		Action<NativeActivityContext> cancelAction;
		string cancelMsg;
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
		public NativeActWithFaultCBRunner (Action<NativeActivityMetadata> cacheMetadata,
		                                   Action<NativeActivityContext, FaultCallback> execute,
		                                   Action<NativeActivityFaultContext, Exception, ActivityInstance> callback,
		                                   Action<NativeActivityContext> cancel)
			:this (cacheMetadata, execute, callback)
		{
			cancelAction = cancel;
		}
		public NativeActWithFaultCBRunner (Action<NativeActivityMetadata> cacheMetadata,
		                                   Action<NativeActivityContext, FaultCallback> execute,
		                                   Action<NativeActivityFaultContext, Exception, ActivityInstance> callback,
		                                   string cancelMsg)
			:this (cacheMetadata, execute, callback)
		{
			this.cancelMsg = cancelMsg;
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
		protected override void Cancel (NativeActivityContext context)
		{
			if (cancelAction != null) {
				cancelAction (context);
			} else if (cancelMsg != null) {
				Console.WriteLine (cancelMsg);
				base.Cancel (context);
			} else {
				base.Cancel (context);
			}
		}
	}
	public class NativeActWithBookmarkRunner : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext, BookmarkCallback> executeAction;
		Action<NativeActivityContext, Bookmark, object, BookmarkCallback> bookmarkAction;
		Action<NativeActivityContext> cancelAction;
		string cancelMsg;
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
		public NativeActWithBookmarkRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                                    Action<NativeActivityContext, BookmarkCallback> execute,
		                                    Action<NativeActivityContext, Bookmark, object, BookmarkCallback> bookmark,
		                                    Action<NativeActivityContext> cancel) 
			:this (cacheMetadata, execute, bookmark)
		{
			cancelAction = cancel;
		}
		public NativeActWithBookmarkRunner (Action<NativeActivityMetadata> cacheMetadata, 
		                                    Action<NativeActivityContext, BookmarkCallback> execute,
		                                    Action<NativeActivityContext, Bookmark, object, BookmarkCallback> bookmark,
		                                    string cancelMsg) 
			:this (cacheMetadata, execute, bookmark)
		{
			this.cancelMsg = cancelMsg;
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
		protected override void Cancel (NativeActivityContext context)
		{
			if (cancelAction != null) {
				cancelAction (context);
			} else if (cancelMsg != null) {
				Console.WriteLine (cancelMsg);
				base.Cancel (context);
			} else {
				base.Cancel (context);
			}
		}
	}
	public class CodeActivityRunner : CodeActivity {
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
	public class CodeActivityTRunner<T> : CodeActivity<T> {
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
	public class ActivityRunner : Activity {
		new public int CacheId {
			get { return base.CacheId; }
		}
		public ActivityRunner (Func<Activity> implementation)
		{
			this.Implementation = implementation;
		}
	}
	public class TrackIdWrite : CodeActivity {
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
		}
		protected override void Execute (CodeActivityContext context)
		{
			Console.WriteLine ("CacheId: {0} ActivityInstanceId: {1} Id: {2}",
					   this.CacheId, context.ActivityInstanceId, this.Id);

		}
	}
	public class WriteLineHolder : NativeActivity {
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
	public class Concat : CodeActivity<string> {
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
	public class ConcatMany : CodeActivity<string> {
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
	public class HelloWorldEx : CodeActivity<string> {
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
		public string ExceptionSourceInstanceId { get; private set; }
		public Activity ExceptionSource { get; private set; }
		public Exception AbortReason { get; private set; }
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
				ExceptionSourceInstanceId = args.ExceptionSourceInstanceId;
				ExceptionSource = args.ExceptionSource;
				reset.Set ();
				return UnhandledExceptionAction.Terminate;
			};
			app.Aborted = (args) => {
				AbortReason = args.Reason;
				Status = WFAppStatus.Aborted;
				reset.Set ();
			};
		}
		public void Run ()
		{
			Console.SetOut (cOut);
			app.Run ();
			reset.WaitOne ();
		}
		public void RunAndCancel (int waitBeforeCancelSecs)
		{
			Console.SetOut (cOut);
			app.Run ();
			if (waitBeforeCancelSecs > 0)
				Thread.Sleep (TimeSpan.FromSeconds (waitBeforeCancelSecs));
			app.Cancel ();
			reset.WaitOne ();
		}
		public void Cancel ()
		{
			reset.Reset ();
			app.Cancel ();
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
									TimeSpan Timeout, AsyncCallback callback, 
									object state)
		{
			if (BeginResumeBookmark != null)
				return BeginResumeBookmark (bookmark, value, Timeout, callback, state);
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

