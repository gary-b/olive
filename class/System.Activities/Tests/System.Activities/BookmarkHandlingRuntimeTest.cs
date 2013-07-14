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
		[Test]
		public void CompletionCallback_ChildClosedAndComplete ()
		{
			var writeLine = new WriteLine { Text = "WriteLine" };
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			bool complete = false;
			var wf = new NativeActWithCBRunner ((metadata)=> {
				metadata.AddChild (writeLine);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine, callback);
			}, (cbContext, completedInstance, callback) => {
				Console.WriteLine ("Callback");
				state = completedInstance.State;
				complete = completedInstance.IsCompleted;
			});
			RunAndCompare (wf, "WriteLine" + Environment.NewLine + "Callback" + Environment.NewLine);
			Assert.AreEqual (ActivityInstanceState.Closed, state);
			Assert.IsTrue (complete);
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
		public void CompletionCallback_DoesntWaitForBookmarksResumedByChild ()
		{
			Bookmark bookmark = null;
			var child1 = new NativeActivityRunner (null, (context) => {
				context.ResumeBookmark (bookmark, "bookmarkran");
			});

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.ScheduleActivity (child1, callback); 
				bookmark = context.CreateBookmark ("b1", writeValueBookCB);
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("callbackran");
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("callbackran{0}bookmarkran{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CompletionCallback_ActivitiesScheduledInCallbackRunBeforeBookmarksResumedByChild ()
		{
			Bookmark bookmark = null;
			var child1 = new NativeActivityRunner (null, (context) => {
				context.ResumeBookmark (bookmark, "bookmarkran");
			});
			var writer = new WriteLine { Text = "writer" };

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (writer);
			}, (context, callback) => {
				context.ScheduleActivity (child1, callback); 
				bookmark = context.CreateBookmark ("b1", writeValueBookCB);
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("callbackran");
				context.ScheduleActivity (writer);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("callbackran{0}writer{0}bookmarkran{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CompletionCallback_ActivitiesScheduledInCallbackRunBeforeBookmarksResumedInExecute ()
		{
			var writer = new WriteLine { Text = "writer" };

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (writer);
			}, (context, callback) => {
				context.ScheduleActivity (writer, callback); 
				var bookmark = context.CreateBookmark ("b1", writeValueBookCB);
				context.ResumeBookmark (bookmark, "bookmarkran");
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("callbackran");
				context.ScheduleActivity (writer);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("writer{0}callbackran{0}writer{0}bookmarkran{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void CompletionCallback_WaitsForBookmarkCreatedByChild ()
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
		[Test]
		public void CompletionCallback_WaitsForMultipleResumeBookmarkCreatedByChild ()
		{
			int i = 0;
			var child = new NativeActWithBookmarkRunner (null, (context, bookmarkCallback) => {
				context.CreateBookmark ("b1", bookmarkCallback, BookmarkOptions.MultipleResume);
			}, (context, bookmark, value, bookmarkCallback) => {
				Console.WriteLine ((string) value);
				if (++i == 2)
					context.RemoveBookmark (bookmark);
			});

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
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			Assert.AreEqual (String.Format ("resumed{0}", Environment.NewLine), app.ConsoleOut);
			app.ResumeBookmark ("b1", "resumed");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("resumed{0}resumed{0}completed{0}", Environment.NewLine), app.ConsoleOut);
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
					context.CreateBookmark ("b1", (context2, mark, value)=>{}, BookmarkOptions.NonBlocking);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = false;
			WorkflowInvoker.Invoke (wf);
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
		public void ResumeBookmark_HappensAfterScheduledChildren ()
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
		public void ResumeBookmark_FromSameActivity ()
		{
			ActivityInstance ai = null;
			ActivityInstanceState state = (ActivityInstanceState)(-1);
			bool isComplete = false;
			var child = new NativeActWithBookmarkRunner (null, (context, callback) => {
				var bookmark = context.CreateBookmark ("b1", callback, BookmarkOptions.None);
				context.ResumeBookmark (bookmark, "Hello\nWorld");
			}, (context, bookmark, value, callback) => {
				Console.WriteLine ((string) value);
				state = ai.State;
				isComplete = ai.IsCompleted;
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				ai = context.ScheduleActivity (child);
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("Hello\nWorld{0}", Environment.NewLine), app.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Executing, state);
			Assert.IsFalse (isComplete);
		}
		[Test]
		public void ResumeBookmark_FromParentsCallback ()
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
		public void ResumeBookmark_FromParentsBookmarkCallback ()
		{
			Bookmark childBookmark = null, parBookmark = null;
			BookmarkResumptionResult result = (BookmarkResumptionResult)(-1);
			var child1 = new NativeActivityRunner (null, (context) => {
				childBookmark = context.CreateBookmark ("cb", writeValueBookCB);
				context.ResumeBookmark (parBookmark, "pbResumed");
			});
			child1.InduceIdle = true;

			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				parBookmark = context.CreateBookmark ("pb", callback);
				context.ScheduleActivity (child1);
			}, (context, bookmark, value, callback) => {
				Console.WriteLine ((string) value);
				result = context.ResumeBookmark (childBookmark, "cbResumed");
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			//Assert.AreEqual (BookmarkResumptionResult.Success, result);
			Assert.AreEqual (String.Format ("pbResumed{0}cbResumed{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void ResumeBookmark_MultipleBookmarksRunInOrderResumedFromActivity ()
		{
			//unlike scheduled children which run in reverse order
			Bookmark bookmark1 = null, bookmark2 = null;
			var child1 = new NativeActivityRunner (null, (context) => {
				context.ResumeBookmark (bookmark1, "b1");
				context.ResumeBookmark (bookmark2, "b2");
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.ScheduleActivity (child1); 
				bookmark1 = context.CreateBookmark ("b1", writeValueBookCB);
				bookmark2 = context.CreateBookmark ("b2", writeValueBookCB);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("b1{0}b2{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void ResumeBookmark_DoesntStopActivityClosing ()
		{
			Bookmark bookmark = null;
			var child1 = new NativeActivityRunner (null, (context) => {
				context.ResumeBookmark (bookmark, "resumed");
			});
			ActivityInstance childAI = null;
			bool childIsComplete = false;
			ActivityInstanceState childState = (ActivityInstanceState)(-1);
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				bookmark = context.CreateBookmark ("b1", callback);
				childAI = context.ScheduleActivity (child1);
			}, (context, bm, value, callback) => {
				Console.WriteLine ((string) value);
				childIsComplete = childAI.IsCompleted;
				childState = childAI.State;
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("resumed{0}", Environment.NewLine), app.ConsoleOut);
			Assert.IsTrue (childIsComplete);
			Assert.AreEqual (ActivityInstanceState.Closed, childState);
		}
		[Test]
		public void ResumeBookmark_DetectsBookmarkAlreadyResumedOnceInSameActivity ()
		{
			BookmarkResumptionResult resume1 = (BookmarkResumptionResult)(-1), resume2 = resume1;
			var wf = new NativeActivityRunner (null, (context) => {
				var bookmark = context.CreateBookmark ("name");
				resume1 = context.ResumeBookmark (bookmark, null);
				resume2 = context.ResumeBookmark (bookmark, null);
			});
			wf.InduceIdle = true;
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (BookmarkResumptionResult.Success, resume1);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resume2);
		}
		[Test]
		public void ResumeBookmark_BookmarksResumedFromBlockedChildRunBeforeItIsResumedItself ()
		{
			Bookmark bookmark = null;
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b2", writeValueBookCB);
				context.ResumeBookmark (bookmark, "b1resumed");
			});
			child.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.None);
				context.ScheduleActivity (child);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual ("b1resumed" + Environment.NewLine, app.ConsoleOut);
			app.ResumeBookmark ("b2", "b2resumed");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("b1resumed{0}b2resumed{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void BookmarkOptions_None_RemoveBookmark_InSameMethodAsResumeBookmark ()
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
		public void BookmarkOptions_MultipleResume_RemoveBookmark_InSameMethodAsResumeBookmark ()
		{
			bool b1Removed = false;
			BookmarkResumptionResult b1Resumed = (BookmarkResumptionResult)(-1);
			var wf = new NativeActivityRunner ((metadata) => {
			}, (context) => {
				//b1 is resumed then removed
				var b1 = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.MultipleResume);
				b1Resumed = context.ResumeBookmark (b1, "b1");
				b1Removed = context.RemoveBookmark (b1);
			});
			wf.InduceIdle = true;

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);

			Assert.AreEqual (BookmarkResumptionResult.Success, b1Resumed);
			Assert.IsTrue (b1Removed);
			Assert.AreEqual ("b1" + Environment.NewLine, app.ConsoleOut);
		}
		[Test]
		public void BookmarkOptions_MultipleResume_CreateAndRemoveWithDupeNames ()
		{

			var wf = new NativeActivityRunner ((metadata) => {
			}, (context) => {
				var b1 = context.CreateBookmark ("b1", writeValueBookCB, BookmarkOptions.MultipleResume);
				context.ResumeBookmark (b1, "1");
				context.RemoveBookmark (b1);
				var b1a = context.CreateBookmark ("b1", (ctx, bookmark, value)=> {
					Console.WriteLine ("1a");
				}, BookmarkOptions.MultipleResume);
				context.ResumeBookmark (b1a, null);
				context.RemoveBookmark (b1a);
			});
			wf.InduceIdle = true;

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("1{0}1a{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void BookmarkOptions_None_CreateAndResumeWithDupeNames ()
		{
			var wf = new NativeActivityRunner ((metadata) => {
			}, (context) => {
				var b2 = context.CreateBookmark ("b2", writeValueBookCB, BookmarkOptions.None);
				context.ResumeBookmark (b2, "2");
				var b2a = context.CreateBookmark ("b2", (ctx, bookmark, value)=> {
					Console.WriteLine ("2a");
				}, BookmarkOptions.None);
				context.ResumeBookmark (b2a, null);
			});
			wf.InduceIdle = true;

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Format ("2{0}2a{0}", Environment.NewLine), app.ConsoleOut);
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
		void ResumeBookmarkFromChild_CheckRemovedImplicitly (BookmarkOptions bookmarkOptions)
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
		void RunWFWithBookmarkCheckingItDoesntBlock (BookmarkOptions bookmarkOptions)
		{
			var wf = new NativeActivityRunner (null, context =>  {
				context.CreateBookmark ("b1", writeValueBookCB, bookmarkOptions);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.AreEqual (String.Empty, app.ConsoleOut);
		}
		void ResumeBookmarkFromChildMultipleTimes_RemoveExplicitly (BookmarkOptions bookmarkOptions)
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
				context.ResumeBookmark (bookmark, "child1");
				context.ResumeBookmark (bookmark, "child2");
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
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			Assert.AreEqual (String.Format ("child1{0}child2{0}1{0}2{0}", Environment.NewLine), app.ConsoleOut);
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
		void CheckCantResumeBookmarkFromWFApp (Activity wf, string name)
		{
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			var resumeResult = app.ResumeBookmark (name, name);
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resumeResult);
		}
		[Test]
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
			Assert.AreNotEqual (contextDefault, bookmarkDefault);
			Assert.AreEqual (Guid.Empty, contextDefault.Id);
			Assert.AreEqual (Guid.Empty, bookmarkDefault.Id);
		}
		[Test]
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
		public void BookmarkScope_ContextDefault_CantResumeFromWFApp ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBookCB, context.DefaultBookmarkScope);
			});
			wf.InduceIdle = true;
			CheckCantResumeBookmarkFromWFApp (wf, "b1");
		}
		[Test]
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
		public void BookmarkScope_BookmarkScopeDefault_CantResumeFromWFApp ()
		{
			var wf = new NativeActivityRunner (null, context =>  {
				context.CreateBookmark ("b1", writeValueBookCB, BookmarkScope.Default);
			});
			wf.InduceIdle = true;
			CheckCantResumeBookmarkFromWFApp (wf, "b1");
		}
		[Test]
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("BookmarkScope")]
		public void BookmarkScope_DupeName_SameScopeNotOK ()
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
		[Ignore ("BookmarkScope")]
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
		[Ignore ("Handles")]
		public void Handles ()
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
		[Ignore ("BookmarkScope")]
		public void BookmarkScope_AddedAsExecutionProperty ()
		{
			var v1 = new Variable<BookmarkScopeHandle> ();
			Bookmark parBm = null;
			int childPropsCount = 0;
			BookmarkResumptionResult resultResumingPar = (BookmarkResumptionResult)(-1);
			BookmarkResumptionResult resultResumingChildBm = (BookmarkResumptionResult)(-1);
			var resumer = new NativeActivityRunner (null, (context) => {
				resultResumingPar = context.ResumeBookmark (parBm, "resumed");
				context.CreateBookmark ("childBm");
				childPropsCount = context.Properties.Count ();
				var childBmWithScope = context.CreateBookmark ("childBmWithScope", writeValueBookCB, 
							((BookmarkScopeHandle) (context.Properties.Single ().Value)).BookmarkScope);
				resultResumingChildBm = context.ResumeBookmark (childBmWithScope, "resumed");
			});
			resumer.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (v1);
				metadata.AddImplementationChild (resumer);
			}, (context) => {
				var o = v1.Get (context);
				o.CreateBookmarkScope (context);
				context.Properties.Add (o.ExecutionPropertyName, o);
				parBm = context.CreateBookmark ("b1", writeValueBookCB, o.BookmarkScope);
				context.ScheduleActivity (resumer);
			});
			wf.InduceIdle = true;

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);

			Assert.AreEqual (BookmarkResumptionResult.NotFound, app.ResumeBookmark ("b1", "b1"));
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resultResumingPar);

			Assert.AreEqual (1, childPropsCount);

			var b1BMInfo = app.GetBookmarks ().Single (b=> b.BookmarkName == "b1");
			Assert.IsNotNull (b1BMInfo.ScopeInfo);
			//check no change to how bookmarks without bookmarkscope created
			var childBMInfo = app.GetBookmarks ().Single (b=> b.BookmarkName == "childBm");
			Assert.IsNull (childBMInfo.ScopeInfo);
			//check bookmark created in child with bookmarkscope created ok
			var childBMWithScopeInfo = app.GetBookmarks ().Single (b=> b.BookmarkName == "childBmWithScope");
			Assert.IsNotNull (childBMWithScopeInfo.ScopeInfo);
			//check bookmark created in child with bookmarkscope cant be resumed
			Assert.AreEqual (BookmarkResumptionResult.NotFound, resultResumingChildBm);
		}
		#endregion

		#region Parallel Execution
		[Test]
		public void Parallel_BookmarkDoesntStopOtherActivitiesRunning ()
		{
			var v1 = new Variable<string> ("v1", "v1");
			var writer = new WriteLine { Text = v1 };
			var act1 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writer);
			}, (context, bookmarkCallback) => {
				Console.WriteLine ("act1");
				context.CreateBookmark ("b1", bookmarkCallback);
			}, (context, bookmark, value, bookmarkCallback) => {
				context.ScheduleActivity (writer);
				Console.WriteLine ((string) value);
			});

			var act2 = new WriteLine { Text = "act2" };

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (v1);
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
			Assert.AreEqual (String.Format ("act1{0}act2{0}resumed{0}v1{0}", Environment.NewLine), 
			                 app.ConsoleOut);
		}
		[Test]
		public void Parallel_ChildPreservedWhileGrandChildBlocks ()
		{
			//demonstrates variable still accessibly on bookmark callback
			var v1 = new Variable<string> ("v1", "v1");

			var writer = new WriteLine { Text = v1 };

			var grandChild = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writer);
			}, (context, bookmarkCallback) => {
				context.CreateBookmark ("b1", bookmarkCallback);
			}, (context, bookmark, value, bookmarkCallback) => {
				context.ScheduleActivity (writer);
				Console.WriteLine ((string) value);
			});

			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (v1);
				metadata.AddImplementationChild (grandChild);
			}, (context) => {
				context.ScheduleActivity (grandChild);
			});

			var child2 = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("child2");
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child1);
				metadata.AddImplementationChild (child2);
			}, (context) => {
				context.ScheduleActivity (child2); //runs 2nd
				context.ScheduleActivity (child1); //runs 1st
			});

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual ("child2" + Environment.NewLine, app.ConsoleOut);
			app.ResumeBookmark ("b1", "host");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("child2{0}host{0}v1{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Parallel_ChildPreservedWhileMultipleGrandChildrenBlockAndResume ()
		{
			var v1 = new Variable<string> ("v1", "v1");

			var writer1 = new WriteLine { Text = v1 };
			var writer2 = new WriteLine { Text = v1 };

			var grandChild1 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writer1);
			}, (context, bookmarkCallback) => {
				context.CreateBookmark ("b1", bookmarkCallback);
			}, (context, bookmark, value, bookmarkCallback) => {
				context.ScheduleActivity (writer1);
				Console.WriteLine ((string) value);
			});
			var grandChild2 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writer2);
			}, (context, bookmarkCallback) => {
				context.CreateBookmark ("b2", bookmarkCallback);
			}, (context, bookmark, value, bookmarkCallback) => {
				context.ScheduleActivity (writer2);
				Console.WriteLine ((string) value);
			});

			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (v1);
				metadata.AddImplementationChild (grandChild1);
				metadata.AddImplementationChild (grandChild2);
			}, (context) => {
				context.ScheduleActivity (grandChild1);
				context.ScheduleActivity (grandChild2);
			});

			var child2 = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("child2");
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child1);
				metadata.AddImplementationChild (child2);
			}, (context) => {
				context.ScheduleActivity (child2); //runs 2nd
				context.ScheduleActivity (child1); //runs 1st
			});

			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual ("child2" + Environment.NewLine, app.ConsoleOut);
			app.ResumeBookmark ("b1", "host1");
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			Assert.AreEqual (String.Format ("child2{0}host1{0}v1{0}", Environment.NewLine), app.ConsoleOut);
			app.ResumeBookmark ("b2", "host2");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("child2{0}host1{0}v1{0}host2{0}v1{0}", Environment.NewLine), app.ConsoleOut);
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

