using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Hosting;

namespace Tests.System.Activities {
	[TestFixture]
	public class FaultCallbackHandlingRuntimeTest : WFTestHelper {
		static BookmarkCallback writeValueBookCB = (ctx, book, value) => {
			Console.WriteLine ((string) value);
		};
		class FaultState {
			public bool IsCompleted { get; private set; }
			public Exception Exception { get; private set; }
			public ActivityInstanceState State { get; private set; }
			public string Id { get; private set;}
			public FaultState (Exception ex, ActivityInstance ai)
			{
				IsCompleted = ai.IsCompleted;
				Exception = ex;
				State = ai.State;
				Id = ai.Id;
			}
			public void AssertFault (bool isFaulted, Exception ex, string id)
			{
				Assert.AreEqual (isFaulted, IsCompleted);
				if (isFaulted)
					Assert.AreEqual (ActivityInstanceState.Faulted, State);
				else
					Assert.AreEqual (ActivityInstanceState.Executing, State);

				Assert.AreSame (ex, Exception);
				Assert.AreEqual (id, Id);
			}
		}
		#region ActivityInstanceState and WorkflowInstanceState tests
		//FIXME: WorkflowInstanceHost methods come from WorkflowInstanceTest, look to come up with tests that dont potentially hang when failing
		WorkflowInstanceHost GetHostToHandleException (Activity wf)
		{
			var host = new WorkflowInstanceHost (wf);
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.AutoResetEvent.Set ();
			};
			return host;
		}
		WorkflowInstanceHost GetHostToHandleExceptionIdleOrComplete (Activity wf)
		{
			var host = GetHostToHandleException (wf);
			host.NotifyPaused = () =>  {
				var state = host.Controller_State;
				if (state == WorkflowInstanceState.Complete || state == WorkflowInstanceState.Idle)
				host.AutoResetEvent.Set ();
			};
			return host;
		}
		void InitRunWait (WorkflowInstanceHost host)
		{
			host.Initialize (null, null);
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
		}
		void RunAgain (WorkflowInstanceHost host)
		{
			host.AutoResetEvent.Reset ();
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
		}
		[Test]
		[Timeout (1500)]
		public void State_ChildExceptions_FaultNotHandled ()
		{
			var helloWorldEx = new HelloWorldEx ();
			ActivityInstance ai = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				ai = context.ScheduleActivity (helloWorldEx);
			});
			var host = GetHostToHandleException (wf);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
		}
		[Test]
		[Timeout (1500)]
		public void State_ChildExceptions_FaultNotHandled_RunAgainSinceStatusRunnable ()
		{
			var helloWorldEx = new HelloWorldEx ();
			ActivityInstance ai = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				ai = context.ScheduleActivity (helloWorldEx);
			});
			var host = GetHostToHandleExceptionIdleOrComplete (wf);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
			RunAgain (host);
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
			Assert.AreEqual (ActivityInstanceState.Closed, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		[Test]
		[Timeout (1500)]
		public void State_ChildExceptions_FaultNotHandled_RunAgainWithOtherPendingChildren ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var writer = new WriteLine { Text = "writer" };
			ActivityInstance ai = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (writer);
			}, (context) => {
				context.ScheduleActivity (writer);
				ai = context.ScheduleActivity (helloWorldEx);
			});
			var host = GetHostToHandleExceptionIdleOrComplete (wf);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
			RunAgain (host);
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
			Assert.AreEqual (ActivityInstanceState.Closed, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.AreEqual ("writer" + Environment.NewLine, host.ConsoleOut);
		}
		[Test]
		[Timeout (1500)]
		[Ignore ("Root Activity Special State Handling on Fault")]
		public void State_RootExceptions ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var host = GetHostToHandleException (helloWorldEx);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
		}
		[Test]
		[Timeout (1500)]
		[Ignore ("Root Activity Special State Handling on Fault")]
		public void State_RootExceptions_TryResumeBookmarkOnRoot ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB);
				throw new Exception ();
			});
			wf.InduceIdle = true;
			var host = GetHostToHandleException (wf);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			var result = host.Controller_ScheduleBookmarkResumption (bookmark, "resumed");
			Assert.AreEqual (BookmarkResumptionResult.NotFound, result);
		}
		[Test]
		[Timeout (1500)]
		[Ignore ("Root Activity Special State Handling on Fault")]
		public void State_RootExceptions_HasBookmarkResumption_TryRunAgainSinceRunnable ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB);
				context.ResumeBookmark (bookmark, "resumed");
				throw new Exception ();
			});
			wf.InduceIdle = true;
			var host = GetHostToHandleExceptionIdleOrComplete (wf);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
			RunAgain (host);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			Assert.AreEqual (String.Empty, host.ConsoleOut); // so bookmark doesnt run
			var result = host.Controller_ScheduleBookmarkResumption (bookmark, "resumed");
			Assert.AreEqual (BookmarkResumptionResult.NotFound, result);
		}
		[Test]
		[Timeout (1500)]
		public void State_FaultHandlerOnRootExceptions_OtherChildToPropageFaultTo ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var writeLine = new WriteLine ();
			ActivityInstance wlAI = null, hWAI = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) =>  {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (writeLine);
			}, (context, callback) =>  {
				wlAI = context.ScheduleActivity (writeLine);
				hWAI = context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, instance) =>  {
				throw new Exception ();
			});
			var host = GetHostToHandleException (wf);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
			Assert.AreEqual (ActivityInstanceState.Faulted, wlAI.State); //This is the child the fault is propagated to
			Assert.AreEqual (ActivityInstanceState.Faulted, hWAI.State);
		}
		[Test]
		[Timeout (1500)]
		public void State_FaultHandlerOnRootExceptions ()
		{
			var helloWorldEx = new HelloWorldEx ();
			ActivityInstance ai = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) =>  {
				metadata.AddChild (helloWorldEx);
			}, (context, callback) =>  {
				ai = context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, instance) =>  {
				throw new Exception ();
			});
			var host = GetHostToHandleException (wf);
			InitRunWait (host);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
		}
		#endregion
		[Test]
		public void AnonymousMethod_CanAccessParamsOk ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (helloWorldEx, (NativeActivityFaultContext faultCtx, 
									 Exception ex, ActivityInstance instance) => {
					ex.ToString ();
					instance.ToString ();
					Console.WriteLine ("anonRan");
					faultCtx.HandleFault ();
				});
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual ("anonRan" + Environment.NewLine, app.ConsoleOut);
		}
		Exception TryScheduleWithFault (NativeActivityContext context, Activity activity, FaultCallback callback)
		{
			try {
				context.ScheduleActivity (activity, callback);
			} catch (Exception ex) {
				return ex;
			}
			return null;
		}
		[Test]
		[Ignore ("Closed variable callback delegate validation")]
		public void AnonymousMethod_ClosedVariablesNotOK ()
		{
			var writeLine = new WriteLine ();
			Exception exception = null, t1 = null, t2 = null, t3 = null, t4 = null, t5 = null;
			var v1 = new Variable<string> ("name", "value");
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLine);
				metadata.AddImplementationVariable (v1);
			}, (context) => {
				t1 = TryScheduleWithFault (context, writeLine, (faultCtx, ex, instance) => {
					faultCtx.GetValue (v1); // fails
				});
				t2 = TryScheduleWithFault (context, writeLine, (faultCtx, ex, instance) => {
					exception = ex; // fails
				});
				t3 = TryScheduleWithFault (context, writeLine, (faultCtx, ex, instance) => {
					var myex = ex; // ok
				});
				t4 = TryScheduleWithFault (context, writeLine, (faultCtx, ex, instance) => {
					var myex = ex;
					exception = myex; // fails
				});
				t5 = TryScheduleWithFault (context, writeLine, (faultCtx, ex, instance) => {
					var r = exception; // fails
				});
			});
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.IsInstanceOfType (typeof (ArgumentException), t1);
			Assert.IsInstanceOfType (typeof (ArgumentException), t2);
			Assert.IsNull (t3);
			Assert.IsInstanceOfType (typeof (ArgumentException), t4);
			Assert.IsInstanceOfType (typeof (ArgumentException), t5);
		}
		[Test]
		public void ChildFaults ()
		{
			var helloWorldEx = new HelloWorldEx ();
			FaultState fState = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context, callback) => {
				context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, instance) => {
				fState = new FaultState (ex, instance);
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			fState.AssertFault (true, helloWorldEx.IThrow, "2");
		}
		[Test]
		public void GrandChildFaults ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wrapper = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (helloWorldEx);
			});

			FaultState fState = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (wrapper);
			}, (context, callback) => {
				context.ScheduleActivity (wrapper, callback);
			}, (context, ex, instance) => {
				fState = new FaultState (ex, instance);
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			fState.AssertFault (false, helloWorldEx.IThrow, "2");
		}
		[Test]
		public void GrandChildFaults_OtherGrandChildrenStillRun ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var writer = new WriteLine { Text = "writer" };
			var wrapper = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (writer);
			}, (context) => {
				context.ScheduleActivity (writer);
				context.ScheduleActivity (helloWorldEx); //runs first
			});
			ActivityInstance ai = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (wrapper);
			}, (context, callback) => {
				ai = context.ScheduleActivity (wrapper, callback);
			}, (context, ex, instance) => {
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual ("writer" + Environment.NewLine, app.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Closed, ai.State);
		}
		static NativeActWithFaultCBRunner GetWFToBubbleFault (out HelloWorldEx helloWorldEx, string whereToHandle)
		{
			if (whereToHandle != "host" && whereToHandle != "child" && whereToHandle != "root")
				throw new ArgumentException ("whereToHandle");
			//cant use out params inside lambdas
			var helloWorldEx2 = new HelloWorldEx ();
			var faultsInCBParentOf1 = new NativeActWithFaultCBRunner ((metadata) =>  {
				metadata.AddChild (helloWorldEx2);
			}, (context, callback) =>  {
				context.ScheduleActivity (helloWorldEx2, callback);
			}, (context, ex, instance) =>  {
				Console.WriteLine ("childFaultCB");
				if (whereToHandle == "child")
					context.HandleFault ();
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) =>  {
				metadata.AddChild (faultsInCBParentOf1);
			}, (context, callback) =>  {
				context.ScheduleActivity (faultsInCBParentOf1, callback);
			}, (context, ex, instance) =>  {
				Console.WriteLine ("rootFaultCB");
				if (whereToHandle == "root")
					context.HandleFault ();
			});
			helloWorldEx = helloWorldEx2;
			return wf;
		}
		[Test]
		public void BubblesThroughCBs_ToHost ()
		{
			HelloWorldEx helloWorldEx;
			var wf = GetWFToBubbleFault (out helloWorldEx, "host");
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.UnhandledException);
			Assert.AreEqual (String.Format ("childFaultCB{0}rootFaultCB{0}", Environment.NewLine), 
					 app.ConsoleOut);
			Assert.AreEqual (helloWorldEx, app.ExceptionSource);
			Assert.AreEqual ("3", app.ExceptionSourceIndtanceId);
			Assert.AreSame (helloWorldEx.IThrow, app.UnhandledException);
		}
		[Test]
		public void BubblesThroughCBs_ChildHandles ()
		{
			HelloWorldEx helloWorldEx;
			var wf = GetWFToBubbleFault (out helloWorldEx, "child");
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("childFaultCB{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void BubblesThroughCBs_RootHandles ()
		{
			HelloWorldEx helloWorldEx;
			var wf = GetWFToBubbleFault (out helloWorldEx, "root");
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("childFaultCB{0}rootFaultCB{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void DeeperFaultHandlerThrows_BeforeHandleFaultCalled ()
		{
			ThrowFaultFromDeeperFaultHandler (false);
		}
		[Test]
		public void DeeperFaultHandlerThrows_AfterHandleFaultCalled ()
		{
			ThrowFaultFromDeeperFaultHandler (true);
		}
		public void ThrowFaultFromDeeperFaultHandler (bool callFaultHandlerFirst)
		{
			// the exception thrown from deeper fault handler is passed to root handler, not HelloWorldEx's
			var helloWorldEx = new HelloWorldEx ();
			Exception faultThrow = new Exception ();
			FaultState fState = null;
			var faultsInCBParentOf1 = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context, callback) => {
				context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("childFaultCB");
				if (callFaultHandlerFirst)
					context.HandleFault ();
				throw faultThrow;
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (faultsInCBParentOf1);
			}, (context, callback) => {
				context.ScheduleActivity (faultsInCBParentOf1, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("rootFaultCB");
				fState = new FaultState (ex, instance);
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("childFaultCB{0}rootFaultCB{0}", Environment.NewLine), app.ConsoleOut);
			fState.AssertFault (true, faultThrow, "2");
		}
		[Test]
		public void FaultPropagatesToChildren ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var writer = new WriteLine { Text = "writer" };

			ActivityInstance writerAI = null;
			var faultsInCBParentOf2 = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (writer);
			}, (context, callback) => {
				writerAI = context.ScheduleActivity (writer);
				context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("childFaultCB");
				context.HandleFault (); //doesnt matter if this is called or not
				throw new Exception (); //makes this activity fault
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (faultsInCBParentOf2);
			}, (context, callback) => {
				context.ScheduleActivity (faultsInCBParentOf2, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("rootFaultCB");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("childFaultCB{0}rootFaultCB{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Faulted, writerAI.State);
			Assert.IsTrue (writerAI.IsCompleted);
		}
		[Test]
		public void FaultPropagatesToChildren_BookmarksAndResumptionsRemoved ()
		{
			Bookmark parentBookmark = null, deepestBookamrk = null;
			var helloWorldEx = new HelloWorldEx ();
			var bookmarker = new NativeActivityRunner (null, (context) => {
				var b1 = context.CreateBookmark (writeValueBookCB);
				deepestBookamrk = context.CreateBookmark (); // active bookmark will be removed
				context.ResumeBookmark (b1, "childResumed"); // resumption wont go ahead
				context.ResumeBookmark (parentBookmark, "parResumed"); // this resumption will go ahead
			});
			bookmarker.InduceIdle = true;
			ActivityInstance ai = null;
			var faultsInCBParentOf2 = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (bookmarker);
			}, (context, callback) => {
				context.ScheduleActivity (helloWorldEx, callback);
				ai = context.ScheduleActivity (bookmarker); // this will run first and be blocking when ex thrown
			}, (context, ex, instance) => {
				Console.WriteLine ("childFaultCB");
				context.HandleFault (); //doesnt matter if this is called or not
				throw new Exception ();
			});
			BookmarkResumptionResult resumption = (BookmarkResumptionResult)(-1);
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (faultsInCBParentOf2);
			}, (context, callback) => {
				context.ScheduleActivity (faultsInCBParentOf2, callback);
				parentBookmark = context.CreateBookmark (writeValueBookCB);
			}, (context, ex, instance) => {
				Console.WriteLine ("rootFaultCB");
				resumption = context.ResumeBookmark (deepestBookamrk, "shouldfail");
				context.HandleFault ();
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("childFaultCB{0}rootFaultCB{0}parResumed{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
			Assert.IsTrue (ai.IsCompleted);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumption);
		}
		[Test]
		public void FaultPropagatesToChildren_TheirFaultCallbacksDontRun ()
		{
			Bookmark parentBookmark = null;
			var resumeAndBlock = new NativeActivityRunner (null, (context) => {
				context.ResumeBookmark (parentBookmark, "parResumed"); // this activity doesnt cause fault
				context.CreateBookmark (); // blocks
			});
			resumeAndBlock.InduceIdle = true;
			ActivityInstance deepestAI = null;
			var schedulesAndCatches = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (resumeAndBlock);
			}, (context, callback) => {
				deepestAI = context.ScheduleActivity (resumeAndBlock, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("childFaultCB");
				context.HandleFault ();
			});
			var schedulesAndBookmarks = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (schedulesAndCatches);
			}, (context) => {
				context.ScheduleActivity (schedulesAndCatches);
				parentBookmark = context.CreateBookmark ((ctx, bk, value) => {
					Console.WriteLine ((string) value);
					throw new Exception ();
				});
			});
			schedulesAndBookmarks.InduceIdle = true;
			ActivityInstanceState deepestAIStateInFaultCB = (ActivityInstanceState)(-1);
			bool deepestAICompletedInFaultCB = false;
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (schedulesAndBookmarks);
			}, (context, callback) => {
				context.ScheduleActivity (schedulesAndBookmarks, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("rootFaultCB");
				deepestAIStateInFaultCB = deepestAI.State;
				deepestAICompletedInFaultCB = deepestAI.IsCompleted;
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("parResumed{0}rootFaultCB{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Faulted, deepestAI.State);
			Assert.AreEqual (ActivityInstanceState.Faulted, deepestAIStateInFaultCB);
			Assert.IsTrue (deepestAICompletedInFaultCB);
		}
		[Test]
		public void FaultHandlerThrows_ToHost ()
		{
			var helloWorldEx = new HelloWorldEx ();
			Exception faultThrow = new Exception ();
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context, callback) => {
				context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("rootFaultCB");
				throw faultThrow;
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.UnhandledException);
			Assert.AreEqual (String.Format ("rootFaultCB{0}", Environment.NewLine), app.ConsoleOut);

			Assert.AreEqual (helloWorldEx, app.ExceptionSource);
			Assert.AreEqual ("2", app.ExceptionSourceIndtanceId);
			Assert.AreSame (faultThrow, app.UnhandledException);
		}
		[Test]
		public void CompletionCallbackThrows ()
		{
			var writeLine = new WriteLine ();
			Exception faultThrow = new Exception ();
			FaultState fState = null;
			var scheduleWithCB = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine, callback);
			}, (context, instance, callback) => {
				throw faultThrow;
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (scheduleWithCB);
			}, (context, callback) => {
				context.ScheduleActivity (scheduleWithCB, callback);
			}, (context, ex, instance) => {
				fState = new FaultState (ex, instance);
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			fState.AssertFault (true, faultThrow, "2");
		}
		[Test]
		public void CompletionCallbackRunsOnFaultAndShowsChildStateIsFaulted ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context, callback) => {
				context.ScheduleActivity (helloWorldEx, (ctx, compInstance) => {
					Console.WriteLine ("CompletionCBRan State:" + compInstance.State);
				}, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("FaultCBRan");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("FaultCBRan{0}CompletionCBRan State:Faulted{0}", Environment.NewLine), 
					 app.ConsoleOut);
		}
		[Test]
		public void CompletionCallbackForGrandChildRunsWhenFaultHandledHigherUp ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var schedulesWithCompCB = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (helloWorldEx, (ctx, compInstance) => {
					Console.WriteLine ("CompletionCBRan State:" + compInstance.State);
				});
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (schedulesWithCompCB);
			}, (context, callback) => {
				context.ScheduleActivity (schedulesWithCompCB, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("FaultCBRan");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("FaultCBRan{0}CompletionCBRan State:Faulted{0}", Environment.NewLine), 
					 app.ConsoleOut);
		}
		[Test]
		public void CompletionCallbackForGrandChildDoesntRunWhenFaultHandlerHigherUpThrows ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var schedulesWithCompCB = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (helloWorldEx, (ctx, compInstance) => {
					Console.WriteLine ("helloWorldCompCBRan State:" + compInstance.State);
				});
			});
			var faultingHandler = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (schedulesWithCompCB);
			}, (context, callback) => {
				context.ScheduleActivity (schedulesWithCompCB, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("FaultingCBRan");
				context.HandleFault ();
				throw new Exception ();
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (faultingHandler);
			}, (context, faultcb) => {
				context.ScheduleActivity (faultingHandler, (ctx, compInstance) => {
					Console.WriteLine ("faultingHandlerCompCBRan State:" + compInstance.State);
				}, faultcb);
			}, (context, ex, instance) => {
				Console.WriteLine ("HandlingCBRan");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("FaultingCBRan{0}HandlingCBRan{0}" +
				"faultingHandlerCompCBRan State:Faulted{0}", Environment.NewLine), 
					 app.ConsoleOut);
		}
		[Test]
		public void CompletionCallback_NoFault ()
		{
			var writeLine = new WriteLine ();
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine, (ctx, compInstance) => {
					Console.WriteLine ("CBRan");
				}, callback);
			}, (context, ex, instance) => {
				Console.WriteLine ("FaultCBRan");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (Environment.NewLine + "CBRan" + Environment.NewLine, app.ConsoleOut);
		}
		[Test]
		public void BookmarkThrowsWhenResumedFromSameActivity ()
		{
			Exception faultThrow = new Exception ();
			FaultState fState = null;
			var exceptionalBookmarker = new NativeActWithBookmarkRunner (null, (context, callback) => {
				var b = context.CreateBookmark (callback);
				context.ResumeBookmark (b, null);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("BookmarkRan");
				throw faultThrow;
			}); 
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (exceptionalBookmarker);
			}, (context, callback) => {
				context.ScheduleActivity (exceptionalBookmarker, callback);
			}, (context, ex, instance) => {
				fState = new FaultState (ex, instance);
				Console.WriteLine ("FaultCBRan");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			fState.AssertFault (true, faultThrow, "2");
			Assert.AreEqual (String.Format ("BookmarkRan{0}FaultCBRan{0}", Environment.NewLine),
					 app.ConsoleOut);
		}
		[Test]
		public void BookmarkThrowsWhenResumedFromDeeperActivity ()
		{
			Exception faultThrow = new Exception ();
			FaultState fState = null;
			Bookmark bookmark = null;
			ActivityInstance aiResumer = null, aiBookmarker = null;
			var bookmarker = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ();
			}); 
			bookmarker.InduceIdle = true;
			var resumer = new NativeActivityRunner (null, (context) => {
				context.ResumeBookmark (bookmark, null);
			}); 
			var bookmarkerAndScheduler = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (resumer);
				metadata.AddChild (bookmarker);
			}, (context, callback) => {
				bookmark = context.CreateBookmark (callback);
				aiBookmarker = context.ScheduleActivity (bookmarker);
				aiResumer = context.ScheduleActivity (resumer);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("BookmarkRan");
				throw faultThrow;
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (bookmarkerAndScheduler);
			}, (context, callback) => {
				context.ScheduleActivity (bookmarkerAndScheduler, callback);
			}, (context, ex, instance) => {
				fState = new FaultState (ex, instance);
				Console.WriteLine ("FaultCBRan");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			fState.AssertFault (true, faultThrow, "2");
			Assert.AreEqual (ActivityInstanceState.Faulted, aiBookmarker.State);
			Assert.IsTrue (aiBookmarker.IsCompleted);
			Assert.AreEqual (ActivityInstanceState.Closed, aiResumer.State);
			Assert.AreEqual (String.Format ("BookmarkRan{0}FaultCBRan{0}", Environment.NewLine),
					 app.ConsoleOut);
		}
		[Test]
		public void BookmarkThrowsWhenResumedFromAncestorActivity ()
		{
			Exception faultThrow = new Exception ();
			FaultState fState = null;
			Bookmark childBookmark = null, parentBookmark = null;
			ActivityInstance aiParent = null, aiChild = null;
			var child = new NativeActWithBookmarkRunner (null, (context, callback) => {
				childBookmark = context.CreateBookmark (callback);
				context.ResumeBookmark (parentBookmark, null);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("ChildBookmarkRan");
				throw faultThrow;
			});
			var parent = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				parentBookmark = context.CreateBookmark (callback);
				aiChild = context.ScheduleActivity (child);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("ParBookmarkRan");
				context.ResumeBookmark (childBookmark, null);
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (parent);
			}, (context, callback) => {
				aiParent = context.ScheduleActivity (parent, callback);
			}, (context, ex, instance) => {
				fState = new FaultState (ex, instance);
				Console.WriteLine ("FaultCBRan");
				context.HandleFault ();
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			fState.AssertFault (false, faultThrow, "2");
			Assert.AreEqual (ActivityInstanceState.Faulted, aiChild.State);
			Assert.IsTrue (aiChild.IsCompleted);
			Assert.AreEqual (ActivityInstanceState.Closed, aiParent.State);
			Assert.AreEqual (String.Format ("ParBookmarkRan{0}ChildBookmarkRan{0}FaultCBRan{0}", Environment.NewLine),
					 app.ConsoleOut);
		}
	}
}

