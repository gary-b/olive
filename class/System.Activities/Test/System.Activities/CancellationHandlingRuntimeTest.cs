using System;
using NUnit.Framework;
using System.Activities;
using System.IO;
using System.Threading;
using System.Activities.Statements;
using System.Collections.ObjectModel;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class CancellationHandlingRuntimeTest : WFTestHelper {
		static NativeActivityRunner GetSleepWithMsgAct (int secondsToSleep, string execMsg)
		{
			var sleeper = new NativeActivityRunner (null, context =>  {
				Thread.Sleep (TimeSpan.FromSeconds (secondsToSleep));
				Console.WriteLine (execMsg);
			});
			return sleeper;
		}
		static NativeActivityRunner GetSleepWithMsgsAct (int secondsToSleep, string execMsg, string cancelMsg)
		{
			var sleeper = new NativeActivityRunner (null, context =>  {
				Thread.Sleep (TimeSpan.FromSeconds (secondsToSleep));
				Console.WriteLine (execMsg);
			}, cancelMsg);
			return sleeper;
		}
		static NativeActivityRunner GetSleepAndThrowAct (int secsToSleep, string execMsg)
		{
			return new NativeActivityRunner (null, context =>  {
				Thread.Sleep (TimeSpan.FromSeconds (secsToSleep));
				Console.WriteLine (execMsg);
				throw new Exception ();
			});
		}
		static NativeActivityRunner GetSleepAndResumeAct (int secsToSleep, string execMsg, string bookmarkName)
		{
			return new NativeActivityRunner (null, context =>  {
				Thread.Sleep (TimeSpan.FromSeconds (secsToSleep));
				Console.WriteLine (execMsg);
				context.ResumeBookmark (new Bookmark (bookmarkName), null);
			});
		}
		static NativeActivity GetWFLooper (Activity loopAct)
		{
			return new NativeActWithCBRunner (metadata =>  {
				metadata.AddChild (loopAct);
			}, (context, callback) =>  {
				context.ScheduleActivity (loopAct, callback);
			}, (context, compAI, callback) =>  {
				context.ScheduleActivity (loopAct, callback);
			});
		}
		static AutoResetEvent GetWFAppToCancel (Activity wf, out WorkflowApplication app, out StringWriter consoleOut)
		{
			consoleOut = new StringWriter ();
			Console.SetOut (consoleOut);
			var reset = new AutoResetEvent (false);
			app = new WorkflowApplication (wf);
			app.OnUnhandledException = e =>  {
				Console.WriteLine ("UnhandledException: " + e.UnhandledException);
				return UnhandledExceptionAction.Abort;
			};
			app.Completed = e =>  {
				Console.WriteLine (e.CompletionState);
				reset.Set ();
			};
			return reset;
		}
		static void ExecuteLooperAndCancel (int waitUntilCancel)
		{
			int count = 0;
			var counter = new NativeActivityRunner (null, (context) => {
				count++;
			});
			var wf = GetWFLooper (counter);

			StringWriter cOut;
			WorkflowApplication app;
			var reset = GetWFAppToCancel (wf, out app, out cOut);

			app.Run ();
			if (waitUntilCancel > 0)
				Thread.Sleep (TimeSpan.FromSeconds(waitUntilCancel));
			app.Cancel ();
			Console.WriteLine ("After Call");
			reset.WaitOne ();
			Assert.AreEqual (String.Format ("After Call{0}Canceled{0}", Environment.NewLine), cOut.ToString ());

			if (waitUntilCancel > 0)
				Assert.IsTrue (count > 100);
			else
				Assert.AreEqual (0, count);
		}
		[Test]
		public void WFApp_CancelImmediately ()
		{
			ExecuteLooperAndCancel (0);
		}
		[Test]
		public void WFApp_CancelAfter1Sec ()
		{
			ExecuteLooperAndCancel (1);
		}
		[Test]
		public void WFApp_NativeActivity_IsCancellationRequested_StaysFalseDuringExecute ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				Console.WriteLine (context.IsCancellationRequested);
				Thread.Sleep (TimeSpan.FromSeconds (2));
				Console.WriteLine (context.IsCancellationRequested);
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("False{0}False{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void WFApp_CancellationRequested_DuringSequence_StopsAfterCurrentlyExecutingActivity ()
		{
			var wf = new Sequence {
				Activities =  {
					GetWriteLine ("1"),
					GetSleepWithMsgAct (2, "2:sleeper"),
					GetWriteLine ("3"),
					GetWriteLine ("4"),
				}
			};

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("1{0}2:sleeper{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Fault_FromActivityExecutingDuringCancel_FirstHandlerStillCalled ()
		{
			var sleepAndThrow = GetSleepAndThrowAct (2, "2:sleeper");

			var seq = new Sequence {
				Activities =  {
					GetWriteLine ("1"),
					sleepAndThrow,
					GetWriteLine ("3"),
				}
			};

			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (seq);
			}, (context, callback) => {
				context.ScheduleActivity (seq, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("FaultHandled:IsCancelReq=" + context.IsCancellationRequested);
				context.HandleFault ();
			}, "wf cancelled");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("1{0}2:sleeper{0}wf cancelled{0}FaultHandled:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Fault_FromActivityExecutingDuringCancel_CancellationsProcessedBeforeFaultHandler ()
		{
			var sleepAndThrow = GetSleepAndThrowAct (2, "sleeper");
			var neverExecuted = GetMsgsAct ("neverExecutes Execute", "neverExecutes Cancel");
			var executesAndBlocks = GetBlocksWithMsgsAct ("executesAndBlocks", "executesAndBlocks Cancel");
			ActivityInstance ai1 = null, ai2 = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (sleepAndThrow);
				metadata.AddChild (executesAndBlocks);
				metadata.AddChild (neverExecuted);
			}, (context, callback) => {
				ai1 = context.ScheduleActivity (neverExecuted);
				context.ScheduleActivity (sleepAndThrow, callback);
				ai2 = context.ScheduleActivity (executesAndBlocks);
			}, (context, ex, ai) => {
				Console.WriteLine ("FaultHandled:IsCancelReq=" + context.IsCancellationRequested);
				context.HandleFault ();
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai1.State);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai2.State);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("executesAndBlocks{0}" +
			                                "sleeper{0}" +
			                                "executesAndBlocks Cancel{0}" +
			                                "FaultHandled:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);

		}
		[Test]
		public void FaultHandler_ExecutingDuringCancel_CancellationProcessedNext ()
		{
			var helloWorldEx = new HelloWorldEx ();

			var child = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context, callback) => {
				context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, ai) => {
				Thread.Sleep (TimeSpan.FromSeconds (2));
				Console.WriteLine ("child fault:IsCancelReq=" + context.IsCancellationRequested);
			}, "child cancel");

			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("wf fault:IsCancelReq=" + context.IsCancellationRequested);
				context.HandleFault ();
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("child fault:IsCancelReq=False{0}" +
			                                "wf cancel{0}" +
			                                "child cancel{0}" +
				                        "wf fault:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Fault_FromActivityExecutingDuringCancel_DoesntBubblePast1stFaultHandler ()
		{
			//The activity 'NativeActWithFaultCBRunner' with ID 2 threw or propagated an exception while being canceled.
			var sleepAndThrow = GetSleepAndThrowAct (2, "sleeper");

			var child = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (sleepAndThrow);
			}, (context, callback) => {
				context.ScheduleActivity (sleepAndThrow, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("child fault:IsCancelReq=" + context.IsCancellationRequested);
			}, "child cancel");

			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("wf fault:IsCancelReq=" + context.IsCancellationRequested);
				context.HandleFault ();
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Aborted, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}" +
			                                "wf cancel{0}" +
			                                "child cancel{0}" +
			                                "child fault:IsCancelReq=True{0}",
			                                Environment.NewLine), app.ConsoleOut);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), app.AbortReason);
		}
		[Test]
		public void CancelMethod_Base_CantScheduleActivity_FromFaultHandler ()
		{
			bool completed = false;
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			var writer = GetWriteLine ("never runs");
			var sleepAndThrow = GetSleepAndThrowAct (2, "sleeper");

			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (sleepAndThrow);
				metadata.AddChild (writer);
			}, (context, callback) => {
				context.ScheduleActivity (sleepAndThrow, callback);
			}, (context, ex, propAI) => {
				var ai = context.ScheduleActivity (writer);
				completed = ai.IsCompleted;
				state = ai.State;
				Console.WriteLine ("FaultHandled:IsCancelReq=" + context.IsCancellationRequested);
				context.HandleFault ();
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}FaultHandled:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
			Assert.IsTrue (completed);
			Assert.AreEqual (ActivityInstanceState.Canceled, state);
		}
		[Test]
		public void CompletionCallback_ForActivityCancelledDuringExecution_StillExecutes ()
		{
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var neverruns = GetWriteLine ("never runs");
			ActivityInstanceState state = (ActivityInstanceState) (-1);
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (neverruns);
				metadata.AddChild (sleeper);
			}, (context, callback) => {
				context.ScheduleActivity (neverruns);
				context.ScheduleActivity (sleeper, callback);
			}, (context, compAI, callback) => {
				state = compAI.State;
				Console.WriteLine ("CompCBRan:IsCancelReq=" + context.IsCancellationRequested);
			}, "wf cancelled");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.AreEqual (String.Format ("sleeper{0}wf cancelled{0}CompCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CompletionCallback_ForActivityCancelledBeforeExecution_StillExecutes ()
		{
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var neverExecuted = GetMsgsAct ("neverExecuted execute", "neverExecuted cancel");
			ActivityInstanceState state = (ActivityInstanceState) (-1);
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (neverExecuted);
				metadata.AddChild (sleeper);
			}, (context, callback) => {
				context.ScheduleActivity (neverExecuted, callback);
				context.ScheduleActivity (sleeper);
			}, (context, compAI, callback) => {
				state = compAI.State;
				Console.WriteLine ("CompCBRan:IsCancelReq=" + context.IsCancellationRequested);
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (ActivityInstanceState.Canceled, state);
			Assert.AreEqual (String.Format ("sleeper{0}CompCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CompletionCallback_ForActivityCancelledDuringExecution_OtherActivitiesCancelFirst ()
		{
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var neverExecuted = GetMsgsAct ("neverExecutes Execute", "neverExecutes Cancel");
			var executesAndBlocks = GetBlocksWithMsgsAct ("executesAndBlocks", "executesAndBlocks Cancel");
			ActivityInstance ai1 = null, ai2 = null;
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (executesAndBlocks);
				metadata.AddChild (sleeper);
				metadata.AddChild (neverExecuted);
			}, (context, callback) => {
				ai1 = context.ScheduleActivity (neverExecuted);
				context.ScheduleActivity (sleeper, callback);
				ai2 = context.ScheduleActivity (executesAndBlocks);
			}, (context, compAI, callback) => {
				Console.WriteLine ("CompCBRan:IsCancelReq=" + context.IsCancellationRequested);
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai1.State);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai2.State);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("executesAndBlocks{0}" +
			                                "sleeper{0}" +
			                                "wf cancel{0}" + 
			                                "executesAndBlocks Cancel{0}" +
			                                "CompCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);

		}
		[Test]
		public void CancelMethod_Base_CantScheduleActivity_FromCompletionCB ()
		{
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var writer = GetWriteLine ("never runs");
			bool completed = false;
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (writer);
				metadata.AddChild (sleeper);
			}, (context, callback) => {
				context.ScheduleActivity (sleeper, callback);
			}, (context, compAI, callback) => {
				var ai = context.ScheduleActivity (writer);
				completed = ai.IsCompleted;
				state = ai.State;
				Console.WriteLine ("CompCBRan:IsCancelReq=" + context.IsCancellationRequested);
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}CompCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
			Assert.IsTrue (completed);
			Assert.AreEqual (ActivityInstanceState.Canceled, state);
		}
		[Test]
		public void CancelMethod_Base_CantScheduleActivity_FromBookmarkCB ()
		{
			var writer = GetWriteLine ("never runs");
			bool completed = false;
			ActivityInstanceState state = (ActivityInstanceState)(-1);

			var sleepAndResume = GetSleepAndResumeAct (2, "sleeper", "bk1");

			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writer);
				metadata.AddChild (sleepAndResume);
			}, (context, callback) => {
				context.CreateBookmark ("bk1", callback);
				context.ScheduleActivity (sleepAndResume);
			}, (context, bk, value, callback) => {
				var ai = context.ScheduleActivity (writer);
				state = ai.State;
				completed = ai.IsCompleted;
				Console.WriteLine ("BookCBRan:IsCancelReq=" + context.IsCancellationRequested);
			}/*, (context) => {
				context.RemoveAllBookmarks ();
				context.CancelChildren ();
				context.MarkCanceled ();
			}*/);

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}BookCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
			Assert.IsTrue (completed);
			Assert.AreEqual (ActivityInstanceState.Canceled, state);
		}
		static NativeActivity GetBlocksWithMsgsAct (string executeMsg, string cancelMsg)
		{
			var executesAndBlocks = new NativeActivityRunner (null, (context) =>  {
				context.CreateBookmark ();
				Console.WriteLine (executeMsg);
			}, cancelMsg);
			executesAndBlocks.InduceIdle = true;
			return executesAndBlocks;
		}

		static NativeActivity GetMsgsAct (string executeMsg, string cancelMsg)
		{
			return new NativeActivityRunner (null, (context) => {
				Console.WriteLine (executeMsg);
			}, cancelMsg);
		}
		[Test]
		public void BookmarkResumption_SetFromActivityCancelledDuringExecuting_StillExecutes ()
		{
			var writer = GetWriteLine ("never runs");
			var sleepAndResume = GetSleepAndResumeAct (2, "sleeper", "bk1");

			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writer);
				metadata.AddChild (sleepAndResume);
			}, (context, callback) => {
				context.CreateBookmark ("bk1", callback);
				context.ScheduleActivity (writer);
				context.ScheduleActivity (sleepAndResume);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("BookCBRan:IsCancelReq=" + context.IsCancellationRequested);
			}, "wf cancelled");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}wf cancelled{0}BookCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void BookmarkResumption_SetBeforeCancellation_StillExecutes ()
		{
			var sleepAndResume = GetSleepWithMsgAct (2, "sleeper");

			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (sleepAndResume);
			}, (context, callback) => {
				var bk = context.CreateBookmark ("bk1", callback);
				context.ResumeBookmark (bk, null);
				context.ScheduleActivity (sleepAndResume);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("BookCBRan:IsCancelReq=" + context.IsCancellationRequested);
			}, "wf cancelled");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}wf cancelled{0}BookCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void BookmarkResumption_SetFromActivityCancelledDuringExecuting_CancellationsProcessedFirst ()
		{
			var neverExecuted = GetMsgsAct ("neverExecutes Execute", "neverExecutes Cancel");
			var executesAndBlocks = GetBlocksWithMsgsAct ("executesAndBlocks", "executesAndBlocks Cancel");
			var sleepAndResume = GetSleepAndResumeAct (2, "sleeper", "bk1");

			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (executesAndBlocks);
				metadata.AddChild (neverExecuted);
				metadata.AddChild (sleepAndResume);
			}, (context, callback) => {
				context.CreateBookmark ("bk1", callback);
				context.ScheduleActivity (neverExecuted);
				context.ScheduleActivity (sleepAndResume);
				context.ScheduleActivity (executesAndBlocks);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("BookCBRan:IsCancelReq=" + context.IsCancellationRequested);
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.IsNull (app.UnhandledException);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("executesAndBlocks{0}" +
							"sleeper{0}" +
			                                "wf cancel{0}" +
							"executesAndBlocks Cancel{0}" +
							"BookCBRan:IsCancelReq=True{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_CanCreateAndResumeSubsequentBookmarks ()
		{
			var sleepAndResume = GetSleepAndResumeAct (2, "sleeper", "bk1");

			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (sleepAndResume);
			}, (context, callback) => {
				context.CreateBookmark ("bk1", callback);
				context.ScheduleActivity (sleepAndResume);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("BookCBRan:IsCancelReq=" + context.IsCancellationRequested);
				var bookmark = context.CreateBookmark ((ctx, bk2, value2) => {
					Console.WriteLine ("subsequent BookCBRan");
				});
				context.ResumeBookmark (bookmark, null);
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.IsNull (app.UnhandledException);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}" +
			                                "wf cancel{0}" +
			                                "BookCBRan:IsCancelReq=True{0}" +
			                                "subsequent BookCBRan{0}", Environment.NewLine), app.ConsoleOut);
		}

		static NativeActivity GetSched2ChildrenMsgsAct (string executeMsg, string cancelMsg, Activity child1, Activity child2)
		{
			var wf = new NativeActivityRunner (metadata =>  {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, context =>  {
				Console.WriteLine (executeMsg);
				context.ScheduleActivity (child2);
				context.ScheduleActivity (child1);
			}, cancelMsg);
			return wf;
		}
		static NativeActivity GetSched1ChildMsgsAct (string executeMsg, string cancelMsg, Activity child1)
		{
			var wf = new NativeActivityRunner (metadata =>  {
				metadata.AddChild (child1);
			}, context =>  {
				Console.WriteLine (executeMsg);
				context.ScheduleActivity (child1);
			}, cancelMsg);
			return wf;
		}
		[Test]
		public void Propagation ()
		{
			var child1child1 = GetBlocksWithMsgsAct ("1.1:E", "1.1:C");
			var child1child2 = GetBlocksWithMsgsAct ("1.2:E", "1.2:C");
			var child2child1 = GetBlocksWithMsgsAct ("2.1:E", "2.1:C");
			var child2child2 = GetBlocksWithMsgsAct ("2.2:E", "2.2:C");

			var child1 = GetSched2ChildrenMsgsAct ("1:E", "1:C", child1child1, child1child2);

			var child2 = GetSched2ChildrenMsgsAct ("2:E", "2:C", child2child1, child2child2);

			var wf = GetSched2ChildrenMsgsAct ("wf:E", "wf:C", child1, child2);

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("wf:E{0}1:E{0}1.1:E{0}1.2:E{0}" +
			                                "2:E{0}2.1:E{0}2.2:E{0}" +
			                                "wf:C{0}1:C{0}1.1:C{0}1.2:C{0}" +
			                                "2:C{0}2.1:C{0}2.2:C{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_NoChildrenCallMarkedCancelled_State_Completes ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (false);

			var child2 = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("2");
			}, (context) => {
				//do nothing
			});
			child2.InduceIdle = true;

			var wf = GetSched2ChildrenMsgsAct ("wf:E", "wf:C", child1, child2);

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark ("2", null);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("wf:E{0}" +
			                                "wf:C{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_SingleChildCallsMarkedCancelled_State_Cancelled ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (true);

			var child2 = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("2");
			}, (context) => {
				//do nothing
			});
			child2.InduceIdle = true;

			var wf = GetSched2ChildrenMsgsAct ("wf:E", "wf:C", child1, child2);

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark ("2", null);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("wf:E{0}" +
			                                "wf:C{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_ChildCallsMarkedCancelled_State_Cancelled ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (true);

			var wf = GetSched1ChildMsgsAct ("wf:E", "wf:C", child1);

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("wf:E{0}" +
			                                "wf:C{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_ChildDoesntCallMarkedCancelled_State_Complete ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (false);

			var wf = GetSched1ChildMsgsAct ("wf:E", "wf:C", child1);

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("wf:E{0}" +
			                                "wf:C{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_NotMarkedCancelled_ChildCallsMarkedCancelled_State_Complete ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (true);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				Console.WriteLine ("wf:E");
				context.ScheduleActivity (child1);
			}, (context) => {
				Console.WriteLine ("wf:C");
				context.CancelChildren ();
			});

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("wf:E{0}" +
			                                "wf:C{0}", Environment.NewLine), app.ConsoleOut);
		}

		static NativeActivityRunner GetBlocksOnCancelRemovesBkAct (bool markCancelled)
		{
			var child1 = new NativeActivityRunner (null, context =>  {
				context.CreateBookmark ("1");
			}, context =>  {
				context.RemoveAllBookmarks ();
				if (markCancelled)
					context.MarkCanceled ();
			});
			child1.InduceIdle = true;
			return child1;
		}

		[Test]
		public void CancelMethod_MarkedCancelled_ChildCallsMarkedCancelled_State_Cancelled ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (true);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				Console.WriteLine ("wf:E");
				context.ScheduleActivity (child1);
			}, (context) => {
				Console.WriteLine ("wf:C");
				context.CancelChildren ();
				context.MarkCanceled ();
			});
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("wf:E{0}" +
			                                "wf:C{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void NotMarkedCancelled_CanScheduleActivityOnBookmarkResumption ()
		{
			var writeLine = GetWriteLine ("writeline");
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context, callback) => {
				Console.WriteLine ("execute");
				context.CreateBookmark ("bk", callback);
			}, (context, bk, value, callback) => {
				context.ScheduleActivity (writeLine);
			}, (context) => {
				Console.WriteLine ("cancel");
				//do nothing
			});

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark ("bk", null);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("execute{0}cancel{0}" +
			                                "writeline{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void MarkedCancelled_CanScheduleActivityOnBookmarkResumption ()
		{
			var writeLine = GetWriteLine ("writeline");
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context, callback) => {
				Console.WriteLine ("execute");
				context.CreateBookmark ("bk", callback);
			}, (context, bk, value, callback) => {
				context.ScheduleActivity (writeLine);
			}, (context) => {
				Console.WriteLine ("cancel");
				context.MarkCanceled ();
			});

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark ("bk", null);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("execute{0}cancel{0}" +
			                                "writeline{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void MarkCancelled_CantCallIfNotIsCancellationRequested ()
		{
			//Only activities which have been requested to cancel can call MarkCanceled.  Check ActivityInstance.HasCancelBeenRequested before calling this method.
			Exception exception = null;
			var wf = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("child executed");
				try {
					context.MarkCanceled ();
				} catch (Exception ex) {
					exception = ex;
				}
			});

			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), exception);
		}
		[Test]
		public void MarkCancelled_CanCallIfIsCancellationRequested ()
		{
			//Only activities which have been requested to cancel can call MarkCanceled.  Check ActivityInstance.HasCancelBeenRequested before calling this method.

			var wf = new NativeActWithBookmarkRunner (null, (context, callback) => {
				var bk = context.CreateBookmark (callback);
				Thread.Sleep (TimeSpan.FromSeconds (2));
				context.ResumeBookmark (bk, null);
				Console.WriteLine ("execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("bookmark");
				context.MarkCanceled ();
			}, (context) => {
				Console.WriteLine ("cancel");
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (String.Format ("execute{0}cancel{0}bookmark{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
		}
		[Test]
		public void Propagation_CancelMethodNotCalledIfCompleteWithNoChildren_Root ()
		{
			var wf = GetSleepWithMsgsAct (2, "execute", "cancel");
			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("execute{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Propagation_CancelMethodNotCalledIfCompleteWithNoChildren_NotRoot ()
		{
			var child = GetSleepWithMsgsAct (2, "child execute", "child cancel");
			var wf = GetSched1ChildMsgsAct ("wf execute", "wf cancel", child);

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("wf execute{0}child execute{0}wf cancel{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Propagation_CancelMethodNotCalledIfBookmarkResumptionInProgress ()
		{
			var wf = new NativeActWithBookmarkRunner (null, (context, callback) => {
				var bk = context.CreateBookmark (callback);
				context.ResumeBookmark (bk, null);
				Console.WriteLine ("execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("bookmark");
				Thread.Sleep (TimeSpan.FromSeconds (2));
			}, (context) => {
				Console.WriteLine ("cancel");
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("execute{0}bookmark{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Propagation_CancelMethodNotCalledIfNonBlockingBookmarkPresent ()
		{
			var wf = new NativeActWithBookmarkRunner (null, (context, callback) => {
				var bk = context.CreateBookmark (callback, BookmarkOptions.NonBlocking);
				Thread.Sleep (TimeSpan.FromSeconds (2));
				Console.WriteLine ("execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("bookmark");
			}, (context) => {
				Console.WriteLine ("cancel");
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("execute{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_RemovesBookmarks_State_Cancel ()
		{
			var wf = GetBlocksWithMsgsAct ("execute", "cancel");
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (String.Format ("execute{0}cancel{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
		}
		[Test]
		public void CancelMethod_Base_RemovesNonBlockingBookmarkChildNotMarkedCancelled_State_Complete ()
		{
			var child = GetBlocksOnCancelRemovesBkAct (false);
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child);
				context.CreateBookmark (callback, BookmarkOptions.NonBlocking);
				Console.WriteLine ("execute");
			}, (context, bk, value, callback) => {

			}, "cancel");
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (String.Format ("execute{0}cancel{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
		}
		[Test]
		public void CancelMethod_CanScheduleActivityAfterMarkedCancelledAsNormal ()
		{
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var eWriter = GetWriteLine ("e writer");
			var cWriter = GetWriteLine ("c writer");
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (eWriter);
				metadata.AddChild (cWriter);
				metadata.AddChild (sleeper);
			}, (context) => {
				context.ScheduleActivity (eWriter);
				context.ScheduleActivity (sleeper);
			}, (context) => {
				context.MarkCanceled ();
				Console.WriteLine ("cancel");
				context.ScheduleActivity (cWriter);
			});
			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}cancel{0}" +
			                                "c writer{0}e writer{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void FaultAfterCancel_CausesAbort_ExFromFaultHandler ()
		{
			//The activity 'NativeActWithFaultCBRunner' with ID 2 threw or propagated an exception while being canceled.
			var sleepAndThrow = GetSleepAndThrowAct (2, "sleeper");

			var child = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (sleepAndThrow);
			}, (context, callback) => {
				context.ScheduleActivity (sleepAndThrow, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("child fault:IsCancelReq=" + context.IsCancellationRequested);
				context.HandleFault ();
				throw new Exception ();
			}, "child cancel");

			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("wf fault:IsCancelReq=" + context.IsCancellationRequested);
				context.HandleFault ();
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Aborted, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}" +
			                                "wf cancel{0}" +
			                                "child cancel{0}" +
			                                "child fault:IsCancelReq=True{0}"
			                                , Environment.NewLine), app.ConsoleOut);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), app.AbortReason);
		}
		[Test]
		public void FaultAfterCancel_CausesAbort_ExFromCancelMethod ()
		{
			//
			Exception exThrow = new Exception ();
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (sleeper);
			}, (context) => {
				context.ScheduleActivity (sleeper);
			}, (context) => {
				context.MarkCanceled ();
				Console.WriteLine ("cancel");
				throw exThrow;
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child, (ctx, ex, ai) => {
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				});
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Aborted, app.Status);

			Assert.AreEqual (String.Format ("sleeper{0}cancel{0}",
			                                Environment.NewLine), app.ConsoleOut);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), app.AbortReason);
			Assert.AreSame (exThrow, app.AbortReason.InnerException);
		}
		[Test]
		public void FaultAfterCancel_CausesAbort_ExFromBookmarkCB ()
		{
			var child1 = new NativeActWithBookmarkRunner (null, (context, callback) => {
				context.CreateBookmark ("childBK", callback);
				context.ResumeBookmark (new Bookmark ("parentBK"), null);
				Console.WriteLine ("child1 execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("childBK");
				throw new Exception ();
			}, (context) => {
				Console.WriteLine ("child cancel");
				context.RemoveAllBookmarks ();
			});

			var state = (ActivityInstanceState)(-1);
			ActivityInstance ai = null;
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.CreateBookmark ("parentBK", callback);
				ai = context.ScheduleActivity (child1, (ctx, propEx, propAI) => {
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				});
				Console.WriteLine ("wf execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("parentBK");
				context.CancelChild (ai);
				context.ResumeBookmark (new Bookmark ("childBK"), null);
				state = ai.State;
			});

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Aborted, app.Status);

			Assert.AreEqual (String.Format ("wf execute{0}" +
			                                "child1 execute{0}" +
			                                "parentBK{0}" +
			                                "child cancel{0}" +
			                                "childBK{0}", 
			                                Environment.NewLine), app.ConsoleOut);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), app.AbortReason);
			//Assert.AreSame (exThrow, abortEx.InnerException);
		}
		[Test]
		public void FaultAfterCancel_CausesAbort_ChildScheduledFromCancelMethod ()
		{
			//The activity 'HelloWorldEx' with ID 4 threw or propagated an exception while being canceled.
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var helloWorldEx = new HelloWorldEx ();
			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (sleeper);
			}, (context) => {
				context.ScheduleActivity (sleeper);
			}, (context) => {
				context.MarkCanceled ();
				Console.WriteLine ("cancel");
				context.ScheduleActivity (helloWorldEx);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child, (ctx, ex, ai) => {
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				});
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Aborted, app.Status);

			Assert.AreEqual (String.Format ("sleeper{0}cancel{0}",
			                                Environment.NewLine), app.ConsoleOut);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), app.AbortReason);
			Assert.AreSame (helloWorldEx.IThrow, app.AbortReason.InnerException);
		}
		[Test]
		public void FaultAfterCancel_CausesAbort_ChildScheduledFromBookmarkCB  ()
		{
			//The activity 'HelloWorldEx' with ID 3 threw or propagated an exception while being canceled.
			var helloWorldEx = new HelloWorldEx ();
			var child1 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context, callback) => {
				context.CreateBookmark ("childBK", callback);
				context.ResumeBookmark (new Bookmark ("parentBK"), null);
				Console.WriteLine ("child1 execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("childBK");
				context.ScheduleActivity (helloWorldEx/*, (ctx, propEx, propAI) => {
					//adding fault handler here would work
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				}*/);
			}, (context) => {
				Console.WriteLine ("child cancel");
				context.RemoveAllBookmarks ();
				//not even marked cancelled
			});

			var state = (ActivityInstanceState)(-1);
			ActivityInstance ai = null;
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.CreateBookmark ("parentBK", callback);
				ai = context.ScheduleActivity (child1, (ctx, propEx, propAI) => {
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				});
				Console.WriteLine ("wf execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("parentBK");
				context.CancelChild (ai);
				context.ResumeBookmark (new Bookmark ("childBK"), null);
				state = ai.State;
			});

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Aborted, app.Status);

			Assert.AreEqual (String.Format ("wf execute{0}" +
			                                "child1 execute{0}" +
			                                "parentBK{0}" +
			                                "child cancel{0}" +
			                                "childBK{0}", Environment.NewLine), app.ConsoleOut);
			//Assert.IsNull (abortEx);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), app.AbortReason);
			//Assert.AreSame (exThrow, abortEx.InnerException);
		}
		[Test]
		public void FaultAfterCancel_ActivityFaultsCanBeCaughtByLastCancelledActivity ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var sleeper = GetSleepWithMsgAct (2, "sleeper");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (sleeper);
			}, (context) => {
				context.ScheduleActivity (sleeper);
			}, (context) => {
				context.MarkCanceled ();
				Console.WriteLine ("wf cancel");
				context.ScheduleActivity (helloWorldEx, (ctx, ex, ai) => {
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				});
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}" +
			                                "wf cancel{0}" +
			                                "fault handled{0}", Environment.NewLine), app.ConsoleOut);

		}
		[Test]
		public void CancelMethod_Base_DoesntStopChildrenScheduling ()
		{
			var child1Writer = GetWriteLine ("child1Writer");
			var child1 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1Writer);
			}, (context, callback) => {
				var bk = context.CreateBookmark (callback);
				Thread.Sleep (TimeSpan.FromSeconds (2));
				context.ResumeBookmark (bk, null);
				Console.WriteLine ("child1execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("child1bookmark");
				context.ScheduleActivity (child1Writer);
			}, (context) => {
				Console.WriteLine ("child1cancel");
				//do nothing
			});
			var child2Writer = GetWriteLine ("child2Writer");
			var child2 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child2Writer);
			}, (context, callback) => {
				var bk = context.CreateBookmark (callback);
				context.ResumeBookmark (bk, null);
				Console.WriteLine ("child2execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("child2bookmark");
				context.ScheduleActivity (child2Writer);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, (context) => {
				context.ScheduleActivity (child1);
				context.ScheduleActivity (child2);
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("child2execute{0}child1execute{0}" +
							"child1cancel{0}child2bookmark{0}" +
			                                "child1bookmark{0}child1Writer{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_StopsActivityDelegatesScheduling ()
		{
			var sleeper = GetSleepAndResumeAct (2, "sleeper", "bk");
			var action = new ActivityAction {
				Handler = GetWriteLine ("action")
			};
			var state = (ActivityInstanceState)(-1);
			string id = null;
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddDelegate (action);
				metadata.AddChild (sleeper);
			}, (context, callback) => {
				context.CreateBookmark ("bk", callback);
				context.ScheduleActivity (sleeper);
			}, (context, bk, value, callback) => {
				Console.WriteLine ("bookmark");
				var ai = context.ScheduleAction (action);
				state = ai.State;
				id = ai.Id;
			}, "cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}cancel{0}bookmark{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual ("0", id);
			Assert.AreEqual (ActivityInstanceState.Canceled, state);
		}
		[Test]
		public void CancelMethod_Base_WFCancelledAfterChildFaultedEarlier_State_Complete ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var sleeper = GetSleepWithMsgAct (2, "sleeper");
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
				metadata.AddChild (sleeper);
			}, (context, callback) => {
				context.ScheduleActivity (sleeper);
				context.ScheduleActivity (helloWorldEx, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("fault");
				context.HandleFault ();
			});
			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);

			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("fault{0}sleeper{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancelMethod_Base_WFCancelledThenExecutingChildFaults_State_Cancelled ()
		{
			var sleeper = GetSleepAndThrowAct (2, "sleeper");
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (sleeper);
			}, (context, callback) => {
				context.ScheduleActivity (sleeper, callback);
			}, (context, ex, ai) => {
				Console.WriteLine ("fault");
				context.HandleFault ();
			});
			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);

			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (String.Format ("sleeper{0}fault{0}", Environment.NewLine), app.ConsoleOut);
		}

		static AutoResetEvent GetWFAppCancelsOnEx (NativeActivityRunner wf, out WorkflowApplication app, out StringWriter consoleOut)
		{
			consoleOut = new StringWriter ();
			Console.SetOut (consoleOut);
			var reset = new AutoResetEvent (false);
			app = new WorkflowApplication (wf);
			app.OnUnhandledException = e =>  {
				Console.WriteLine ("UnhandledException");
				return UnhandledExceptionAction.Cancel;
			};
			app.Completed = e =>  {
				Console.WriteLine (e.CompletionState);
				reset.Set ();
			};
			return reset;
		}
		[Test]
		public void WFApp_UnhandledExceptionAction_Cancel_State_Cancel ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = GetActSchedulesPubChild (helloWorldEx);
			StringWriter consoleOut;
			WorkflowApplication app;
			var reset = GetWFAppCancelsOnEx (wf, out app, out consoleOut);
			app.Run ();
			reset.WaitOne ();
			Assert.AreEqual (String.Format ("UnhandledException{0}Canceled{0}", Environment.NewLine), consoleOut.ToString ());
		}
		[Test]
		public void WFApp_UnhandledExceptionAction_Cancel_IgnoreInWF_State_Complete ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var writeLine = GetWriteLine ("writer");
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLine);
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				Console.WriteLine ("wf execute");
				context.ScheduleActivity (writeLine);
				context.ScheduleActivity (helloWorldEx);
			}, (context) => {
				Console.WriteLine ("wf cancel");
			});
			StringWriter consoleOut;
			WorkflowApplication app;
			var reset = GetWFAppCancelsOnEx (wf, out app, out consoleOut);
			app.Run ();
			reset.WaitOne ();
			Assert.AreEqual (String.Format ("wf execute{0}UnhandledException{0}wf cancel{0}writer{0}Closed{0}", Environment.NewLine), consoleOut.ToString ());
		}
		[Test]
		public void CancelChild_BeforeExecution_ChildCancelNotCalled ()
		{
			var child = GetMsgsAct ("child execute", "child cancel");
			var state = (ActivityInstanceState)(-1);
			ActivityInstance ai = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				ai = context.ScheduleActivity (child);
				context.CancelChild (ai);
				state = ai.State;
				Console.WriteLine ("wf execute");
			});
			RunAndCompare (wf, "wf execute" + Environment.NewLine);
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai.State);
		}
		[Test]
		public void CancelChild_BeforeExecution_CompletionCallbackStillRuns ()
		{
			var writeLine = GetWriteLine ("never runs");
			var state = (ActivityInstanceState)(-1);
			ActivityInstance ai = null;
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context, callback) => {
				ai = context.ScheduleActivity (writeLine, callback);
				context.CancelChild (ai);
				state = ai.State;
			}, (context, compAI, callback) => {
				Console.WriteLine ("compCB");
			});
			RunAndCompare (wf, "compCB" + Environment.NewLine);
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai.State);
		}
		[Test]
		public void CancelChild_DuringExecution_ChildsCancelAndCompletionCBCalled ()
		{
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ();
				context.ResumeBookmark (new Bookmark ("bk"), null);
				Console.WriteLine ("child execute");
			}, (context) => {
				Console.WriteLine ("child cancel IsCancReq:" + context.IsCancellationRequested);
				context.RemoveAllBookmarks ();
				context.MarkCanceled  ();
			});
			child.InduceIdle = true;
			var state = (ActivityInstanceState)(-1);
			ActivityInstance ai = null;
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.CreateBookmark ("bk", callback);
				ai = context.ScheduleActivity (child, (ctx, compAI) => {
					Console.WriteLine ("child callback");
				});
				Console.WriteLine ("wf execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("bookmark");
				context.CancelChild (ai);
				state = ai.State;
			});
			RunAndCompare (wf, String.Format ("wf execute{0}" +
			                                  "child execute{0}" +
			                                  "bookmark{0}" +
			                                  "child cancel IsCancReq:True{0}" +
			                                  "child callback{0}", Environment.NewLine));
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai.State);
		}
		[Test]
		public void CancelChild_DuringExecution_NotMarkedCancelled_State_Completes ()
		{
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ();
				context.ResumeBookmark (new Bookmark ("bk"), null);
				Console.WriteLine ("child execute");
			}, (context) => {
				Console.WriteLine ("child cancel");
				context.RemoveAllBookmarks ();
			});
			child.InduceIdle = true;
			var state = (ActivityInstanceState)(-1);
			ActivityInstance ai = null;
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.CreateBookmark ("bk", callback);
				ai = context.ScheduleActivity (child, (ctx, compAI) => {
					Console.WriteLine ("child callback");
				});
				Console.WriteLine ("wf execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("bookmark");
				context.CancelChild (ai);
				state = ai.State;
			});
			RunAndCompare (wf, String.Format ("wf execute{0}" +
			                                  "child execute{0}" +
			                                  "bookmark{0}" +
			                                  "child cancel{0}" +
			                                  "child callback{0}", Environment.NewLine));
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.AreEqual (ActivityInstanceState.Closed, ai.State);
		}
		[Test]
		public void CancelChild_CanResumeBookmarkAfter ()
		{
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("childBK", (ctx, bk, value) => {
					Console.WriteLine ("childBK");
				});
				context.ResumeBookmark (new Bookmark ("parentBK"), null);
				Console.WriteLine ("child execute");
			}, (context) => {
				Console.WriteLine ("child cancel");
				context.RemoveAllBookmarks ();
			});
			child.InduceIdle = true;
			var state = (ActivityInstanceState)(-1);
			ActivityInstance ai = null;
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.CreateBookmark ("parentBK", callback);
				ai = context.ScheduleActivity (child);
				Console.WriteLine ("wf execute");
			}, (context, bk, value, callback) => {
				Console.WriteLine ("parentBK");
				context.CancelChild (ai);
				context.ResumeBookmark (new Bookmark ("childBK"), null);
				state = ai.State;
			});
			RunAndCompare (wf, String.Format ("wf execute{0}" +
			                                  "child execute{0}" +
			                                  "parentBK{0}" +
			                                  "child cancel{0}" +
			                                  "childBK{0}", Environment.NewLine));
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.AreEqual (ActivityInstanceState.Closed, ai.State);
		}
		[Test]
		public void CancelChild_AlreadyCompleted ()
		{
			var child = GetMsgsAct ("child execute", "child cancel");
			ActivityInstance ai = null;
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, compAI, callback) => {
				context.CancelChild (compAI);
				Console.WriteLine ("cancel");
				ai = compAI;
			});
			RunAndCompare (wf, String.Format ("child execute{0}cancel{0}", Environment.NewLine));
			Assert.AreEqual (ActivityInstanceState.Closed, ai.State);
		}
		[Test]
		public void CancelChild_Twice ()
		{
			var child = GetMsgsAct ("child:E", "child:C");
			ActivityInstance ai = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				ai = context.ScheduleActivity (child);
				context.CancelChild (ai);
				context.CancelChild (ai);
				Console.WriteLine ("wf execute");
			});
			RunAndCompare (wf, String.Format ("wf execute{0}", Environment.NewLine));
			Assert.AreEqual (ActivityInstanceState.Canceled, ai.State);
		}
		[Test]
		public void CancelChild_AlreadyFaulted ()
		{
			var child = new HelloWorldEx ();
			ActivityInstance ai = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, compAI) => {
				context.CancelChild (compAI);
				context.HandleFault ();
				Console.WriteLine ("fault");
				ai = compAI;
			});
			RunAndCompare (wf, String.Format ("fault{0}", Environment.NewLine));
			Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
		}
		[Test]
		public void CancelChild_Multiple ()
		{
			var child5 = GetWriteLine ("child5");
			var child4 = GetWriteLine ("child4");
			var child3 = GetBlocksWithMsgsAct ("child3:E", "child3:C");
			var child2 = GetBlocksWithMsgsAct ("child2:E", "child2:C");
			var child1 = GetBlocksWithMsgsAct ("child1:E", "child1:C");


			ActivityInstance ai1 = null, ai2 = null, ai3 = null;
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
				metadata.AddChild (child3);
				metadata.AddChild (child4);
				metadata.AddChild (child5);
			}, (context, callback) => {
				context.ScheduleActivity (child5);
				context.ScheduleActivity (child4, callback);
				ai3 = context.ScheduleActivity (child3);
				ai2 = context.ScheduleActivity (child2);
				ai1 = context.ScheduleActivity (child1);
			}, (context, compAI, callback) => {
				Console.WriteLine ("comp cb");
				context.CancelChild (ai3);
				context.CancelChild (ai1);
				context.CancelChild (ai2);

			});
			RunAndCompare (wf, String.Format ("child1:E{0}" +
			                                  "child2:E{0}" +
			                                  "child3:E{0}" +
			                                  "child4{0}" +
			                                  "comp cb{0}" +
			                                  "child2:C{0}" +
			                                  "child1:C{0}" +
			                                  "child3:C{0}" +
			                                  "child5{0}", Environment.NewLine));
		}
		[Test]
		public void CancelChildren_BeforeExecution ()
		{
			var child1 = GetMsgsAct ("child1 execute", "child1 cancel");
			var child2 = GetMsgsAct ("child2 execute", "child2 cancel");
			var state1 = (ActivityInstanceState)(-1);
			ActivityInstance ai1 = null;
			var state2 = (ActivityInstanceState)(-1);
			ActivityInstance ai2 = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, (context) => {
				ai2 = context.ScheduleActivity (child2);
				ai1 = context.ScheduleActivity (child1);
				context.CancelChildren ();
				state2 = ai2.State;
				state1 = ai1.State;
				Console.WriteLine ("wf execute");
			});
			RunAndCompare (wf, "wf execute" + Environment.NewLine);
			Assert.AreEqual (ActivityInstanceState.Executing, state1);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai1.State);
			Assert.AreEqual (ActivityInstanceState.Executing, state2);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai2.State);
		}
		[Test]
		public void CancelChildren_DuringExecution ()
		{
			var child4 = GetWriteLine ("child4");
			var child3 = GetBlocksWithMsgsAct ("child3:E", "child3:C");
			var child2 = GetBlocksWithMsgsAct ("child2:E", "child2:C");
			var child1 = GetBlocksWithMsgsAct ("child1:E", "child1:C");

			ActivityInstance ai1 = null, ai2 = null, ai3 = null;

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
				metadata.AddChild (child3);
				metadata.AddChild (child4);
			}, (context, callback) => {
				context.ScheduleActivity (child4, callback);
				ai3 = context.ScheduleActivity (child3);
				ai2 = context.ScheduleActivity (child2);
				ai1 = context.ScheduleActivity (child1);
			}, (context, compAI, callback) => {
				context.CancelChildren ();
				Console.WriteLine ("comp cb");
			});

			RunAndCompare (wf, String.Format ("child1:E{0}" +
			                                  "child2:E{0}" +
			                                  "child3:E{0}" +
			                                  "child4{0}" +
			                                  "comp cb{0}" +
			                                  "child1:C{0}" +
			                                  "child2:C{0}" +
			                                  "child3:C{0}", Environment.NewLine));

			Assert.AreEqual (ActivityInstanceState.Canceled, ai1.State);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai2.State);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai3.State);
		}
		//FIXME: move test
		[Test]
		public void NativeActivityContext_GetChildren ()
		{
			var child3 = GetWriteLine ("child3");
			var child2 = GetBlocksWithMsgsAct ("child2:E", "child2:C");
			var child1 = GetBlocksWithMsgsAct ("child1:E", "child1:C");

			ActivityInstance ai1 = null, ai2 = null, ai3 = null;

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
				metadata.AddChild (child3);
			}, (context, callback) => {
				ai3 = context.ScheduleActivity (child3, callback);
				ai2 = context.ScheduleActivity (child2);
				ai1 = context.ScheduleActivity (child1);
				var ais = context.GetChildren ();
				Assert.AreEqual (3, ais.Count);
				Assert.AreEqual (0, ais.IndexOf (ai3));
				Assert.AreEqual (1, ais.IndexOf (ai2));
				Assert.AreEqual (2, ais.IndexOf (ai1));
			}, (context, compAI, callback) => {
				var ais = context.GetChildren ();
				Assert.AreEqual (2, ais.Count);
				Assert.AreEqual (0, ais.IndexOf (ai2));
				Assert.AreEqual (1, ais.IndexOf (ai1));
				context.CancelChildren ();
				ais = context.GetChildren ();
				Assert.AreEqual (2, ais.Count);
				Assert.AreEqual (0, ais.IndexOf (ai2));
				Assert.AreEqual (1, ais.IndexOf (ai1));
			});

			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void Activity_ChildMarkedCancelled_State_Cancelled ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (true);
			var wf = new ActivityRunner (() => child1);
			var app = new WFAppWrapper (wf);
			app.Run ();
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
		}
		[Test]
		public void Activity_ChildNotMarkedCancelled_State_Completed ()
		{
			var child1 = GetBlocksOnCancelRemovesBkAct (false);
			var wf = new ActivityRunner (() => child1);
			var app = new WFAppWrapper (wf);
			app.Run ();
			app.Cancel ();
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
		}
		[Test]
		public void CancelMethod_Base_ClearsBookmarksAndChildren ()
		{
			var child = GetBlocksWithMsgsAct ("child execute", "child cancel");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				Console.WriteLine ("wf execute");
				context.CreateBookmark ();
				context.ScheduleActivity (child);
			}, "wf cancel");
			wf.InduceIdle = true;

			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.Cancel ();
			Assert.AreEqual (String.Format ("wf execute{0}" +
			                                "child execute{0}" +
			                                "wf cancel{0}" +
			                                "child cancel{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancellationRequested_Fault_State_AlreadySetToFaultedWhenCancelCalled ()
		{
			var child = GetSleepAndThrowAct (2, "child execute");
			ActivityInstance ai = null;
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				ai = context.ScheduleActivity (child, callback);
			}, (context, ex, compAI) => {
				Console.WriteLine ("handle fault");
				context.HandleFault ();
			}, (context) => {
				Console.WriteLine ("wf cancel");
				Console.WriteLine (ai.State);
			});
			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (String.Format ("child execute{0}" +
			                                "wf cancel{0}" +
							"Faulted{0}" +
			                                "handle fault{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancellationRequested_Fault_SubTreeAlreadyTerminatedWhenCancelCalled ()
		{

			ActivityInstance ai = null;
			var child1_1 = new NativeActivityRunner (null, (context) => {
				context.ResumeBookmark (new Bookmark ("bk"), null);
				context.CreateBookmark ();
			}, "child1_1 cancel");
			child1_1.InduceIdle = true;
			var child1 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context, callback) => {
				context.CreateBookmark ("bk", callback);
				ai = context.ScheduleActivity (child1_1);
			}, (context, bk, value, callback) => {
				Thread.Sleep (TimeSpan.FromSeconds (2));
				Console.WriteLine ("bookmark");
				throw new Exception ();
			}, "child cancel");

			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.ScheduleActivity (child1, callback);
			}, (context, ex, compAI) => {
				Console.WriteLine ("handle fault");
				context.HandleFault ();
			}, (context) => {
				Console.WriteLine ("wf cancel");
				Console.WriteLine (ai.State);
			});
			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (String.Format ("bookmark{0}" +
			                                "wf cancel{0}" +
			                                "Faulted{0}" +
			                                "handle fault{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CancellationRequested_Fault_FurtherExceptionInCancelCausesAbort ()
		{
			//
			Exception exThrow = new Exception ();
			var sleepThrow = GetSleepAndThrowAct (2, "sleeper");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (sleepThrow);
			}, (context) => {
				context.ScheduleActivity (sleepThrow, (ctx, ex, ai) => {
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				});
			}, (context) => {
				context.MarkCanceled ();
				Console.WriteLine ("cancel");
				throw exThrow;
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child, (ctx, ex, ai) => {
					Console.WriteLine ("fault handled");
					ctx.HandleFault ();
				});
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);
			Assert.AreEqual (WFAppStatus.Aborted, app.Status);

			Assert.AreEqual (String.Format ("sleeper{0}cancel{0}",
			                                Environment.NewLine), app.ConsoleOut);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), app.AbortReason);
			Assert.AreSame (exThrow, app.AbortReason.InnerException);
		}
	}
}

