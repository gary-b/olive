using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.Threading;
using System.IO;
using System.Linq;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
			WorkflowInvoker.Invoke (wf);
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
		[Test]
		public void CompletionCallback_WaitsForBookmark ()
		{
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			child.InduceIdle = true;

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, instance, callback) => {
				Console.WriteLine ("completed");
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual (String.Empty, app.ConsoleOut);
			app.ResumeBookmark ("b1", "resumed");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("resumed{0}completed{0}", Environment.NewLine), app.ConsoleOut);
		}
		#endregion
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
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}

		static WFAppWrapper GetWFAppWrapperAndRun (Activity wf, WFAppStatus expectedStatus)
		{
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (expectedStatus, app.Status);
			return app;
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
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			app.ResumeBookmark ("b1", "Hello\nWorld");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, app.ConsoleOut);
		}
		[Test]
		public void BookmarkCallback_AnonymousMethod ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			wf.InduceIdle = true;

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			app.ResumeBookmark ("b1", "Hello\nWorld");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, app.ConsoleOut);
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

			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
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
				bookmark = context.CreateBookmark ("b1", writeValueBookCB);
				context.ScheduleActivity (remover);
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
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

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("Activity{0}Activity{0}Bookmark{0}", Environment.NewLine), app.ConsoleOut);
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

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("Hello\nWorld{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void BookmarkResumption_FromParent ()
		{
			Bookmark bookmark = null;
			BookmarkResumptionResult result = (BookmarkResumptionResult)(-1);
			var child1 = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB);
			});
			child1.InduceIdle = true;
			var child2 = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("child2");
			});

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, (context, callback) => {
				context.ScheduleActivity (child2, callback); // callback will be called while bm active
				context.ScheduleActivity (child1); // this runs first, but completes last due to bm
			}, (context, instance, callback) => {
				result = context.ResumeBookmark (bookmark, "resumed");
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (BookmarkResumptionResult.Success, result);
			Assert.AreEqual (String.Format ("child2{0}resumed{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void RemoveBookmark_InSameMethodAsResumeBookmark ()
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

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.IsTrue (b1Removed);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, b1Resumed);
			
			Assert.AreEqual (BookmarkResumptionResult.Success, b2Resumed);
			Assert.IsFalse (b2Removed);
			Assert.AreEqual ("b2" + Environment.NewLine, app.ConsoleOut);
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
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			var resumeResult = app.ResumeBookmark ("b2", "b2");
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumeResult);

		}
		#region Test different BookmarkOptions
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
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual ("resumed" + Environment.NewLine, app.ConsoleOut);
			// check result of 2nd call to ResumeBookmark
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumeResult);
		}
		static void RunWFWithBookmarkCheckingItDoesntBlock (BookmarkOptions bookmarkOptions)
		{
			var wf = new NativeActivityRunner (null, context =>  {
				context.CreateBookmark ("b1", writeValueBookCB, bookmarkOptions);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Empty, app.ConsoleOut);
		}
		static void ResumeBookmarkFromChildMultipleTimes_RemoveExplicitly (BookmarkOptions bookmarkOptions)
		{
			Bookmark bookmark = null;
			BookmarkResumptionResult resumeResult = (BookmarkResumptionResult)(-1);
			var resumer = new NativeActivityRunner (null, context =>  {
				resumeResult = context.ResumeBookmark (bookmark, "resumed");
			});
			var wf = new NativeActWithCBRunner (metadata =>  {
				metadata.AddChild (resumer);
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
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("resumed{0}resumed{0}", Environment.NewLine), app.ConsoleOut);
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
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			app.ResumeBookmark ("b1", "Hello\nWorld");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, app.ConsoleOut);
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
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			app.ResumeBookmark ("b1", "1");
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark ("b1", "2");
			Assert.AreEqual (WFAppStatus.Idle, app.Status); // no way to end this workflow gracefully?
			Assert.AreEqual (String.Format ("child{0}1{0}2{0}", Environment.NewLine), app.ConsoleOut);
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
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			app.ResumeBookmark ("nonblocking", "nonblocking");
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark ("blocking", "blocking");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("nonblocking{0}blocking{0}", Environment.NewLine), app.ConsoleOut);
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
		#endregion

		#region BookmarkScope Tests
		static void CheckCantResumeBookmarkFromHost (Activity wf, string name)
		{
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			var resumeResult = app.ResumeBookmark (name, name);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumeResult);
		}
		[Test]
		public void BookmarkScope_NotSupplied_Defaults ()
		{
			Assert.IsNotNull (BookmarkScopeHandle.Default.BookmarkScope);
			Assert.AreSame (BookmarkScopeHandle.Default.BookmarkScope, BookmarkScope.Default);
			bool try1 = false, try2 = false;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1");
				try1 = context.RemoveBookmark ("b1", BookmarkScope.Default);
				try2 = context.RemoveBookmark ("b1", context.DefaultBookmarkScope);
			});

			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			BookmarkInfo b1Info = app.GetBookmarks ().Single (i => i.BookmarkName == "b1");
			Assert.IsNull (b1Info.ScopeInfo);
			Assert.IsFalse (try1);
			Assert.IsFalse (try2);
		}
		[Test]
		public void BookmarkScope_UnregisteredBookmarkScope ()
		{
			//System.InvalidOperationException: Only registered bookmark scopes can be used for creating scoped bookmarks.
			var bs = new BookmarkScope (Guid.NewGuid ());
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				try {
					context.CreateBookmark ("b1", writeValueBookCB, bs, BookmarkOptions.MultipleResume);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void BookmarkScope_CantCallInitializeAfterInstantiation ()
		{
			//The  bookmark scope cannot be initialized because it is already initialized.
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				try {
					var bs = new BookmarkScope (Guid.NewGuid ());
					bs.Initialize (context, Guid.NewGuid ()); // same with passing bs.Id
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void BookmarkScope_BookmarkDefaultAndContextDefaultBookmark_NotSame ()
		{
			BookmarkScope contextDefault = null, bookmarkDefault = null;
			var wf = new NativeActivityRunner (null, (context) => {
				contextDefault = context.DefaultBookmarkScope;
				bookmarkDefault = BookmarkScope.Default;
			});
			wf.InduceIdle = true;
			WorkflowInvoker.Invoke (wf);
			Assert.AreNotSame (contextDefault, bookmarkDefault);
		}
		[Test]
		public void BookmarkScope_ContextDefault ()
		{
			BookmarkScope defaultScope = null;
			var wf = new NativeActivityRunner (null, (context) => {
				defaultScope = context.DefaultBookmarkScope;
				context.CreateBookmark ("b1", writeValueBookCB, defaultScope);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);

			Assert.IsNotNull (defaultScope);
			Assert.IsFalse (defaultScope.IsInitialized);

			BookmarkInfo b1Info = app.GetBookmarks ().Single (i => i.BookmarkName == "b1");

			Assert.IsNotNull (b1Info.ScopeInfo);
			Assert.IsFalse (b1Info.ScopeInfo.IsInitialized);
			Assert.AreEqual (Guid.Empty, b1Info.ScopeInfo.Id);
			Assert.AreNotEqual (Guid.Empty, b1Info.ScopeInfo.TemporaryId);
		}
		[Test]
		public void BookmarkScope_ContextDefault_CantResumeFromHost ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB, context.DefaultBookmarkScope, 
				                        BookmarkOptions.MultipleResume);
			});
			wf.InduceIdle = true;
			CheckCantResumeBookmarkFromHost (wf, "b1");
		}
		[Test]
		public void BookmarkScope_ContextDefault_CantResumeFromChild ()
		{
			Bookmark bookmark = null;
			BookmarkResumptionResult result = (BookmarkResumptionResult)(-1);
			var resumer = new NativeActivityRunner (null, (context) => {
				result = context.ResumeBookmark (bookmark, "resumed");
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (resumer);
			}, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, context.DefaultBookmarkScope, 
				                                   BookmarkOptions.NonBlocking);
				context.ScheduleActivity (resumer);
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, result);
		}
		[Test]
		public void BookmarkScope_BookmarkScopeDefault ()
		{
			BookmarkScope defaultScope = null;
			var wf = new NativeActivityRunner (null, (context) => {
				defaultScope = BookmarkScope.Default;
				context.CreateBookmark ("b1", writeValueBookCB, defaultScope);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.IsNotNull (defaultScope);
			Assert.IsTrue (defaultScope.IsInitialized); // this is the only difference to BookmarkScope_ContextDefault
			Assert.AreEqual (Guid.Empty, defaultScope.Id);

			BookmarkInfo b2Info = app.GetBookmarks ().Single (i => i.BookmarkName == "b1");
			Assert.IsNotNull (b2Info.ScopeInfo);
			Assert.IsFalse (b2Info.ScopeInfo.IsInitialized);
			Assert.AreEqual (Guid.Empty, b2Info.ScopeInfo.Id);
			Assert.AreNotEqual (Guid.Empty, b2Info.ScopeInfo.TemporaryId);
		}
		[Test]
		public void BookmarkScope_BookmarkScopeDefault_CantResumeFromHost ()
		{
			var wf = new NativeActivityRunner (null, context =>  {
				context.CreateBookmark ("b1", writeValueBookCB, BookmarkScope.Default, BookmarkOptions.MultipleResume);
			});
			wf.InduceIdle = true;
			CheckCantResumeBookmarkFromHost (wf, "b1");
		}
		[Test]
		public void BookmarkScope_BookmarkScopeDefault_CantResumeFromChild ()
		{
			Bookmark bookmark = null;
			BookmarkResumptionResult result = (BookmarkResumptionResult)(-1);
			var resumer = new NativeActivityRunner (null, (context) => {
				result = context.ResumeBookmark (bookmark, "resumed");
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (resumer);
			}, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, BookmarkScope.Default, 
				                                   BookmarkOptions.NonBlocking);
				context.ScheduleActivity (resumer);
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, result);
		}
		[Test]
		public void BookmarkScope_RemoveFromSameActivity ()
		{
			bool removeWithScope = false, removeWithoutScope = false;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB, context.DefaultBookmarkScope, 
				                        BookmarkOptions.None);
				removeWithoutScope = context.RemoveBookmark ("b1");
				removeWithScope = context.RemoveBookmark ("b1", context.DefaultBookmarkScope);
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.IsFalse (removeWithoutScope);
			Assert.IsTrue (removeWithScope);
		}
		[Test]
		public void BookmarkScope_CantRemoveFromParent ()
		{
			//Bookmarks can only be removed by the activity instance that created them.
			BookmarkScope scope = null;
			Exception ex = null;
			var remover = new NativeActivityRunner (null, (context) => {
				try {
					context.RemoveBookmark ("b1", scope);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (remover);
			}, (context) => {
				scope = context.DefaultBookmarkScope;
				context.CreateBookmark ("b1", writeValueBookCB, scope, 
				                        BookmarkOptions.MultipleResume);
				context.ScheduleActivity (remover);
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void Bookmarks_DupeName_NoScopeNotOk ()
		{
			//A bookmark with the name 'b1' already exists.
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1");
				try {
					context.CreateBookmark ("b1");
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void Bookmarks_DupeName_ParentAndChild_NoScopeNotOk ()
		{
			//A bookmark with the name 'b1' already exists.
			Exception ex = null;
			var child = new NativeActivityRunner (null, (context) => {
				try {
					context.CreateBookmark ("b1");
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.CreateBookmark ("b1");
				context.ScheduleActivity (child);
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void BookmarkScope_DupeName_NoScopeAndScopedOK ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1");
				context.CreateBookmark ("b1", writeValueBookCB, BookmarkScope.Default);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual (2, app.GetBookmarks ().Count);
		}
		[Test]
		public void BookmarkScope_DupeName_DifferentScopesNotOK ()
		{
			//A bookmark with the name 'b1' already exists.
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB, BookmarkScope.Default);
				try {
					context.CreateBookmark ("b1", writeValueBookCB, context.DefaultBookmarkScope);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void Bookmarks_DupeName_SameScopeNotOK ()
		{
			//A bookmark with the name 'b1' already exists.
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB, context.DefaultBookmarkScope);
				try {
					context.CreateBookmark ("b1", writeValueBookCB, context.DefaultBookmarkScope);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = true;

			GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void BookmarkScope_BookmarkScopeHandle ()
		{
			//System.Activities.BookmarkScopeHandle
			int propNoPar = 0, propNoChild = 0, postCreatePropNoPar = 0;
			BookmarkScopeHandle handle = null;
			BookmarkScope handleBookmarkScope = null, createdBookmarkScope = null;
			string str = null; 
			Activity handleOwnerAct = null;
			var child = new NativeActivityRunner (null, (context) => {
				propNoChild = context.Properties.Count ();
			});
			var vHandle = new Variable<BookmarkScopeHandle> ();
			var vStr = new Variable<string> ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vHandle);
				metadata.AddImplementationVariable (vStr);
				metadata.AddImplementationChild (child);
			}, (context) => {
				propNoPar = context.Properties.Count ();
				handle = vHandle.Get (context);
				str = vStr.Get (context);
				//handle.Initialize (context, Guid.NewGuid ()); // throws NRE
				handleOwnerAct = handle.Owner.Activity;
				handleBookmarkScope = handle.BookmarkScope;
				handle.CreateBookmarkScope (context);
				createdBookmarkScope = handle.BookmarkScope;
				postCreatePropNoPar = context.Properties.Count ();
				context.CreateBookmark ("b1", writeValueBookCB, createdBookmarkScope, BookmarkOptions.NonBlocking);
				context.ScheduleActivity (child);

			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (0, propNoPar); // no execution properties of any kind by default
			Assert.AreEqual (0, propNoChild);
			Assert.IsNotNull (handle, "handle");
			Assert.IsNull (str);
			Assert.AreEqual (wf, handleOwnerAct);
			Assert.IsNull (handleBookmarkScope);
			Assert.AreNotSame (BookmarkScope.Default, handle);
			Assert.IsNotNull (createdBookmarkScope);
			Assert.IsFalse (createdBookmarkScope.IsInitialized);
			Assert.AreEqual (0, postCreatePropNoPar);
		}
		[Test] //FIXME: move to more suitable file
		public void Handle ()
		{
			var v1 = new Variable<NoPersistHandle> ();
			//var v2 = new Variable<CorrelationHandle> ();
			var v3 = new Variable<RuntimeTransactionHandle> ();
			var v4 = new Variable<ExclusiveHandle> ();
			var v5 = new Variable<BookmarkScopeHandle> ();
			object e1 = null, /*e2 = null,*/ e3 = null, e4 = null, e5 = null;
			object o1 = null, /*o2 = null,*/ o3 = null, o4 = null, o5 = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (v1);
				//metadata.AddImplementationVariable (v2);
				metadata.AddImplementationVariable (v3);
				metadata.AddImplementationVariable (v4);
				metadata.AddImplementationVariable (v5);
			}, (context) => {
				e1 = context.Properties.Find ((new NoPersistHandle()).ExecutionPropertyName);
				//e2 = context.Properties.Find ((new CorrelationHandle ()).ExecutionPropertyName);
				e3 = context.Properties.Find ((new RuntimeTransactionHandle ()).ExecutionPropertyName);
				e4 = context.Properties.Find ((new ExclusiveHandle ()).ExecutionPropertyName);
				e5 = context.Properties.Find ((new BookmarkScopeHandle ()).ExecutionPropertyName);
				o1 = v1.Get (context);
				//o2 = v2.Get (context);
				o3 = v3.Get (context);
				o4 = v4.Get (context);
				o5 = v5.Get (context);
			});
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.IsNull (e1);
			//Assert.IsNull (e2);
			Assert.IsNull (e3);
			Assert.IsNull (e4);
			Assert.IsNull (e5);
			Assert.IsNotNull (o1);
			//Assert.IsNotNull (o2);
			Assert.IsNotNull (o3);
			Assert.IsNotNull (o4);
			Assert.IsNotNull (o5);
		}
		[Test]
		public void BookmarkScope_AddedAsExecutionProperty ()
		{
			var v1 = new Variable<BookmarkScopeHandle> ();
			Bookmark bm = null;
			BookmarkResumptionResult result = (BookmarkResumptionResult)(-1);
			var resumer = new NativeActivityRunner (null, (context) => {
				result = context.ResumeBookmark (bm, "resumed");
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (v1);
				metadata.AddImplementationChild (resumer);
			}, (context) => {
				var o = v1.Get (context);
				o.CreateBookmarkScope (context);
				context.Properties.Add (o.ExecutionPropertyName, o);
				bm = context.CreateBookmark ("b1", writeValueBookCB, o.BookmarkScope);
				context.ScheduleActivity (resumer);
			});
			wf.InduceIdle = true;

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, app.ResumeBookmark ("b1", "b1"));
			Assert.AreEqual (BookmarkResumptionResult.NotFound, result);
		}
		#endregion

		#region TExecutionOrder
		[Test]
		public void Bookmarks_DoesntStopScheduledActivitiesRunning ()
		{
			var act1 = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("act1");
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			act1.InduceIdle = true;
			var act2 = new WriteLine { Text = "act2" };

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (act1);
				metadata.AddChild (act2);
			}, (context) => {
				context.ScheduleActivity (act2); // runs second
				context.ScheduleActivity (act1); // runs first
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual (String.Format ("act1{0}act2{0}", Environment.NewLine), app.ConsoleOut);
			app.ResumeBookmark ("b1", "resumed");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("act1{0}act2{0}resumed{0}", Environment.NewLine), 
			                 app.ConsoleOut);
		}
		#endregion

		#region Test WFAppWrapper class
		[Test]
		public void WFAppWrapper_UnhandledException ()
		{
			Exception exception = new Exception();
			var wf = new NativeActivityRunner (null, (context) => {
				throw exception;
			});
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.UnhandledException, app.Status);
			Assert.AreSame (exception, app.UnhandledException);
		}
		[Test]
		public void WFAppWrapper_Idle_Resume_Complete ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB);
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark ("b1", "hello\nworld");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual ("hello\nworld" + Environment.NewLine, app.ConsoleOut);
		}
		[Test]
		public void WFAppWrapper_Run ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("hello\nworld");
			});
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual ("hello\nworld" + Environment.NewLine, app.ConsoleOut);
		}
		[Test]
		public void WFAppWrapper_GetBookmarks ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1");
				context.CreateBookmark ("b2");
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			var bms = app.GetBookmarks ();
			Assert.AreEqual (2, bms.Count);
		}
		#endregion
	}
}

