using System;
using NUnit.Framework;
using System.Activities.Hosting;
using System.Runtime.DurableInstancing;
using System.Activities;
using System.Collections.Generic;
using System.Activities.Statements;
using System.IO;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Collections.ObjectModel;
using System.Linq;

namespace Tests.System.Activities {
	[TestFixture]
	public class WorkflowInstanceTest {
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
			new public void Initialize (IDictionary<string, object> workflowArgumentValues, IList<Handle> workflowExecutionProperties)
			{
				base.Initialize (workflowArgumentValues, workflowExecutionProperties);
			}
			new public void RegisterExtensionManager (WorkflowInstanceExtensionManager extensionManager)
			{
				base.RegisterExtensionManager (extensionManager);
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
			public ActivityInstanceState Controller_GetCompletionState (out IDictionary<string, object> outputs, out Exception terminationException)
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
			public BookmarkResumptionResult Controller_ScheduleBookmarkResumption (Bookmark bookmark, object value, BookmarkScope scope)
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
			protected override IAsyncResult OnBeginResumeBookmark (Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
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
			protected override IAsyncResult OnBeginAssociateKeys (ICollection<InstanceKey> keys, AsyncCallback callback, object state)
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
			protected override void OnNotifyUnhandledException (Exception exception, Activity source, string sourceInstanceId)
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
		static WorkflowInstanceHost GetHostToComplete (Activity wf)
		{
			var host = new WorkflowInstanceHost (wf);
			host.NotifyPaused = () =>  {
				if (host.Controller_State == WorkflowInstanceState.Complete)
					host.AutoResetEvent.Set ();
			};
			return host;
		}
		static WorkflowInstanceHost GetHostToIdle (Activity wf)
		{
			var host = new WorkflowInstanceHost (wf);
			host.NotifyPaused = () =>  {
				if (host.Controller_State == WorkflowInstanceState.Idle)
					host.AutoResetEvent.Set ();
			};
			return host;
		}
		static WorkflowInstanceHost GetHostToIdleOrComplete (Activity wf)
		{
			var host = new WorkflowInstanceHost (wf);
			host.NotifyPaused = () =>  {
				var state = host.Controller_State;
				if (state == WorkflowInstanceState.Complete || state == WorkflowInstanceState.Idle)
					host.AutoResetEvent.Set ();
			};
			return host;
		}
		static WorkflowInstanceHost GetHostToHandleException (Activity wf)
		{
			var host = new WorkflowInstanceHost (wf);
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.AutoResetEvent.Set ();
			};
			return host;
		}
		static void InitRunWait (WorkflowInstanceHost host)
		{
			host.Initialize (null, null);
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
		}
		static void InitRunWait (WorkflowInstanceHost host, IDictionary<string, object> inputs)
		{
			host.Initialize (inputs, null);
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
		}
		static BookmarkResumptionResult InitRunWaitScheduleResumeBookmark (WorkflowInstanceHost host, ref Bookmark bookmark, object value)
		{
			// bookmark param can be set inside a delegate which isnt executed until its passed to InitRunWait, thus need that ref, not copy of ref
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			return host.Controller_ScheduleBookmarkResumption (bookmark, value);
		}
		static void RunAgain (WorkflowInstanceHost host)
		{
			host.AutoResetEvent.Reset ();
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
		}
		//FIXME: add null checks for methods etc
		[Test, ExpectedException (typeof (ArgumentNullException))] 
		public void Ctor_NullEx ()
		{
			new WorkflowInstanceHost (null);
		}
		[Test]
		public void Ctor ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };
			var host = new WorkflowInstanceHost (writeLine);
			Assert.IsNull (host.HostEnvironment);
			Assert.IsNull (host.SynchronizationContext);
			Assert.AreSame (writeLine, host.WorkflowDefinition);
			//FIXME: Assert.IsFalse (mock.IsReadOnly);
		}
		[Test]
		public void Initialize ()
		{
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.Initialize (null, null);
			Assert.IsNull (host.HostEnvironment);
			Assert.IsNull (host.SynchronizationContext);
			//FIXME: Assert.IsTrue (host.IsReadOnly);
			Assert.AreEqual ("System.Activities.Hosting.WorkflowInstance+WorkflowInstanceControl", 
			                 host.Controller_ToString ());
		}
		//IsReadOnly tested in Initialise / Ctor
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Controller_WINotIntializedEx ()
		{
			//System.InvalidOperationException : WorkflowInstance.Controller is only valid after Initialize has been called.
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.Controller_ToString ();
		}
		[Test]
		public void ExecuteActivities ()
		{

			var wf = new Sequence { Activities = { 
					new CodeActivityRunner (null, (context) => {
						Thread.Sleep (500); // prove it waits for thread
					}),
					new WriteLine { Text = "1" },
					new WriteLine { Text = "2" },
					new WriteLine { Text = "3" },
				}
			};
			//FIXME: test ActivityInstanceState.Cancelled and Faulted
			//FIXME: test WorkflowInstanceState.Aborted
			var host = GetHostToComplete (wf);
			host.Initialize (null, null);
			host.Controller_Run ();
			Assert.IsNull (host.SynchronizationContext); // this seems never to be set
			host.AutoResetEvent.WaitOne ();
			Assert.AreEqual ("1" + Environment.NewLine 
			                 + "2" + Environment.NewLine 
			                 + "3" + Environment.NewLine, host.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Closed, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		[Test]
		public void PassArgsIn ()
		{
			var host = GetHostToComplete (new WriteLine ());
			InitRunWait (host, new Dictionary<string, object> () { {"Text", "Hello\nWorld"} });
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, host.ConsoleOut);
		}
		//FIXME: the same testing that is done for WorkflowInvoker.Invoke(Activity. IDictionary) could be applicable here
		[Test, ExpectedException (typeof (ArgumentException))]
		public void PassArgsIn_Invalid ()
		{
			/* System.ArgumentException : The values provided for the root activity's arguments did not satisfy the root activity's requirements:
			 * 'WriteLine': The following keys from the input dictionary do not map to arguments and must be removed: Text2.  
			 * Please note that argument names are case sensitive.
			 * Parameter name: rootArgumentValues
			 */
			var wf = new WriteLine ();
			var host = new WorkflowInstanceHost (wf);
			host.Initialize (new Dictionary<string, object> () { {"Text", "Hello\nWorld"},
									     {"Text2", "Hello\nWorld"} }, null);
		}
		[Test]
		public void Controller_State_AfterInitialized ()
		{
			var host = GetHostToComplete (new WriteLine ());
			host.Initialize (null, null);
			var state = host.Controller_GetCompletionState ();
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
		}
		[Test]
		public void Controller_WFCompletedNormallyWith1OutArg ()
		{
			/*Test Controller.GetCompletionState (out IDictionary, out Exception) 
			 * 	IDictionary contains output param, Exception null
			 * 	return value is ActivityInstanceState.Completed
			 *Test Controller.State returns WorkflowInstanceState.Aborted
			 */
			var wf = new Concat { String1 = "Hello\n", String2 = "World" };
			IDictionary<string, object> outputs;
			Exception exception;
			var host = GetHostToComplete (wf);
			InitRunWait (host);
			var state = host.Controller_GetCompletionState (out outputs, out exception);
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.IsNull (exception);
			Assert.AreEqual (1, outputs.Count);
			Assert.AreEqual ("Hello\nWorld", outputs ["Result"]);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		[Test]
		public void Controller_WFCompletedNormallyWithNoOutArgs ()
		{
			//as Controller_WFCompletedNormallyWith1OutArg without Arg
			IDictionary<string, object> outputs;
			Exception exception;
			var host = GetHostToComplete (new WriteLine ());
			InitRunWait (host);
			var state = host.Controller_GetCompletionState (out outputs, out exception);
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.IsNull (exception);
			Assert.IsNull (outputs);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		[Test]
		public void Controller_WFUnhandledExceptionWith1OutArg ()
		{
			/*Test Controller.GetCompletionState (out IDictionary, out Exception) 
			 * 	bothOut args null
			 * 	return value is ActivityInstanceState.Executing
			 *Test Controller.State returns WorkflowInstanceState.Idle
			 */
			IDictionary<string, object> outputs;
			Exception returnedEx;
			var wf = new Sequence { Activities = { new HelloWorldEx () } };
			var host = GetHostToHandleException (wf);
			InitRunWait (host);
			var state = host.Controller_GetCompletionState (out outputs, out returnedEx);
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.IsNull (returnedEx);
			Assert.IsNull (outputs);
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
		}
		[Test]
		public void Controller_State_WhenUnhandledExceptionHit ()
		{
			//seems to go to Idle if root Activity raises exception, but Runnable if child does
			var root = GetHostToHandleException (new HelloWorldEx ());
			InitRunWait (root);
			var stateRootEx = root.Controller_State;
			Assert.AreEqual (WorkflowInstanceState.Idle, stateRootEx);

			var pubChild = GetHostToHandleException (new Sequence { Activities = { new HelloWorldEx () }});
			InitRunWait (pubChild);
			var statePubChild = pubChild.Controller_State;
			Assert.AreEqual (WorkflowInstanceState.Runnable, statePubChild);

			var helloWorldEx = new HelloWorldEx ();
			var hasImpWF = new NativeActivityRunner ((metadata)=> {
				metadata.AddImplementationChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (helloWorldEx);
			});
			var impChild = GetHostToHandleException (hasImpWF);
			InitRunWait (impChild);
			var stateImpChild = impChild.Controller_State;
			Assert.AreEqual (WorkflowInstanceState.Runnable, stateImpChild);
		}
		[Test]
		public void Controller_WFTerminatedWithExWith1OutArg ()
		{
			/*Test Controller.GetCompletionState (out IDictionary, out Exception) 
			 * 	IDictionary null, Exception matches that passed to terminate
			 * 	return value is ActivityInstanceState.Faulted
			 *Test Controller.Terminate (Exception) stops workflow when paused
			 *	calling Controller.State returns WorkflowInstanceState.Complete
			 */
			var ex = new InvalidOperationException ("Hello\nException");
			var wf = new HelloWorldEx ();

			IDictionary<string, object> outputs;
			Exception returnedEx;
			var host = new WorkflowInstanceHost (wf);
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.Controller_Terminate (ex);
				host.AutoResetEvent.Set ();
			};
			InitRunWait (host);
			var state = host.Controller_GetCompletionState (out outputs, out returnedEx);
			Assert.AreEqual (ActivityInstanceState.Faulted, state);
			Assert.AreSame (ex, returnedEx);
			Assert.IsNull (outputs);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		[Test]
		public void Controller_WFTerminatedWithNullExWith1OutArg ()
		{
			//delegate runs on a different thread so not using ExpectedException
			var host = new WorkflowInstanceHost (new HelloWorldEx ());
			IDictionary<string, object> outputs;
			Exception returnedEx;
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.Controller_Terminate (null);
				host.AutoResetEvent.Set ();
			};
			InitRunWait (host);
			var state = host.Controller_GetCompletionState (out outputs, out returnedEx);
			Assert.AreEqual (ActivityInstanceState.Faulted, state);
			Assert.IsNull (returnedEx);
			Assert.IsNull (outputs);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		//FIXME: test when OnRequestAbort (Exception) is called, doesnt seem to be in response to Controller.Abort (..)
		[Test]
		public void Controller_WFAbortedWithExWith1OutArg ()
		{
			/*Test Controller.GetCompletionState (out IDictionary, out Exception) 
			 * 	out args both null, (root has out arg)
			 * 	return value is ActivityInstanceState.Executing
			 *Test Controller.Abort (Exception) aborts workflow when paused
			 *	calling Controller.State returns WorkflowInstanceState.Aborted
			 *Test Controller.GetAbortReason () returns exception passed to abort
			 */

			var ex = new InvalidOperationException ("Hello\nException");
			var wf = new HelloWorldEx ();

			IDictionary<string, object> outputs;
			Exception returnedEx;
			var host = new WorkflowInstanceHost (wf);
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.Controller_Abort (ex);
				host.AutoResetEvent.Set ();
			};
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Aborted, host.Controller_State);
			Assert.AreSame (ex, host.Controller_GetAbortReason ());

			var state = host.Controller_GetCompletionState (out outputs, out returnedEx);
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.IsNull (returnedEx);
			Assert.IsNull (outputs);
		}
		[Test]
		public void Controller_WFAbortedNoExWith1OutArg ()
		{
			// as Controller_Aborting_WithException but without exception
			var wf = new HelloWorldEx ();

			IDictionary<string, object> outputs;
			Exception returnedEx;
			var host = new WorkflowInstanceHost (wf);
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.Controller_Abort ();
				host.AutoResetEvent.Set ();
			};
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Aborted, host.Controller_State);
			Assert.IsNull (host.Controller_GetAbortReason ());

