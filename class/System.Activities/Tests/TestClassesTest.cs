using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	public class TestClassesTest : WFTestHelper {
		#region Test WFAppWrapper class
		static BookmarkCallback writeValueBookCB = (ctx, book, value) => {
			Console.WriteLine ((string) value);
		};
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
		#region ConcatMany Tests
		[Test]
		public void TestConcatMany_AllPresent ()
		{
			var v1 = new Variable<string> ();
			var wf = new Sequence {
				Variables = { v1 },
				Activities = {
					new ConcatMany {
						String1 = "1", String2 = "2", String3 = "3", String4 = "4",
						String5 = "5", String6 = "6", String7 = "7", String8 = "8",
						String9 = "9", String10 = "10", String11 = "11", String12 = "12",
						String13 = "13", String14 = "14", String15 = "15", String16 = "16",
						Result = new OutArgument<string> (v1)
					},
					new WriteLine { Text = v1 }
				}
			};
			RunAndCompare (wf, "12345678910111213141516" + Environment.NewLine);
		}
		[Test]
		public void TestConcatMany_EvensPresent ()
		{
			var v1 = new Variable<string> ();
			var wf = new Sequence {
				Variables = { v1 },
				Activities = {
					new ConcatMany {
						String2 = "2", String4 = "4", String6 = "6", String8 = "8",
						String10 = "10", String12 = "12", String14 = "14", String16 = "16",
						Result = new OutArgument<string> (v1)
					},
					new WriteLine { Text = v1 }
				}
			};
			RunAndCompare (wf, "246810121416" + Environment.NewLine);
		}
		#endregion
	}
}

