using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ExecutionPropertyHandlingRuntimeTest : WFTestHelper {
		[Test]
		public void ExecutionProperties_Find_PropsAvailableToChildrenAndNotParent ()
		{
			var str = "str";
			var child1_1_1 = new NativeActivityRunner (null, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
			});
			var child1_1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1_1);
			}, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (child1_1_1);
			});
			var child1 = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context, callback) => {
				context.Properties.Add ("name", str);
				context.ScheduleActivity (child1_1, callback);
			}, (context, compAI, callback) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				Assert.IsNull (context.Properties.Find ("name"));
				context.ScheduleActivity (child1, callback);
			}, (context, compAI, callback) => {
				Assert.IsNull (context.Properties.Find ("name"));
			});

			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_Add_DupeNamesWithinActivityEx ()
		{
			// A property with the name 'name' has already been defined at this scope. To replace the current
			// property, first remove it and then add the new property.
			Exception exception = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", "str1");
				try {
					context.Properties.Add ("name", "str2");
				} catch (Exception ex) {
					exception = ex;
				}
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (ArgumentException), exception);
		}
		[Test]
		public void ExecutionProperties_Add_DupeNamesInParentAndChild ()
		{
			string str1 = "str1", str2 = "str2";
			var child1_1 = new NativeActivityRunner (null, (context) => {
				Assert.AreSame (str2, context.Properties.Find ("name"));
			});
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				Assert.AreSame (str1, context.Properties.Find ("name"));
				context.Properties.Add ("name", str2);
				Assert.AreSame (str2, context.Properties.Find ("name"));
				context.ScheduleActivity (child1_1);
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.Properties.Add ("name", str1);
				Assert.AreSame (str1, context.Properties.Find ("name"));
				context.ScheduleActivity (child1, callback);
			}, (context, compAI, callback) => {
				Assert.AreSame (str1, context.Properties.Find ("name"));
			});

			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_Remove_Self ()
		{
			var str = "str";
			var wf = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", str);
				Assert.AreSame (str, context.Properties.Find ("name"));
				Assert.IsTrue (context.Properties.Remove ("name"));
				Assert.IsNull (context.Properties.Find ("name"));
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_Remove_NotFound ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				Assert.IsFalse (context.Properties.Remove ("name"));
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_Remove_CantRemoveParents ()
		{
			string str = "str";
			var child1 = new NativeActivityRunner (null, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
				Assert.IsFalse (context.Properties.Remove ("name"));
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.Properties.Add ("name", str);
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (child1, callback);
			}, (context, compAI, callback) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_Add_boolOnlyVisibleToPubChildren_True ()
		{
			string str = "str";
			var impChildPubChild = new NativeActivityRunner (null, (context) => {
				Assert.IsNull (context.Properties.Find ("name"));
			});
			var impChild = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (impChildPubChild);
			}, (context) => {
				Assert.IsNull (context.Properties.Find ("name"));
				context.ScheduleActivity (impChildPubChild);
			});
			var pubChildImpChild = new NativeActivityRunner (null, (context) => {
				Assert.IsNull (context.Properties.Find ("name"));
			});
			var pubChild1 = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (pubChildImpChild);
			}, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (pubChildImpChild);
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (pubChild1);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.Properties.Add ("name", str, true);
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (pubChild1);
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_Add_boolOnlyVisibleToPubChildren_False ()
		{
			string str = "str";
			var impChildPubChild = new NativeActivityRunner (null, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
			});
			var impChild = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (impChildPubChild);
			}, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (impChildPubChild);
			});
			var pubChildImpChild = new NativeActivityRunner (null, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
			});
			var pubChild1 = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (pubChildImpChild);
			}, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (pubChildImpChild);
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (pubChild1);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.Properties.Add ("name", str, false);
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (pubChild1);
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_Add_boolOnlyVisibleToPubChildren_NotSuppliedSameAsFalse ()
		{
			string str = "str";
			var impChildPubChild = new NativeActivityRunner (null, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
			});
			var impChild = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (impChildPubChild);
			}, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (impChildPubChild);
			});
			var pubChildImpChild = new NativeActivityRunner (null, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
			});
			var pubChild1 = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (pubChildImpChild);
			}, (context) => {
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (pubChildImpChild);
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (pubChild1);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.Properties.Add ("name", str);
				Assert.AreSame (str, context.Properties.Find ("name"));
				context.ScheduleActivity (pubChild1);
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ExecutionProperties_CantAddOrRemove_ChildrenExecuting ()
		{
			//System.InvalidOperationException : An activity cannot add or remove workflow 
			//execution properties while it has executing children.
			var str = "str";
			Exception addException = null, removeException = null;
			Bookmark bookmark1 = null, bookmark2 = null;
			var child = new NativeActWithBookmarkRunner (null, (context, callback) => {
				bookmark2 = context.CreateBookmark (callback);
				context.ResumeBookmark (bookmark1, "bookmark1Resumed");
			}, (context, bm, value, callback) => {
				Console.WriteLine (value);
			}); 
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.Properties.Add ("other", "other");
				bookmark1 = context.CreateBookmark (callback);
				context.ScheduleActivity (child);
			}, (context, bm, value, callback) => {
				try {
					context.Properties.Add ("name", str);
				} catch (Exception ex) {
					addException = ex;
				}
				Assert.IsFalse (context.Properties.Remove ("blabla"));
				try {
					context.Properties.Remove ("other");
				} catch (Exception ex) {
					removeException = ex;
				}
				context.ResumeBookmark (bookmark2, "bookmark2Resumed");
				Console.WriteLine (value);
			});
			RunAndCompare (wf, String.Format ("bookmark1Resumed{0}bookmark2Resumed{0}", Environment.NewLine));
			Assert.IsInstanceOfType (typeof (InvalidOperationException), addException);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), removeException);
		}
		[Test]
		public void ExecutionProperties_CantAddOrRemove_AfterChildScheduled ()
		{
			//FIXME: test scheduling a delegate
			var child = new WriteLine ();
			Exception addException = null, removeException = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child);
			}, (context) => {
				context.Properties.Add ("1", "str");
				context.ScheduleActivity (child);
				try {
					context.Properties.Add ("2", "str");
				} catch (Exception ex) {
					addException = ex;
				}
				context.Properties.Remove ("dont exist");
				try {
					context.Properties.Remove ("1");
				} catch (Exception ex) {
					removeException = ex;
				}
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), addException);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), removeException);
		}
		[Test]
		public void ExecutionProperties_CanAddAndRemove_NoChildrenExecuting ()
		{
			string str = "str";
			int i = 0;
			var child1 = new NativeActivityRunner (null, (context) => {
				Console.WriteLine (context.Properties.Find ("name"));
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.ScheduleActivity (child1, callback);
			}, (context, compAI, callback) => {
				if (i == 0) {
					context.Properties.Add ("name", str);
					context.ScheduleActivity (child1, callback);
				} else if (i == 1) {
					context.Properties.Remove ("name");
					context.ScheduleActivity (child1, callback);
				}
				i++;
			});
			RunAndCompare (wf, String.Format ("{0}str{0}{0}", Environment.NewLine));
		}
		[Test]
		public void ExecutionProperties_Find_CantAccessSiblingTree () 
		{
			var str = "str";
			Bookmark bookmark = null;
			var child2 = new NativeActivityRunner (null, (context) => {
				Assert.IsNull (context.Properties.Find ("name"));
				context.ResumeBookmark (bookmark, context.Properties.Find ("name"));
			});

			var child1 = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", str);
				bookmark = context.CreateBookmark ((ctx, bk, value) => {
					Console.WriteLine (value);
				});
			});
			child1.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, (context) => {
				context.ScheduleActivity (child2);
				context.ScheduleActivity (child1); //  runs first
			});
			RunAndCompare (wf, Environment.NewLine);
		}
		[Test]
		public void ExecutionProperties_Enumerator_RespectsVisibility ()
		{
			string str1 = "str1", str2 = "str2", str3 = "str3", str4 = "str4", str5 = "str5";
			var list = new List<object> ();
			var impChild = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("str2", str2);
				context.Properties.Add ("name", str5);
				foreach (var p in context.Properties)
					list.Add (p.Value);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.Properties.Add ("str1", str1);
				context.Properties.Add ("str3", str3, true);
				context.Properties.Add ("name", str4);
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (3, list.Count);
			Assert.Contains (str1, list);
			Assert.Contains (str2, list);
			Assert.Contains (str5, list);
		}
		[Test]
		public void ExecutionProperties_IsEmpty_CountsInvisible ()
		{
			int impChild1Count = 0, impChild1_1Count = 0;
			var impChild1_1 = new NativeActivityRunner (null, (context) => {
				Assert.IsFalse (context.Properties.IsEmpty);
				Assert.IsNull (context.Properties.Find ("par"));
				Assert.IsNotNull (context.Properties.Find ("child1"));
				foreach (var p in context.Properties)
					impChild1_1Count++;
			});
			var impChild1 = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (impChild1_1);
			}, (context) => {
				Assert.IsFalse (context.Properties.IsEmpty);
				Assert.IsNull (context.Properties.Find ("par")); //thus IsEmpty counts invisible prop
				foreach (var p in context.Properties)
					impChild1Count++;
				context.Properties.Add ("child1", "str");
				context.ScheduleActivity (impChild1_1);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (impChild1);
			}, (context) => {
				Assert.IsTrue (context.Properties.IsEmpty);
				context.Properties.Add ("par", "str", true);
				context.ScheduleActivity (impChild1);
				Assert.IsFalse (context.Properties.IsEmpty);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (0, impChild1Count);
			Assert.AreEqual (1, impChild1_1Count);
		}
		static void ExecuteStatementAndThrow (Action<NativeActivityContext> action)
		{
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				try {
					action (context);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			if (ex != null) {
				throw ex;
			} else {
				if (app.Status != WFAppStatus.CompletedSuccessfully)
					throw new Exception ("something unexpected went wrong in the workflow");
			}
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Add_String_Object_NameNullEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Add (null, "str");
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Add_String_Object_NameEmptyEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Add (String.Empty, "str");
			});
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ExecutionProperties_Add_String_Object_NullObjectEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Add ("str", null);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Add_String_Object_Bool_NameNullEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Add (null, "str", true);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Add_String_Object_Bool_NameEmptyEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Add (String.Empty, "str", true);
			});
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ExecutionProperties_Add_String_Object_Bool_ObjectNullEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Add ("str", null, true);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Remove_NameNullEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Remove (null);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Remove_NameEmptyEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Remove (String.Empty);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Find_NameNullEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Find (null);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ExecutionProperties_Find_NameEmptyEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.Properties.Find (String.Empty);
			});
		}
		#region IExecutionProperty
		enum TLSState {
			NotCalled,
			SetupCalled,
			CleanupCalled,
		}
		class ExecPropMon : IExecutionProperty {
			public TLSState State { get; set; }
			public int SetupCalled { get; set; }
			public int CleanupCalled { get; set; }
			Action setupAction;
			Action cleanupAction;
			public ExecPropMon ()
			{
			}
			public ExecPropMon (Action setupAction, Action cleanupAction)
			{
				this.setupAction = setupAction;
				this.cleanupAction = cleanupAction;
			}
			public void SetupWorkflowThread ()
			{
				State = TLSState.SetupCalled;
				SetupCalled = ++SetupCalled;
				if (setupAction != null)
					setupAction ();
			}
			public void CleanupWorkflowThread ()
			{
				State = TLSState.CleanupCalled;
				CleanupCalled = ++CleanupCalled;
				if (cleanupAction != null)
					cleanupAction ();
			}
		}
		[Test]
		public void IExecutionProperty_SetupAndCleanupCalledOnEachActivityExecution ()
		{
			var mon = new ExecPropMon ();
			var child1_1_1 = new NativeActivityRunner (null, (context) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (2, mon.SetupCalled);
				Assert.AreEqual (1, mon.CleanupCalled);
				Assert.AreSame (mon, context.Properties.Find ("name"));
			});
			var child1_1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1_1);
			}, (context) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (1, mon.SetupCalled);
				Assert.AreEqual (0, mon.CleanupCalled);
				Assert.AreSame (mon, context.Properties.Find ("name"));
				context.ScheduleActivity (child1_1_1);
			});
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				context.Properties.Add ("name", mon);
				Assert.AreEqual (TLSState.NotCalled, mon.State);
				context.ScheduleActivity (child1_1);
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.ScheduleActivity (child1, callback);
			}, (context, compAI, callback) => {
				Assert.AreEqual (TLSState.CleanupCalled, mon.State);
				Assert.IsNull (context.Properties.Find ("name"));
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (2, mon.SetupCalled);
			Assert.AreEqual (2, mon.CleanupCalled);
		}
		[Test]
		public void IExecutionProperty_SetupAndCleanupCalledOnCallbacks ()
		{
			var mon = new ExecPropMon ();

			var child1_1 = new NativeActivityRunner (null, (context) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (2, mon.SetupCalled);
				Assert.AreEqual (1, mon.CleanupCalled);
			});
			var child1 = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context, callback) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (1, mon.SetupCalled);
				Assert.AreEqual (0, mon.CleanupCalled);
				context.ScheduleActivity (child1_1, callback);
			}, (context, compAI, callback) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (3, mon.SetupCalled);
				Assert.AreEqual (2, mon.CleanupCalled);
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.Properties.Add ("name", mon);
				Assert.AreEqual (TLSState.NotCalled, mon.State);
				context.ScheduleActivity (child1, callback);
			}, (context, compAI, callback) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (4, mon.SetupCalled);
				Assert.AreEqual (3, mon.CleanupCalled);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (4, mon.SetupCalled);
			Assert.AreEqual (4, mon.CleanupCalled);
		}
		[Test, ExpectedException (typeof (WorkflowApplicationAbortedException))]
		[Ignore ("Runtime Exceptions causing Abort")]
		public void IExecutionProperty_Setup_Throws_CantHandle ()
		{
			//FIXME: change test to use WorkflowApplication / WorkflowInstance
			/*System.Activities.WorkflowApplicationAbortedException : The workflow has been aborted.
			  ----> System.OperationCanceledException : An IExecutionProperty threw an exception 
			  while setting up or cleaning up the workflow thread.  See the inner exception for more details.
			  ----> System.Exception : Exception of type 'System.Exception' was thrown.*/
			var mon = new ExecPropMon (() => {
				throw new Exception ();
			}, null);
			var child = new WriteLine ();
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.Properties.Add ("name", mon);
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				context.HandleFault ();
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (WorkflowApplicationAbortedException))]
		[Ignore ("Runtime Exceptions causing Abort")]
		public void IExecutionProperty_Cleanup_Throws_CantHandle ()
		{
			//FIXME: change test to use WorkflowApplication / WorkflowInstance
			var mon = new ExecPropMon (null, () => {
				throw new Exception ();
			});
			var child = new WriteLine ();
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.Properties.Add ("name", mon);
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				context.HandleFault ();
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void IExecutionProperty_SetupAndCleanupCalledOnBookmarks ()
		{
			var mon = new ExecPropMon ();
			Bookmark parBookmark = null, childBookmark = null;
			var child1_1 = new NativeActivityRunner (null, (context) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (2, mon.SetupCalled);
				Assert.AreEqual (1, mon.CleanupCalled);
				context.ResumeBookmark (parBookmark, "parResumed");
			});
			var child1 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context, callback) => {
				childBookmark = context.CreateBookmark (callback);
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (1, mon.SetupCalled);
				Assert.AreEqual (0, mon.CleanupCalled);
				context.ScheduleActivity (child1_1);
			}, (context, bk, value, callback) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (4, mon.SetupCalled);
				Assert.AreEqual (3, mon.CleanupCalled);
				Console.WriteLine (value);
			});
			var wf = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				parBookmark = context.CreateBookmark (callback);
				context.Properties.Add ("name", mon);
				Assert.AreEqual (TLSState.NotCalled, mon.State);
				context.ScheduleActivity (child1);
			}, (context, bk, value, callback) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (3, mon.SetupCalled);
				Assert.AreEqual (2, mon.CleanupCalled);
				Console.WriteLine (value);
				context.ResumeBookmark (childBookmark, "childResumed");
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (4, mon.SetupCalled);
			Assert.AreEqual (4, mon.CleanupCalled);
		}
		[Test]
		public void IExecutionProperty_SetupAndCleanupCalledOnFaultCallbacks ()
		{
			//FaultCallbacks swallow exceptions raised by Assert
			var mon = new ExecPropMon ();
			var child1_1_1 = new NativeActivityRunner (null, (context) => {
				//Assert.AreEqual (2, mon.SetupCalled);
				//Assert.AreEqual (1, mon.CleanupCalled);
				throw new Exception ();
			});
			var child1_1 = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1_1_1);
			}, (context, callback) => {
				//Assert.AreEqual (1, mon.SetupCalled);
				//Assert.AreEqual (0, mon.CleanupCalled);
				context.ScheduleActivity (child1_1_1, callback);
			}, (context, ex, ai) => {
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
			});
			var child1 = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context, callback) => {
				context.Properties.Add ("name", mon);
				context.ScheduleActivity (child1_1, callback);
			}, (context, ex, ai) => {
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.ScheduleActivity (child1, callback);
			}, (context, ex, ai) => {
				//Assert.IsNull (context.Properties.Find ("name"));
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
				context.HandleFault ();
			});
			RunAndCompare (wf, String.Format ("SetupCalled-S:3C:2{0}SetupCalled-S:4C:3{0}CleanupCalled-S:4C:4{0}", Environment.NewLine));
			Assert.AreEqual (4, mon.SetupCalled);
			Assert.AreEqual (4, mon.CleanupCalled);
		}
		[Test]
		public void IExecutionProperty_SetupAndCleanupCalledOnCancel ()
		{
			//WFApp swallows exceptions raised by Assert
			var mon = new ExecPropMon ();
			var child1child1 = new NativeActivityRunner (null, (context) => {
				Thread.Sleep (TimeSpan.FromSeconds (2));
			}, "child1child1 cancel");
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1child1);
			}, (context) => {
				context.ScheduleActivity (child1child1);
			}, "child1 cancel");
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.Properties.Add ("mon", mon);
				context.ScheduleActivity (child1);
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.Run ();
			//Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (2, mon.SetupCalled);
			Assert.AreEqual (2, mon.CleanupCalled);
			Assert.AreEqual (String.Empty, app.ConsoleOut);
			mon = new ExecPropMon ();
			var appCancels = new WFAppWrapper (wf);
			appCancels.RunAndCancel (1);
			//Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (4, mon.SetupCalled);
			Assert.AreEqual (4, mon.CleanupCalled);
			Assert.AreEqual (String.Format ("wf cancel{0}" +
				"child1 cancel{0}", Environment.NewLine), appCancels.ConsoleOut);
		}
		[Test]
		public void IExecutionProperty_CleanupCalledAfterCancelMethodFaults ()
		{
			//WFApp swallows exceptions raised by Assert
			var mon = new ExecPropMon ();
			var child1 = new NativeActivityRunner (null, (context) => {
				Thread.Sleep (TimeSpan.FromSeconds (2));
			}, "child1 cancel");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.Properties.Add ("mon", mon);
				context.ScheduleActivity (child1);
			}, (context) => {
				Console.WriteLine ("wf cancel");
				throw new Exception ();
			});

			var consoleOut = new StringWriter ();
			Console.SetOut (consoleOut);
			var reset = new AutoResetEvent (false);
			var app = new WorkflowApplication (wf);
			app.OnUnhandledException = (e) =>  {
				return UnhandledExceptionAction.Terminate;
			};
			app.Completed = (e) =>  {
				reset.Set ();
			};
			Exception abortEx = null;
			app.Aborted = (e) => {
				Console.WriteLine ("Abort Raised");
				abortEx = e.Reason;
				reset.Set ();
			};
			app.Run ();
			Thread.Sleep (TimeSpan.FromSeconds (1));
			app.Cancel ();
			reset.WaitOne ();

			Assert.AreEqual (2, mon.SetupCalled);
			Assert.AreEqual (2, mon.CleanupCalled);
			Assert.AreEqual (TLSState.CleanupCalled, mon.State);
			Assert.AreEqual (String.Format ("wf cancel{0}Abort Raised{0}", Environment.NewLine), consoleOut.ToString ());
		}
		[Test]
		public void IExecutionProperty_CleanupCalledForExecutingActivityJustFaultedAndCancelCalled ()
		{
			//WFApp swallows exceptions raised by Assert
			var mon = new ExecPropMon ();
			var child1 = new NativeActivityRunner (null, (context) => {
				Thread.Sleep (TimeSpan.FromSeconds (2));
				Console.WriteLine ("child1 execute");
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
				throw new Exception ();
			}, "child1 cancel");

			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.Properties.Add ("mon", mon);
				context.ScheduleActivity (child1, callback);
			}, (context, ex, campAI) => {
				Console.WriteLine ("fault cb");
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
				context.HandleFault ();
			}, (context) => {
				Console.WriteLine ("wf cancel");
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
			});

			var appCancels = new WFAppWrapper (wf);
			appCancels.RunAndCancel (1);
			//Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (3, mon.SetupCalled);
			Assert.AreEqual (3, mon.CleanupCalled);
			Assert.AreEqual (String.Format ("child1 execute{0}" +
			                                "SetupCalled-S:1C:0{0}" +
			                                "wf cancel{0}" +
			                                "SetupCalled-S:2C:1{0}" +
			                                "fault cb{0}" +
			                                "SetupCalled-S:3C:2{0}", Environment.NewLine), appCancels.ConsoleOut);
		}
		[Test]
		public void IExecutionProperty_SetupAndCleanupCalledOnFaultCallbacks_AfterFaultCallbackFaults ()
		{
			//FaultCallbacks swallow exceptions raised by Assert
			var mon = new ExecPropMon ();
			var child1_1 = new NativeActivityRunner (null, (context) => {
				//Assert.AreEqual (2, mon.SetupCalled);
				//Assert.AreEqual (1, mon.CleanupCalled);
				throw new Exception ();
			});
			var child1 = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context, callback) => {
				//Assert.AreEqual (1, mon.SetupCalled);
				//Assert.AreEqual (0, mon.CleanupCalled);
				context.ScheduleActivity (child1_1, callback);
			}, (context, ex, ai) => {
				//Assert.AreEqual (3, mon.SetupCalled);
				//Assert.AreEqual (2, mon.CleanupCalled);
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
				throw new Exception ();
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.Properties.Add ("name", mon);
				context.ScheduleActivity (child1, callback);
			}, (context, ex, ai) => {
				//Assert.AreEqual (4, mon.SetupCalled);
				//Assert.AreEqual (3, mon.CleanupCalled);
				Console.WriteLine (String.Format ("{0}-S:{1}C:{2}", mon.State, mon.SetupCalled, mon.CleanupCalled));
				context.HandleFault ();
			});
			RunAndCompare (wf, String.Format ("SetupCalled-S:3C:2{0}SetupCalled-S:4C:3{0}", Environment.NewLine));
			Assert.AreEqual (4, mon.SetupCalled);
			Assert.AreEqual (4, mon.CleanupCalled);
		}
		[Test]
		public void IExecutionProperty_AddedPublicOnly_SetupAndCleanupStillCalledForImpChild ()
		{
			var mon = new ExecPropMon ();

			var impChild = new NativeActivityRunner (null, (context) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (1, mon.SetupCalled);
				Assert.AreEqual (0, mon.CleanupCalled);
				Assert.IsNull (context.Properties.Find ("name"));
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context, callback) => {
				context.Properties.Add ("name", mon, true);
				Assert.AreEqual (TLSState.NotCalled, mon.State);
				context.ScheduleActivity (impChild, callback);
			}, (context, compAI, callback) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (2, mon.SetupCalled);
				Assert.AreEqual (1, mon.CleanupCalled);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (2, mon.SetupCalled);
			Assert.AreEqual (2, mon.CleanupCalled);
		}
		[Test]
		public void IExecutionProperty_ParallelActivityExecution ()
		{
			var mon = new ExecPropMon ();
			Bookmark bookmark = null;
			var child2 = new NativeActivityRunner (null, (context) => {
				Assert.IsNull (context.Properties.Find ("name"));
				Assert.AreEqual (TLSState.CleanupCalled, mon.State);
				Assert.AreEqual (2, mon.SetupCalled);
				Assert.AreEqual (2, mon.CleanupCalled);
				context.ResumeBookmark (bookmark, "resumed");
			});
			var child1_1_1 = new NativeActWithBookmarkRunner (null, (context, callback) => {
				bookmark = context.CreateBookmark (callback);
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (2, mon.SetupCalled);
				Assert.AreEqual (1, mon.CleanupCalled);
			}, (context, bk, value, callback) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (3, mon.SetupCalled);
				Assert.AreEqual (2, mon.CleanupCalled);
				Console.WriteLine (value);
			});
			var child1_1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1_1);
			}, (context) => {
				Assert.AreEqual (TLSState.SetupCalled, mon.State);
				Assert.AreEqual (1, mon.SetupCalled);
				Assert.AreEqual (0, mon.CleanupCalled);
				context.ScheduleActivity (child1_1_1);
			});
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				context.Properties.Add ("name", mon);
				context.ScheduleActivity (child1_1);
			});
			child1.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, (context) => {
				context.ScheduleActivity (child2);
				context.ScheduleActivity (child1); //  runs first
			});
			RunAndCompare (wf, "resumed" + Environment.NewLine);
			Assert.AreEqual (TLSState.CleanupCalled, mon.State);
			Assert.AreEqual (3, mon.SetupCalled);
			Assert.AreEqual (3, mon.CleanupCalled);
		}
		[Test]
		public void IExecutionProperty_Multiple ()
		{
			int setupCounter = 0, mon1Setup = 0, mon2Setup = 0, mon3Setup = 0;
			int cleanupCounter = 0, mon1Cleanup = 0, mon2Cleanup = 0, mon3Cleanup = 0;

			var mon1 = new ExecPropMon (() => { mon1Setup = ++setupCounter; }, () => { mon1Cleanup = ++cleanupCounter; });
			var mon2 = new ExecPropMon (() => { mon2Setup = ++setupCounter; }, () => { mon2Cleanup = ++cleanupCounter; });
			var mon3 = new ExecPropMon (() => { mon3Setup = ++setupCounter; }, () => { mon3Cleanup = ++cleanupCounter; });

			var child1_1 = new NativeActivityRunner (null, null);
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				context.Properties.Add ("mon3", mon3);
				context.ScheduleActivity (child1_1);
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.Properties.Add ("mon1", mon1);
				context.Properties.Add ("mon2", mon2);
				context.ScheduleActivity (child1);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (3, mon1Setup);
			Assert.AreEqual (4, mon2Setup);
			Assert.AreEqual (5, mon3Setup);
			Assert.AreEqual (5, mon1Cleanup);
			Assert.AreEqual (4, mon2Cleanup);
			Assert.AreEqual (3, mon3Cleanup);
		}
		#endregion
		#region IPropertyRegistrationCallback
		enum RegState {
			NotCalled,
			Registered,
			UnRegistered
		}
		class PropRegMon : IPropertyRegistrationCallback {
			Action<RegistrationContext> regAction;
			Action<RegistrationContext> unRegAction;
			public RegState State { get; set; }
			public int RegesterCalled { get; set; }
			public int UnregesterCalled { get; set; }
			public PropRegMon ()
			{
			}
			public PropRegMon (Action<RegistrationContext> regAction, Action<RegistrationContext> unRegAction)
			{
				this.regAction = regAction;
				this.unRegAction = unRegAction;
			}
			public void Register (RegistrationContext context)
			{
				State = RegState.Registered;
				RegesterCalled = ++RegesterCalled;
				if (regAction != null)
					regAction (context);
			}
			public void Unregister (RegistrationContext context)
			{
				State = RegState.UnRegistered;
				UnregesterCalled = ++UnregesterCalled;
				if (unRegAction != null)
					unRegAction (context);
			}
		}
		[Test]
		public void IPropertyRegistrationCallback ()
		{
			PropRegMon regMon = new PropRegMon (), regMon2 = new PropRegMon (), regMon3 = new PropRegMon ();

			var child1_1 = new NativeActivityRunner (null, (context) => {
				Assert.AreEqual (RegState.Registered, regMon.State);
				Assert.AreEqual (1, regMon.RegesterCalled);
				Assert.AreEqual (0, regMon.UnregesterCalled);
				Assert.AreEqual (regMon, context.Properties.Find ("regMon"));
			});
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				context.Properties.Add ("regMon", regMon);
				Assert.AreEqual (RegState.Registered, regMon.State);
				Assert.AreEqual (1, regMon.RegesterCalled);
				Assert.AreEqual (0, regMon.UnregesterCalled);
				Assert.AreEqual (regMon, context.Properties.Find ("regMon"));
				context.ScheduleActivity (child1_1);
			});
			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				//add and remove call reg / unreg interface members
				context.Properties.Add ("regMon3", regMon3);
				context.Properties.Remove ("regMon3");
				Assert.AreEqual (RegState.UnRegistered ,regMon3.State);

				context.Properties.Add ("regMon2", regMon2);
				context.ScheduleActivity (child1, callback);
			}, (context, compAI, callback) => {
				Assert.AreEqual (RegState.UnRegistered, regMon.State);
				Assert.AreEqual (1, regMon.RegesterCalled);
				Assert.AreEqual (1, regMon.UnregesterCalled);
				Assert.IsNull (context.Properties.Find ("regMon"));
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (RegState.UnRegistered, regMon.State);
			Assert.AreEqual (1, regMon.RegesterCalled);
			Assert.AreEqual (1, regMon.UnregesterCalled);
			Assert.AreEqual (RegState.UnRegistered, regMon2.State);
			Assert.AreEqual (1, regMon2.RegesterCalled);
			Assert.AreEqual (1, regMon2.UnregesterCalled);
			Assert.AreEqual (RegState.UnRegistered, regMon3.State);
			Assert.AreEqual (1, regMon3.RegesterCalled);
			Assert.AreEqual (1, regMon3.UnregesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_WFCancelled ()
		{
			//Note: this test doesnt explicitly check when unregister called on props
			PropRegMon regMon = new PropRegMon (), regMon2 = new PropRegMon (), regMon3 = new PropRegMon ();
			ActivityInstance ai1 = null, ai2 = null, ai3 = null;
			var child1_1_1 = new NativeActivityRunner (null, (context) => {
				Thread.Sleep (TimeSpan.FromSeconds (2));
			}, "child1_1_1 cancel");
			var child1_1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1_1);
			}, (context) => {
				context.Properties.Add ("regMon3", regMon3);
				ai3 = context.ScheduleActivity (child1_1_1);
			}, "child1_1 cancel");
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				context.Properties.Add ("regMon", regMon);
				context.CreateBookmark (); // so set to cancelled
				ai2 = context.ScheduleActivity (child1_1);
			}, "child1 cancel");
			child1.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				//add and remove call reg / unreg interface members
				context.Properties.Add ("regMon2", regMon2);
				ai1 = context.ScheduleActivity (child1);
			}, "wf cancel");

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);

			Assert.AreEqual (WFAppStatus.Cancelled, app.Status);
			Assert.AreEqual (ActivityInstanceState.Canceled, ai1.State);
			Assert.AreEqual (ActivityInstanceState.Closed, ai2.State);
			Assert.AreEqual (ActivityInstanceState.Closed, ai3.State);
			Assert.AreEqual (RegState.UnRegistered, regMon.State);
			Assert.AreEqual (1, regMon.RegesterCalled);
			Assert.AreEqual (1, regMon.UnregesterCalled);
			Assert.AreEqual (RegState.UnRegistered, regMon2.State);
			Assert.AreEqual (1, regMon2.RegesterCalled);
			Assert.AreEqual (1, regMon2.UnregesterCalled);
			Assert.AreEqual (RegState.UnRegistered, regMon3.State);
			Assert.AreEqual (1, regMon3.RegesterCalled);
			Assert.AreEqual (1, regMon3.UnregesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_WFCancelled_WhileCurrentFaults ()
		{
			//Note: this test doesnt explicitly check when unregister called on props
			PropRegMon regMon = new PropRegMon ();

			var child1 = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon", regMon);
				Thread.Sleep (TimeSpan.FromSeconds (2));
				throw new Exception ();
			}, "child1 cancel");
			child1.InduceIdle = true;

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.ScheduleActivity (child1, (ctx, ex, compAI) => {
					Console.WriteLine ("fault cb");
					ctx.HandleFault ();
				});
			}, (context) => {
				Console.WriteLine (String.Format ("{0}-R:{1}U:{2}", regMon.State, regMon.RegesterCalled, regMon.UnregesterCalled));
			});

			var app = new WFAppWrapper (wf);
			app.RunAndCancel (1);

			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual (String.Format ("UnRegistered-R:1U:1{0}fault cb{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void IPropertyRegistrationCallback_Faulted ()
		{
			int unregCounter = 0, regMonUnreg = 0, regMon2Unreg = 0;
			var regMon = new PropRegMon (null, (context) => { regMonUnreg = ++unregCounter; });
			var regMon2 = new PropRegMon (null, (context) => { regMon2Unreg = ++unregCounter; });
			Bookmark bookmark = null;
			var child1_1 = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon2", regMon2);
				context.ResumeBookmark (bookmark, "resumed");
				context.CreateBookmark ();
			});
			child1_1.InduceIdle = true;
			var child1 = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context, callback) => {
				context.Properties.Add ("regMon", regMon);
				bookmark = context.CreateBookmark (callback);
				context.ScheduleActivity (child1_1);
			}, (context, bk, value, callback) => {
				Console.WriteLine (value);
				throw new Exception ();
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context, callback) => {
				context.ScheduleActivity (child1, callback);
			}, (context, ex, ai) => {
				Assert.AreEqual (RegState.UnRegistered, regMon.State);
				Assert.AreEqual (1, regMon.RegesterCalled);
				Assert.AreEqual (1, regMon.UnregesterCalled);
				Assert.IsNull (context.Properties.Find ("name"));
				context.HandleFault ();
			});
			RunAndCompare (wf, "resumed" + Environment.NewLine);
			Assert.AreEqual (RegState.UnRegistered, regMon.State);
			Assert.AreEqual (1, regMon.RegesterCalled);
			Assert.AreEqual (1, regMon.UnregesterCalled);
			Assert.AreEqual (1, regMon2.RegesterCalled);
			Assert.AreEqual (1, regMon2.UnregesterCalled);
			Assert.AreEqual (1, regMon2Unreg);
			Assert.AreEqual (2, regMonUnreg);
		}
		[Test]
		public void IPropertyRegistrationCallback_Root_Faulted ()
		{
			var regMon = new PropRegMon ();
			Exception exception = null;
			var wf = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", regMon);
				throw new Exception ();
			});
			try {
				WorkflowInvoker.Invoke (wf);
			} catch (Exception ex) {
				exception = ex;
			}
			Assert.IsNotNull (exception);
			Assert.AreEqual (RegState.UnRegistered, regMon.State);
			Assert.AreEqual (1, regMon.RegesterCalled);
			Assert.AreEqual (1, regMon.UnregesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_ParallelActivityExecution ()
		{
			var regMon = new PropRegMon ();
			Bookmark bookmark = null;
			var child2 = new NativeActivityRunner (null, (context) => {
				Assert.IsNull (context.Properties.Find ("name"));
				context.ResumeBookmark (bookmark, "resumed");
			});
			var child1_1_1 = new NativeActWithBookmarkRunner (null, (context, callback) => {
				bookmark = context.CreateBookmark (callback);
			}, (context, bk, value, callback) => {
				Console.WriteLine (value);
			});
			var child1_1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1_1);
			}, (context) => {
				Assert.AreSame (regMon, context.Properties.Find ("name"));
				context.ScheduleActivity (child1_1_1);
			});
			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				context.Properties.Add ("name", regMon);
				Assert.AreEqual (RegState.Registered, regMon.State);
				Assert.AreEqual (1, regMon.RegesterCalled);
				Assert.AreEqual (0, regMon.UnregesterCalled);
				context.ScheduleActivity (child1_1);
			});
			child1.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, (context) => {
				context.ScheduleActivity (child2);
				context.ScheduleActivity (child1); //  runs first
			});
			RunAndCompare (wf, "resumed" + Environment.NewLine);
			Assert.AreEqual (RegState.UnRegistered, regMon.State);
			Assert.AreEqual (1, regMon.RegesterCalled);
			Assert.AreEqual (1, regMon.UnregesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_Register_RegistrationContext_Find_PropsInSameActivityAndParent ()
		{
			string sameAct = "sameAct", parentAct = "parentAct", sameActLater = "sameActLater";

			PropRegMon regMon = null;
			regMon = new PropRegMon ((context) => {
				Assert.AreSame (parentAct, context.FindProperty ("parentAct"));
				Assert.AreSame (sameAct, context.FindProperty ("sameAct"));
				Assert.IsNull (context.FindProperty ("regMon"));
				Assert.IsNull (context.FindProperty ("sameActLater"));
			}, null);

			var child1 = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("sameAct", sameAct);
				context.Properties.Add ("regMon", regMon);
				context.Properties.Add ("sameActLater", sameActLater);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.Properties.Add ("parentAct", parentAct);
				context.ScheduleActivity (child1);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (1, regMon.RegesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_Register_RegistrationContext_Find_DupeName_InSameActivityAndParent ()
		{
			string str1 = "str1", str2 = "str2";

			PropRegMon regMon = null;
			regMon = new PropRegMon ((context) => {
				Assert.AreSame (str2, context.FindProperty ("name"));
			}, null);

			var child1 = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", str2);
				context.Properties.Add ("regMon", regMon);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.Properties.Add ("name", str1);
				context.ScheduleActivity (child1);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (1, regMon.RegesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_Register_RegistrationContext_Find_DupeName_InAncestors ()
		{
			string str1 = "str1", str2 = "str2";

			PropRegMon regMon = null;
			regMon = new PropRegMon ((context) => {
				Assert.AreSame (str2, context.FindProperty ("name"));
			}, null);

			var child1_1 = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon", regMon);
			});

			var child1 = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1_1);
			}, (context) => {
				context.Properties.Add ("name", str2);
				context.ScheduleActivity (child1_1);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.Properties.Add ("name", str1);
				context.ScheduleActivity (child1);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (1, regMon.RegesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_Unregister_RegistrationContext_Find_PropsInSameActivityAndParent ()
		{
			string sameAct = "sameAct", parentAct = "parentAct", sameActLater = "sameActLater";
			string sameAct2 = "sameAct2", sameAct3 = "sameAct3", sameAct4 = "sameAct4";

			var regMon3 = new PropRegMon (null, (context) => {
				Assert.AreSame (parentAct, context.FindProperty ("parentAct"));
				Assert.IsNull (context.FindProperty ("regMon"));
				Assert.IsNull (context.FindProperty ("sameAct"));
				Assert.IsNull (context.FindProperty ("sameAct2"));
				Assert.IsNull (context.FindProperty ("regMon2"));
				Assert.IsNull (context.FindProperty ("sameAct3"));
				Assert.IsNull (context.FindProperty ("sameAct4"));
				Assert.IsNull (context.FindProperty ("sameActLater"));
				Assert.IsNull (context.FindProperty ("regMon3"));
			});
			var regMon2 = new PropRegMon (null, (context) => {
				Assert.AreSame (parentAct, context.FindProperty ("parentAct"));
				Assert.IsNull (context.FindProperty ("regMon"));
				Assert.IsNull (context.FindProperty ("sameAct"));
				Assert.IsNull (context.FindProperty ("sameAct2"));
				Assert.IsNull (context.FindProperty ("regMon2"));
				Assert.AreSame (sameAct3, context.FindProperty ("sameAct3"));
				Assert.AreSame (sameAct4, context.FindProperty ("sameAct4"));
				Assert.AreSame (sameActLater, context.FindProperty ("sameActLater"));
				Assert.AreSame (regMon3, context.FindProperty ("regMon3"));
			});
			var regMon = new PropRegMon (null, (context) => {
				Assert.AreSame (parentAct, context.FindProperty ("parentAct"));
				Assert.IsNull (context.FindProperty ("regMon"));
				Assert.AreSame (sameAct, context.FindProperty ("sameAct"));
				Assert.AreSame (sameAct2, context.FindProperty ("sameAct2"));
				Assert.AreSame (regMon2, context.FindProperty ("regMon2"));
				Assert.AreSame (sameAct3, context.FindProperty ("sameAct3"));
				Assert.AreSame (sameAct4, context.FindProperty ("sameAct4"));
				Assert.AreSame (sameActLater, context.FindProperty ("sameActLater"));
				Assert.AreSame (regMon3, context.FindProperty ("regMon3"));
			});
			var child1 = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon", regMon);
				context.Properties.Add ("sameAct", sameAct);
				context.Properties.Add ("sameAct2", sameAct2);
				context.Properties.Add ("regMon2", regMon2);
				context.Properties.Add ("sameAct3", sameAct3);
				context.Properties.Add ("sameAct4", sameAct4);
				context.Properties.Add ("sameActLater", sameActLater);
				context.Properties.Add ("regMon3", regMon3);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
			}, (context) => {
				context.Properties.Add ("parentAct", parentAct);
				context.ScheduleActivity (child1);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (1, regMon.UnregesterCalled);
			Assert.AreEqual (1, regMon2.UnregesterCalled);
			Assert.AreEqual (1, regMon3.UnregesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_Register_Throws_HandledByFaultCB ()
		{
			Exception throws = new Exception (), caught = null;
			var regMon = new PropRegMon ((context) => {
				throw throws;
			}, null);
			var child = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", regMon);
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
				caught = ex;
				context.HandleFault ();
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreSame (throws, caught);
		}
		[Test]
		public void IPropertyRegistrationCallback_Unregister_Throws_HandledByFaultCB_NoOtherUnregsCalled ()
		{
			//FIXME: change test to use WorkflowApplication / WorkflowInstance
			Exception throw1 = new Exception (), caught = null;
			var regMon = new PropRegMon (null, (context) => {
				throw throw1;
			});

			var child = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon", regMon);
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
				caught = ex;
				context.HandleFault ();
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreSame (throw1, caught);
		}
		[Test]
		public void IPropertyRegistrationCallback_Unregister_Throws_NoOtherPropsHaveUnRegCalled ()
		{
			//FIXME: change test to use WorkflowApplication / WorkflowInstance
			var regMon = new PropRegMon (null, (context) => {
				throw new Exception ();
			});
			var regMon2 = new PropRegMon (null, null);

			var child = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon", regMon);
				context.Properties.Add ("regMon2", regMon2);
			});
			var wf1 = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				throw new Exception ();
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (wf1);
			}, (context, callback) => {
				context.ScheduleActivity (wf1, callback);
			}, (context, ex, ai) => {
				context.HandleFault ();
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (1, regMon2.RegesterCalled);
			Assert.AreEqual (0, regMon2.UnregesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_Unregister_Throws_AfterActivityThrew_Ignored ()
		{

			Exception propThrow = new Exception (), actThrow = new Exception (), caught = null;
			var regMon = new PropRegMon (null, (context) => {
				throw propThrow;
			});
			var regMon2 = new PropRegMon (null, null);

			var child = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon", regMon);
				context.Properties.Add ("regMon2", regMon2);
				throw actThrow;
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
				caught = ex;
				context.HandleFault ();
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreSame (actThrow, caught);
			Assert.AreEqual (1, regMon.RegesterCalled);
			Assert.AreEqual (1, regMon.UnregesterCalled);
			Assert.AreEqual (1, regMon2.RegesterCalled);
			Assert.AreEqual (1, regMon2.UnregesterCalled);
		}
		[Test]
		public void IPropertyRegistrationCallback_Unregister_Throws_WhenAncestorAlreadyThrown_Ignored ()
		{
			Bookmark bookmark = null;
			Exception propThrow = new Exception (), actThrow = new Exception (), caught = null;
			var regMon = new PropRegMon (null, (context) => {
				throw propThrow;
			});
			var regMon2 = new PropRegMon (null, null);

			var deeper = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("regMon", regMon);
				context.Properties.Add ("regMon2", regMon2);
				context.CreateBookmark ();
				context.ResumeBookmark (bookmark, "resumed");
			});
			deeper.InduceIdle = true;
			var child = new NativeActWithBookmarkRunner ((metadata) => {
				metadata.AddChild (deeper);
			}, (context, callback) => {
				context.ScheduleActivity (deeper);
				bookmark = context.CreateBookmark (callback);
			}, (context, bk, value, callback) => {
				Console.WriteLine (value);
				throw actThrow;
			});
			var wf = new NativeActWithFaultCBRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context, callback) => {
				context.ScheduleActivity (child, callback);
			}, (context, ex, ai) => {
				Assert.AreEqual (ActivityInstanceState.Faulted, ai.State);
				caught = ex;
			});
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual ("resumed" + Environment.NewLine, app.ConsoleOut);
			Assert.AreSame (child, app.ExceptionSource);
			Assert.AreSame (actThrow, app.UnhandledException);
			Assert.AreSame (actThrow, caught);
			Assert.AreEqual (1, regMon.RegesterCalled);
			Assert.AreEqual (1, regMon.UnregesterCalled);
			Assert.AreEqual (1, regMon2.RegesterCalled);
			Assert.AreEqual (1, regMon2.UnregesterCalled);
		}
		#endregion
		[Test]
		public void RegistrationContext_FindProperty_NameEmptyEx ()
		{
			Exception exception = null;
			var regMon = new PropRegMon ((context) => {
				try {
					context.FindProperty (String.Empty);
				} catch (Exception ex) {
					exception = ex;
				}
			}, null);
			var wf = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", regMon);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (ArgumentException), exception);
		}
		[Test]
		public void RegistrationContext_FindProperty_NameNullEx ()
		{
			Exception exception = null;
			var regMon = new PropRegMon ((context) => {
				try {
					context.FindProperty (null);
				} catch (Exception ex) {
					exception = ex;
				}
			}, null);
			var wf = new NativeActivityRunner (null, (context) => {
				context.Properties.Add ("name", regMon);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (ArgumentException), exception);
		}
	}
}