			var state = host.Controller_GetCompletionState (out outputs, out returnedEx);
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.IsNull (returnedEx);
			Assert.IsNull (outputs);
		}
		[Test]
		public void Controller_WFAbortedWithNullExOk ()
		{
			var host = new WorkflowInstanceHost (new HelloWorldEx ());
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.Controller_Abort (null);
				host.AutoResetEvent.Set ();
			};
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Aborted, host.Controller_State);
			Assert.IsNull (host.Controller_GetAbortReason ());
		}
		[Test]
		public void Controller_GetAbortReason_DidntAbort ()
		{
			var host = GetHostToComplete (new WriteLine ());
			InitRunWait (host);
			Assert.IsNull (host.Controller_GetAbortReason ());
		}
		[Test]
		[Ignore ("ScheduleCancel")]
		public void Controller_WFScheduledCanceledWith1OutArg ()
		{
			var wf = new HelloWorldEx ();

			IDictionary<string, object> outputs;
			Exception returnedEx;
			var host = new WorkflowInstanceHost (wf);
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) =>  {
				host.Controller_ScheduleCancel ();
				host.AutoResetEvent.Set ();
			};
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.IsNull (host.Controller_GetAbortReason ());

			var state = host.Controller_GetCompletionState (out outputs, out returnedEx);
			Assert.AreEqual (ActivityInstanceState.Canceled, state);
			Assert.IsNull (returnedEx);
			Assert.IsNull (outputs);
		}
		[Test]
		public void OnNotifyUnhandledException ()
		{
			Exception returnedEx = null;
			Activity returnedSource = null;
			string returnedInstanceId = null;

			var exActivity = new HelloWorldEx ();
			var wf = new Sequence { Activities = { exActivity } };

			var host = new WorkflowInstanceHost (wf);
			host.NotifyUnhandledException = (exception, source, sourceInstanceId) => {
				returnedEx = exception;
				returnedSource = source;
				returnedInstanceId = sourceInstanceId;
				host.AutoResetEvent.Set ();
			};
			InitRunWait (host);
			Assert.AreEqual (exActivity.IThrow, returnedEx);
			Assert.AreEqual (exActivity, returnedSource);
			Assert.AreEqual ("2", returnedInstanceId);

			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
		}
		[Test]
		[Ignore ("Extension and Bookmarks with Delay Activity")]
		public void ExtensionsAndBookmarks ()
		{
			var wf = new Sequence { Activities = { 
					new Delay { Duration = new InArgument<TimeSpan> (TimeSpan.FromSeconds (2))},
					new WriteLine { Text = "Hello\nWorld" }
				}
			};

			var host = GetHostToComplete (wf);
			host.BeginResumeBookmark = (bookmark, value, timeout, callback, state) => {
				var del = new Func<BookmarkResumptionResult> (() => host.Controller_ScheduleBookmarkResumption (bookmark, value));
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			host.EndResumeBookmark = (result) => {
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				result.AsyncWaitHandle.Close ();
				host.Controller_Run ();
				return retValue;
			};
			var extman = new WorkflowInstanceExtensionManager ();
			host.RegisterExtensionManager (extman); // needs to be called before Initialize

			host.Initialize (null, null);
			host.Controller_Run ();
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
			Thread.Sleep (500);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (String.Empty, host.ConsoleOut);
			host.AutoResetEvent.WaitOne ();
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, host.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Closed, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		static BookmarkCallback writeValueBookCB = (ctx, book, value) => {
			Console.WriteLine ((string) value);
		};
		static void CheckBookmarkWithScopeCanBeResumedAndThenCompletes (Activity wf, ref Bookmark bookmark, ref BookmarkScope scope)
		{
			//bookmark and scope will be set when delegate executes thus needs refs
			var host = GetHostToIdleOrComplete (wf);
			var result1 = InitRunWaitScheduleResumeBookmark (host, ref bookmark, "resumed");
			//cant resume by SchdeduleBookmarkResumtpion (bookmark, value) overload
			Assert.AreEqual (BookmarkResumptionResult.NotFound, result1);
			var result2 = host.Controller_ScheduleBookmarkResumption (bookmark, "resumed", scope);
			Assert.AreEqual (BookmarkResumptionResult.Success, result2);
			RunAgain (host);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.AreEqual (ActivityInstanceState.Closed, host.Controller_GetCompletionState ());
			Assert.AreEqual ("resumed" + Environment.NewLine, host.ConsoleOut);
		}
		[Test]
		public void BookmarkScope_ContextDefault_ResumeFromWorkflowInstance ()
		{
			BookmarkScope scope = null;
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				scope = context.DefaultBookmarkScope;
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, scope);
			});
			wf.InduceIdle = true;
			CheckBookmarkWithScopeCanBeResumedAndThenCompletes (wf, ref bookmark, ref scope);
		}
		[Test]
		public void BookmarkScope_BookmarkScopeDefault_ResumeFromWorkflowInstance ()
		{
			BookmarkScope scope = null;
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, context =>  {
				scope = BookmarkScope.Default;
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, scope);
			});
			wf.InduceIdle = true;
			CheckBookmarkWithScopeCanBeResumedAndThenCompletes (wf, ref bookmark, ref scope);
		}
		[Test]
		public void Controller_GetBookmarks_IncludesThoseWithBookmarkScope ()
		{
			BookmarkScope scope = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1");
				scope = BookmarkScope.Default;
				context.CreateBookmark ("b1", writeValueBookCB, scope);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdle (wf);
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			Assert.AreEqual (2, host.Controller_GetBookmarks ().Count);
			Assert.AreEqual (1, host.Controller_GetBookmarks (scope).Count);
		}
		[Test]
		public void Controller_GetBookmarks_BookmarkScope ()
		{
			BookmarkScope bookmarkDefault = null, contextDefault = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1");
				bookmarkDefault = BookmarkScope.Default;
				context.CreateBookmark ("b2", writeValueBookCB, bookmarkDefault);
				contextDefault = context.DefaultBookmarkScope;
				context.CreateBookmark ("b3", writeValueBookCB, contextDefault);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdle (wf);
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			//these return the same bookmarks, but doesnt seem to be equal,
			// see Bookmark..HandlingTest.BookmarkDefaultAndContextDefaultBookmarkNotSame
			Assert.AreEqual (2, host.Controller_GetBookmarks (bookmarkDefault).Count);
			Assert.AreEqual (2, host.Controller_GetBookmarks (contextDefault).Count);
			var bm = new BookmarkScope (Guid.NewGuid ());
			Assert.AreEqual (0, host.Controller_GetBookmarks (bm).Count);
			//FIXME: check b2 and b3 BookmarkInfo prop values
		}
		[Test, ExpectedException (typeof (NullReferenceException))]
		public void Controller_GetBookmarks_BookmarkScope_NullEx ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB, BookmarkScope.Default);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdle (wf);
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			host.Controller_GetBookmarks (null);
		}
		[Test]
		public void Controller_GetBookmarks ()
		{
			//havnt tested different BookmarkOptions
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b2");
			});
			child.DisplayName = "child";
			child.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.CreateBookmark ("b1");
				context.ScheduleActivity (child);
			});
			wf.DisplayName = "wf";
			wf.InduceIdle = true;
			var host = GetHostToIdle (wf);
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			Assert.AreEqual (2, host.Controller_GetBookmarks ().Count);
			var b1 = host.Controller_GetBookmarks ().Single (b => b.BookmarkName == "b1");
			Assert.AreEqual (wf.DisplayName, b1.OwnerDisplayName);
			Assert.IsNull (b1.ScopeInfo);
			var b2 = host.Controller_GetBookmarks ().Single (b => b.BookmarkName == "b2");
			Assert.AreEqual (child.DisplayName, b2.OwnerDisplayName);
			Assert.IsNull (b2.ScopeInfo);
		}
		[Test]
		public void Controller_GetBookmarks_IgnoresThoseWithoutName ()
		{
			var wf = new NativeActivityRunner (null , (context) => {
				context.CreateBookmark ();
			});
			wf.InduceIdle = true;
			var host = GetHostToIdle (wf);
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			Assert.AreEqual (0, host.Controller_GetBookmarks ().Count);
		}
		[Test]
		public void Controller_ScheduleBookmarkResumption_Bookmark_Value ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark (writeValueBookCB);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdleOrComplete (wf);
			var result = InitRunWaitScheduleResumeBookmark (host, ref bookmark, "resumed");
			Assert.AreEqual (BookmarkResumptionResult.Success, result);
			RunAgain (host);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.AreEqual ("resumed" + Environment.NewLine, host.ConsoleOut);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Controller_ScheduleBookmarkResumption_Bookmark_Value_NullEx ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdle (wf);
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			host.Controller_ScheduleBookmarkResumption (null, "resumed");
		}
		[Test]
		public void Controller_ScheduleBookmarkResumption_Bookmark_Value_NotFound ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdle (wf);
			var bm = new Bookmark ("bob");
			var result = InitRunWaitScheduleResumeBookmark (host, ref bm, "resumed");
			Assert.AreEqual (BookmarkResumptionResult.NotFound, result);
		}
		[Test]
		public void Controller_ScheduleBookmarkResumption_Bookmark_Value_DifferentInstanceSameNameResumesOK ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdleOrComplete (wf);
			var bm = new Bookmark ("b1");
			var result = InitRunWaitScheduleResumeBookmark (host, ref bm, "resumed");
			Assert.AreEqual (BookmarkResumptionResult.Success, result);
			RunAgain (host);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.AreEqual ("resumed" + Environment.NewLine, host.ConsoleOut);
		}
		[Test]
		public void OnBeginResumeBookmark_NotCalledOnContext_ResumeBookmark ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				var bookmark = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
				context.ResumeBookmark (bookmark, "Hello\nWorld");
			});
			wf.InduceIdle = true;
			var host = GetHostToComplete (wf);
			bool wasCalled = false;
			host.BeginResumeBookmark = (bookmark, value, timeout, callback, state) => {
				wasCalled = true;
				var del = new Func<BookmarkResumptionResult> (() => host.Controller_ScheduleBookmarkResumption (bookmark, value));
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			host.EndResumeBookmark = (result) => {
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				result.AsyncWaitHandle.Close ();
				host.Controller_Run ();
				return retValue;
			};
			InitRunWait (host);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.IsFalse (wasCalled);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, host.ConsoleOut);
		}
		[Test]
		public void OnBeginResumeBookmark_NotCalledOnScheduleResumeBookmark ()
		{
			Bookmark bm = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bm = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdleOrComplete (wf);
			bool wasCalled = false;
			host.BeginResumeBookmark = (bookmark, value, timeout, callback, state) => {
				wasCalled = true;
				var del = new Func<BookmarkResumptionResult> (() => host.Controller_ScheduleBookmarkResumption (bookmark, value));
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			host.EndResumeBookmark = (result) => {
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				result.AsyncWaitHandle.Close ();
				host.Controller_Run ();
				return retValue;
			};
			var bmResult = InitRunWaitScheduleResumeBookmark (host, ref bm, "resumed");
			Assert.AreEqual (BookmarkResumptionResult.Success, bmResult);
			RunAgain (host);
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.IsFalse (wasCalled);
			Assert.AreEqual ("resumed" + Environment.NewLine, host.ConsoleOut);
		}
	}
}

