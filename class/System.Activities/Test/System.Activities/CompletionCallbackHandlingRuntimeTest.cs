using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class CompletionCallbackHandlingRuntimeTest : WFTestHelper {
		static BookmarkCallback writeValueBookCB = (ctx, book, value) => {
			Console.WriteLine ((string) value);
		};
		[Test]
		public void AnonymousMethod_CanAccessParamsOk ()
		{
			var writeLine = new WriteLine { Text = "WriteLine" };
			var wf = new NativeActivityRunner ((metadata)=> {
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine, (cbContext, completedInstance) => {
					cbContext.ToString ();
					completedInstance.ToString ();
				});
			});
			RunAndCompare (wf, "WriteLine" + Environment.NewLine);
		}
		Exception TryScheduleWithCallback (NativeActivityContext context, Activity activity, CompletionCallback callback)
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
			Exception t1 = null, t2 = null, t3 = null, t4 = null, t5 = null;
			ActivityInstance ai = null;
			var v1 = new Variable<string> ("name", "value");
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLine);
				metadata.AddImplementationVariable (v1);
			}, (context) => {
				t1 = TryScheduleWithCallback (context, writeLine, (ctx, instance) => {
					ctx.GetValue (v1); // fails
				});
				t2 = TryScheduleWithCallback (context, writeLine, (ctx, instance) => {
					ai = instance; // fails
				});
				t3 = TryScheduleWithCallback (context, writeLine, (ctx, instance) => {
					var myinstance = instance; // ok
				});
				t4 = TryScheduleWithCallback (context, writeLine, (ctx, instance) => {
					var myinstance = instance;
					ai = myinstance; // fails
				});
				t5 = TryScheduleWithCallback (context, writeLine, (ctx, instance) => {
					var r = ai; // fails
				});
			});
			GetWFAppWrapperAndRun (wf, WFAppStatus.CompletedSuccessfully);
			Assert.IsInstanceOfType (typeof (ArgumentException), t1);
			Assert.IsInstanceOfType (typeof (ArgumentException), t2);
			Assert.IsNull (t3);
			Assert.IsInstanceOfType (typeof (ArgumentException), t4);
			Assert.IsInstanceOfType (typeof (ArgumentException), t5);
		}
		//FIMXE: havent explicitly tested ScheduleActivityT does this with CompletionDelegate<T>
		[Test]
		public void ChildClosedAndComplete ()
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
		[Test]
		public void Simple ()
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
		public void VarAccessAndMultipleSchedules ()
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
		public void VarAccessAndSchedulesOtherActivities ()
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
		public void DoesntWaitForBookmarksResumedByChild ()
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
		public void ActivitiesScheduledInCallbackRunBeforeBookmarksResumedByChild ()
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
		public void ActivitiesScheduledInCallbackRunBeforeBookmarksResumedInExecuteButBlocksIgnored ()
		{
			var writer = new WriteLine { Text = "writer" };
			var blocker = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("blocker");
				context.CreateBookmark ("b2");
			});
			blocker.InduceIdle = true;

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (writer);
				metadata.AddChild (blocker);
			}, (context, callback) => {
				context.ScheduleActivity (writer, callback); 
				var bookmark = context.CreateBookmark ("b1", writeValueBookCB);
				context.ResumeBookmark (bookmark, "bookmarkran");
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("callbackran");
				context.ScheduleActivity (writer);
				context.ScheduleActivity (blocker);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.AreEqual (String.Format ("writer{0}callbackran{0}blocker{0}writer{0}bookmarkran{0}", 
							Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void WaitsForBookmarkCreatedByChildAndChildItSchedules ()
		{
			//FIXME: test if the bookmark resumed schedules more activities it waits for those as well
			var writeLine = new WriteLine { Text = "writeline" };
			var child = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context, callback) => {
				context.CreateBookmark ("b1", callback);
			}, (context, bookmark, value, callback) => {
				context.ScheduleActivity (writeLine);
				Console.WriteLine ((string) value);
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
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("resumed{0}writeline{0}completed{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void WaitsForMultipleResumeBookmarkCreatedByChild ()
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
	}
}

