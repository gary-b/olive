using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.Threading;
using System.IO;
using System.Linq;
using System.Activities.Hosting;

namespace Tests.System.Activities {
	[TestFixture]
	public class BookmarkHandlingRuntimeTest : WFTest {
		#region Basic bookmarking by means of OnCompletion callbacks when scheduling activities
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
		static BookmarkCallback writeValueBookCB = (ctx, book, value) => {
			Console.WriteLine ((string) value);
		};
		[Test]
		public void CompletionCallback_AnonymousDelegate ()
		{
			var writeLine = new WriteLine { Text = "WriteLine" };
			var wf = new NativeActivityRunner ((metadata)=> {
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine, (cbContext, completedInstance) => {
					Console.WriteLine ("Callback");
				});
			});
			RunAndCompare (wf, "WriteLine" + Environment.NewLine + "Callback" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void CompletionCallback_AnonymousDelegateAccessesContextEx ()
		{
			//System.ArgumentException : 'System.Activities.CompletionCallback' is not a valid activity execution callback. 
			//The execution callback used by '1: NativeActivityRunner' must be an instance method on '1: NativeActivityRunner'.
			//Parameter name: onCompleted
			var writeLine = new WriteLine { Text = "WriteLine" };
			var wf = new NativeActivityRunner ((metadata)=> {
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine, (cbContext, completedInstance) => {
					Console.WriteLine ("sdsadsa");
					cbContext.ScheduleActivity (writeLine);
				});
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void CompletionCallback_ScheduleActivityThrowsWhenAnonymousDelegateWhichAccessesContextPassed ()
		{
			// FIXME: I implemented this validation check to happen when context accessed, it seems to be 
			// done in the ScheduleActivity method however, as this test shows
			var vStr = new Variable<string> ();
			var writeLine = new WriteLine { Text = "WriteLine" };
			Exception ex = null;
			var wf = new NativeActivityRunner ((metadata)=> {
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				try {
					context.ScheduleActivity (writeLine, (cbContext, completedInstance) => {
						Console.WriteLine (cbContext.GetValue (vStr));
					});
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			try {
				WorkflowInvoker.Invoke (wf);
			} catch (Exception) {
			}
			Assert.IsNotNull (ex);
		}
		[Test]
		public void CompletionCallback_Simple ()
		{
			var writeLine = new WriteLine { Text = "W" };
			var wf = new NativeActWithCBRunner ((metadata)=> {
				metadata.AddImplementationChild (writeLine);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine, callback);
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("CompletionCallback");
			});
			RunAndCompare (wf, String.Format ("W{0}CompletionCallback{0}", Environment.NewLine));
		}
		[Test]
		public void CompletionCallback_VarAccessAndMultipleSchedules ()
		{
			var vStr = new Variable<string> ("","O");
			var writeLine = new WriteLine { Text = vStr };
			var appendToVar = new Assign { Value = new InArgument<string> (new Concat {String1 = vStr, String2 = "M"}),
				To = new OutArgument<string> (vStr)
			};
			var wf = new NativeActWithCBRunner ((metadata)=> {
				metadata.AddImplementationVariable (vStr);
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationChild (appendToVar);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine);
				context.ScheduleActivity (appendToVar);
				context.ScheduleActivity (writeLine, callback);
				context.ScheduleActivity (appendToVar);
				context.ScheduleActivity (writeLine);
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("CompletionCallback");
			});
			RunAndCompare (wf, String.Format ("O{0}OM{0}CompletionCallback{0}OMM{0}", Environment.NewLine));
		}
		[Test]
		public void CompletionCallback_VarAccessAndSchedulesOtherActivities ()
		{
			var vInt = new Variable<int> ("", 0);
			var writeLine = new WriteLine { Text = "W" };
			var wf = new NativeActWithCBRunner ((metadata)=> {
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationVariable (vInt);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine, callback);
			}, (context, completedInstance, callback) => {
				context.SetValue (vInt, context.GetValue (vInt) + 1);
				if (context.GetValue (vInt) < 4)
					context.ScheduleActivity (writeLine, callback);
			});
			RunAndCompare (wf, String.Format ("W{0}W{0}W{0}W{0}", Environment.NewLine));
		}
		[Test]
		public void CompletionCallbackT_VarAccessAndSchedulesOtherActivities ()
		{
			var vInt = new Variable<int> ("", 0);
			var concat = new Concat { String1 = "H", String2 = "W" };
			var wf = new NativeActWithCBRunner<string> ((metadata)=> {
				metadata.AddImplementationChild (concat);
				metadata.AddImplementationVariable (vInt);
			}, (context, callback) => {
				context.ScheduleActivity (concat, callback);
			}, (context, completedInstance, callback, result) => {
				Console.WriteLine (result);
				context.SetValue (vInt, context.GetValue (vInt) + 1);
				if (context.GetValue (vInt) < 4)
					context.ScheduleActivity (concat, callback);
			});
			RunAndCompare (wf, String.Format ("HW{0}HW{0}HW{0}HW{0}", Environment.NewLine));
		}
		#endregion
		static WorkflowApplication WFApp (Activity wf, out StringWriter sw, out AutoResetEvent reset)
		{
			reset = new AutoResetEvent (false);
			sw = new StringWriter ();
			Console.SetOut (sw);
			var app = new WorkflowApplication (wf);
			return app;
		}
		static string WFAppSyncRun (Activity wf, Action<WorkflowApplicationIdleEventArgs> idle, 
		                          Action<WorkflowApplicationCompletedEventArgs> completed)
		{
			AutoResetEvent reset;
			StringWriter sw;
			var app = WFApp (wf, out sw, out reset);
			app.Idle = (args) => {
				if (idle != null)
					idle (args);
				reset.Set ();
			};
			app.Completed = (args) => {
				if (completed != null)
					completed (args);
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			return sw.ToString ();
		}
		[Test]
		public void CanInduceIdle_False_CreateBookmarkThrowsException ()
		{
			//System.InvalidOperationException: Activity 'Tests.System.Activities.NativeActivityRunner' is invalid. NativeActivity 
			//derived activities that do asynchronous operations by calling one of the CreateBookmark overloads defined on 
			//System.Activities.NativeActivityContext must override the CanInduceIdle property and return true.
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				try {
					//doesnt matter if non blocking
					context.CreateBookmark ((context2, mark, value)=>{}, BookmarkOptions.NonBlocking);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = false;
			WFAppSyncRun (wf, null, null);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void CanInduceIdle_False_ChildCanHaveBookmark ()
		{
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			child.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
			wf.InduceIdle = false;
			AutoResetEvent reset;
			StringWriter sw;
			var app = WFApp (wf, out sw, out reset);
			app.Idle = (args) => {
				reset.Set ();
			};
			app.Completed = (args) => {
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			reset.Reset ();
			app.ResumeBookmark ("b1", "Hello\nWorld");
			reset.WaitOne ();
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, sw.ToString ());
		}
		[Test]
		public void BookmarkCallback_AnonymousMethod ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			wf.InduceIdle = true;

			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			app.Idle = args =>  {
				reset.Set ();
			};
			app.Completed = args =>  {
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			reset.Reset ();
			app.ResumeBookmark ("b1", "Hello\nWorld");
			reset.WaitOne ();
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, sw.ToString ());
		}
		[Test]
		public void BookmarkCallback_AnonymousMethod_CreateBookmarkThrowsWhenAnonymousDelegateWhichAccessesContextPassed ()
		{
			//System.Activities.BookmarkCallback' is not a valid activity execution callback. 
			//The execution callback used by '1: NativeActivityRunner' must be an instance method on 
			//'1: NativeActivityRunner'.
			Exception ex = null;
			var v1 = new Variable<string> ("","var");
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (v1);
			}, (context) => {
				try {
					context.CreateBookmark ("b1", (bmContext, bookmark, value) => {
						Console.WriteLine ((string) value + bmContext.GetValue (v1));
					});
				} catch (Exception ex1) {
					ex = ex1;
				}
			});
			wf.InduceIdle = true;

			var reset = new AutoResetEvent (false);
			var app = new WorkflowApplication (wf);
			app.Completed = (args) => {
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.IsInstanceOfType (typeof (ArgumentException), ex);
		}
		[Test]
		public void RemoveBookmark_CantRemoveParentsBookmark ()
		{
			//System.InvalidOperationException: Bookmarks can only be removed by the activity instance that created them.
			Bookmark bookmark = null;
			Exception ex = null;
			var remover = new NativeActivityRunner (null, (context) => {
				try {
					context.RemoveBookmark (bookmark);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (remover);
			}, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.MultipleResume);
				context.ScheduleActivity (remover);
			});
			wf.InduceIdle = true;
			WFAppSyncRun (wf, null, null);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void BookmarkResumption_HappensAfterScheduledChildren ()
		{
			Bookmark bookmark = null;
			var grandChild = new WriteLine { Text = "Activity" };
			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (grandChild);
			}, (context) => {
				context.ScheduleActivity (grandChild);
				context.ResumeBookmark (bookmark, "Bookmark");
				context.ScheduleActivity (grandChild);
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
				context.ScheduleActivity (child);
			});
			wf.InduceIdle = true;

			var result = WFAppSyncRun (wf, null, null);
			Assert.AreEqual (String.Format ("Activity{0}Activity{0}Bookmark{0}", Environment.NewLine), result);
		}
		[Test]
		public void BookmarkResumption_FromSameActivity ()
		{
			var wf = new NativeActivityRunner ((metadata) => {
			}, (context) => {
				var bookmark = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
				context.ResumeBookmark (bookmark, "Hello\nWorld");
			});
			wf.InduceIdle = true;

			var result = WFAppSyncRun (wf, null, null);
			Assert.AreEqual (String.Format ("Hello\nWorld{0}", Environment.NewLine), result);
		}
		[Test]
		public void RemoveBookmarkInSameMethodAsResumeBookmark ()
		{
			bool b1Removed = false, b2Removed = false;
			BookmarkResumptionResult b1Resumed = (BookmarkResumptionResult)(-1);
			BookmarkResumptionResult b2Resumed = (BookmarkResumptionResult)(-1);
			var wf = new NativeActivityRunner ((metadata) => {
			}, (context) => {
				//b1 is removed before attempted resumption
				var b1 = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
				b1Removed = context.RemoveBookmark (b1);
				b1Resumed = context.ResumeBookmark (b1, "b1");
				//b2 is resumed before attempted removal
				var b2 = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
				b2Resumed = context.ResumeBookmark (b2, "b2");
				b2Removed = context.RemoveBookmark (b2);
			});
			wf.InduceIdle = true;

			var result = WFAppSyncRun (wf, null, null);
			Assert.IsTrue (b1Removed);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, b1Resumed);
			
			Assert.AreEqual (BookmarkResumptionResult.Success, b2Resumed);
			Assert.IsFalse (b2Removed);
			Assert.AreEqual ("b2" + Environment.NewLine, result);
		}
		[Test]
		public void ResumeBookmark_NotYetCreated ()
		{
			// this was a test to check if BookmarkResumptionResult.NotReady would be returned
			// twas a long shot, but leaves the question, what does NotReady mean?... considering 
			// ResumeBookmark blocks until the workflow is ready to resume a found bookmark
			var bookmarker1 = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			bookmarker1.InduceIdle = true;
			var bookmarker2 = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b2", writeValueBookCB);
			});
			bookmarker2.InduceIdle = true;
			var wf = new Sequence {
				Activities = {
					bookmarker1,
					bookmarker2
				}
			};
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			bool idle = false;
			app.Idle = (args) => {
				idle = true;
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.IsTrue (idle);
			var resumeResult = app.ResumeBookmark ("b2", "b2");
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumeResult);

		}
		[Test]
		public void BookmarkScope_Default ()
		{
			BookmarkScope defaultScope = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
				defaultScope = context.DefaultBookmarkScope;
			});
			wf.InduceIdle = true;
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			app.Run ();
			app.Idle = (args) => {
				reset.Set ();
			};
			reset.WaitOne ();
			BookmarkInfo info = app.GetBookmarks ().Single ();
			//
			//Assert.IsTrue (info.ScopeInfo.IsInitialized);
			Assert.IsNotNull (defaultScope);
			Assert.IsFalse (defaultScope.IsInitialized);
			Assert.IsNull (info.ScopeInfo);
		}
		static void ResumeBookmarkFromChild_CheckRemovedImplicitly (BookmarkOptions bookmarkOptions)
		{
			Bookmark bookmark = null;
			BookmarkResumptionResult resumeResult = (BookmarkResumptionResult)(-1);
			var resumer = new NativeActivityRunner (null, context =>  {
				resumeResult = context.ResumeBookmark (bookmark, "resumed");
			});
			var wf = new NativeActivityRunner (metadata =>  {
				metadata.AddChild (resumer);
			}, context =>  {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, bookmarkOptions);
				context.ScheduleActivity (resumer);
				context.ScheduleActivity (resumer);
			});
			wf.InduceIdle = true;
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			app.Completed = args =>  {
				state = args.CompletionState;
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.AreEqual ("resumed" + Environment.NewLine, sw.ToString ());
			// check result of 2nd call to ResumeBookmark
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumeResult);
		}
		static void RunWFWithBookmarkCheckingItDoesntBlock (BookmarkOptions bookmarkOptions)
		{
			var wf = new NativeActivityRunner (null, context =>  {
				context.CreateBookmark ("b1", writeValueBookCB, bookmarkOptions);
			});
			wf.InduceIdle = true;
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			app.Completed = args =>  {
				state = args.CompletionState;
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.AreEqual (String.Empty, sw.ToString ());
		}
		static void ResumeBookmarkFromChildMultipleTimes_RemoveExplicitly (BookmarkOptions bookmarkOptions)
		{
			Bookmark bookmark = null;
			BookmarkResumptionResult resumeResult = (BookmarkResumptionResult)(-1);
			var resumer = new NativeActivityRunner (null, context =>  {
				resumeResult = context.ResumeBookmark (bookmark, "resumed");
			});
			var remover = new NativeActivityRunner (null, context =>  {
				context.RemoveBookmark (bookmark);
			});
			var wf = new NativeActWithCBRunner (metadata =>  {
				metadata.AddChild (resumer);
				metadata.AddChild (remover);
			}, (context, callback) =>  {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, bookmarkOptions);
				context.ScheduleActivity (resumer);
				context.ScheduleActivity (resumer, callback);
				// runs second, callback then removes bookmark
				context.ScheduleActivity (resumer);
				// runs first
			}, (context, completedInstance, callback) =>  {
				context.RemoveBookmark (bookmark);
			});
			wf.InduceIdle = true;
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			app.Completed = args =>  {
				state = args.CompletionState;
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.AreEqual (String.Format ("resumed{0}resumed{0}", Environment.NewLine), sw.ToString ());
			// check result of last call to ResumeBookmark
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumeResult);
		}
		//BookmarkOptions facets not tested:
		//MultipleResumeNonBlocking: have not tested resuming multiple times from host (when idle caused by a different bookmark)
		//MultipleResume: have not tested cannot resume from host after removed by child
		//None: have not tested cannot resume twice from host (when idle caused by a different bookmark)
		//NoneBlocking: have not tested cannot resume twice from host (when idle caused by a different bookmark)
		[Test]
		public void BookmarkOptions_None_ResumedFromHost ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
			});
			wf.InduceIdle = true;
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			bool idle = false;
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			app.Idle = args =>  {
				idle = true;
				reset.Set ();
			};
			app.Completed = args =>  {
				state = args.CompletionState;
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.IsTrue (idle);
			reset.Reset ();
			app.ResumeBookmark ("b1", "Hello\nWorld");
			reset.WaitOne ();
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, sw.ToString ());
		}
		[Test]
		public void BookmarkOptions_None_ResumeFromChild_RemovedImplicitly ()
		{
			ResumeBookmarkFromChild_CheckRemovedImplicitly (BookmarkOptions.None);
		}
		[Test]
		public void BookmarkOptions_MultipleResume_ResumedFromHostAndChild_WFNeverCompletes ()
		{
			Bookmark bookmark = null;
			var resumer = new NativeActivityRunner (null, context =>  {
				context.ResumeBookmark (bookmark, "child");
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (resumer);
			}, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.MultipleResume);
				context.ScheduleActivity (resumer);
			});
			wf.InduceIdle = true;
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			bool idle = false;
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			app.Idle = args =>  {
				idle = true;
				reset.Set ();
			};
			app.Completed = args =>  {
				state = args.CompletionState;
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.IsTrue (idle);
			idle = false;
			reset.Reset ();
			app.ResumeBookmark ("b1", "1");
			reset.WaitOne ();
			Assert.IsTrue (idle);
			idle = false;
			app.ResumeBookmark ("b1", "2");
			reset.WaitOne ();
			Assert.IsTrue (idle); // no way to end this workflow gracefully?
			Assert.AreEqual ((ActivityInstanceState) (-1), state);
			Assert.AreEqual (String.Format ("child{0}1{0}2{0}", Environment.NewLine), sw.ToString ());
		}
		[Test]
		public void BookmarkOptions_MultipleResume_ResumeFromChild_RemovedExplicitly ()
		{
			ResumeBookmarkFromChildMultipleTimes_RemoveExplicitly (BookmarkOptions.MultipleResume);
		}
		[Test]
		public void BookmarkOptions_NonBlocking_NeverResumedDoesntBlock ()
		{
			RunWFWithBookmarkCheckingItDoesntBlock (BookmarkOptions.NonBlocking);
		}
		[Test]
		public void BookmarkOptions_NonBlocking_ResumedFromHostDuringIdleFromBlockingBookmark ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("nonblocking", writeValueBookCB, BookmarkOptions.NonBlocking);
				context.CreateBookmark ("blocking", writeValueBookCB, BookmarkOptions.None);
			});
			wf.InduceIdle = true;
			StringWriter sw;
			AutoResetEvent reset;
			var app = WFApp (wf, out sw, out reset);
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			bool idle = false;
			app.Idle = args =>  {
				idle = true;
				reset.Set ();
			};
			app.Completed = args =>  {
				state = args.CompletionState;
				reset.Set ();
			};
			app.Run ();
			reset.WaitOne ();
			Assert.IsTrue (idle); 
			idle = false;
			reset.Reset ();
			app.ResumeBookmark ("nonblocking", "nonblocking");
			reset.WaitOne ();
			Assert.IsTrue (idle); 
			reset.Reset ();
			app.ResumeBookmark ("blocking", "blocking");
			reset.WaitOne ();
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.AreEqual (String.Format ("nonblocking{0}blocking{0}", Environment.NewLine), sw.ToString ());
		}
		[Test]
		public void BookmarkOptions_NonBlocking_ResumeFromChild_RemovedImplicitly ()
		{
			ResumeBookmarkFromChild_CheckRemovedImplicitly (BookmarkOptions.NonBlocking);
		}
		[Test]
		public void BookmarkOptions_MultipleResumeNonBlocking_NeverResumedDoesntBlock ()
		{
			RunWFWithBookmarkCheckingItDoesntBlock (BookmarkOptions.NonBlocking | BookmarkOptions.MultipleResume);
		}
		[Test]
		public void BookmarkOptions_MultipleResumeNonBlocking_ResumedFromChild_RemovedExplicitly ()
		{
			ResumeBookmarkFromChildMultipleTimes_RemoveExplicitly (BookmarkOptions.MultipleResume 
			                                                       | BookmarkOptions.NonBlocking);
		}
	}
}

