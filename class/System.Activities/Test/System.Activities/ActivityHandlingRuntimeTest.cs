using System;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Linq;
using System.Activities.Expressions;
using System.Activities;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ActivityHandlingRuntimeTest : WFTestHelper {
		// These were early and in some cases exploratory tests.
		class TrackCMWrite : CodeActivity {
			public InArgument<string> Text { get; set; }
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				base.CacheMetadata (metadata);
				Console.WriteLine (this.GetType ().Name + "'s CacheMetaData was Called");
			}

			protected override void Execute (CodeActivityContext context)
			{
				Console.WriteLine (Text.Get (context));
			}
		}
		[Test]
		[Ignore ("If Activity")]
		public void Runtime_CacheMetaDataCalledFirst ()
		{
			// shows CacheMetaData called for all activities as first step even the activity assigned to the Else
			// argument of IfElse which never will be executed

			// for Activity
			Func<Activity> implementation = () => {
				return new Sequence {
					Activities = {
						new TrackCMWrite {
							Text= new InArgument<string> ("SequenceWrite")
						},
						new Sequence {
							Activities = {
								new TrackCMWrite {
									Text= new InArgument<string> ("SubSequenceWrite")
								},									
							}
						},
						new If {
							Condition = new InArgument<bool> (true),
							Then = new TrackCMWrite {
								Text= new InArgument<string> ("If_TrueWrite")
							},
							Else = new TrackCMWrite {
								Text= new InArgument<string> ("If_FalseWrite")
							},
						},
						new ActivityRunner (() => new TrackCMWrite {
							Text = new InArgument<string> ("ActivityImplementationWrite")
						})
					}
				};
			};

			string expected = String.Format ("TrackCMWrite's CacheMetaData was Called{0}" +
							 "TrackCMWrite's CacheMetaData was Called{0}" +
							 "TrackCMWrite's CacheMetaData was Called{0}" +
							 "TrackCMWrite's CacheMetaData was Called{0}" +
							 "TrackCMWrite's CacheMetaData was Called{0}" +
							 "SequenceWrite{0}" +
							 "SubSequenceWrite{0}" +
							 "If_TrueWrite{0}" +
							 "ActivityImplementationWrite{0}", Environment.NewLine);

			var wf = new ActivityRunner (implementation);
			RunAndCompare (wf, expected);
		}
		[Test]
		[Ignore ("If Activity")]
		public void Runtime_CacheId_ActivityInstanceId_ActivityId ()
		{
			// cant use RunAndCompare as need access to wf state post execution
			var sw = new StringWriter ();
			Console.SetOut (sw);
			Func<Activity> implementation = () => {
				return new Sequence {
					Activities = {
						new TrackIdWrite (),
						new Sequence {
							Activities = {
								new TrackIdWrite (),									
							}
						},
						new If {
							Condition = new InArgument<bool> (true),
							Then = new TrackIdWrite (),
							Else = new TrackIdWrite (),
						},
					}
				};
			};
			var wf = new ActivityRunner (implementation);
			WorkflowInvoker.Invoke (wf);
			// FIXME: cacheId changes when multiple tests run together <- investigate
			string expected = String.Format ("CacheId: {1} ActivityInstanceId: 3 Id: 1.8{0}" +
							 "CacheId: {1} ActivityInstanceId: 5 Id: 1.7{0}" +
							 "CacheId: {1} ActivityInstanceId: 7 Id: 1.5{0}", 
							 Environment.NewLine, wf.CacheId);

			Assert.AreEqual (expected, sw.ToString ());
		}
		Activity GetMetadataWriter (string msg)
		{
			return new NativeActivityRunner ((metadata)=> { 
				Console.WriteLine (msg); 
			}, null);
		}
		[Test]
		public void CacheMetadata_Children_OrderCalled_ActivityIdGeneration ()
		{
			//children executed in lifo manner
			//but implementation children executed first
			int orderCounter = 0;
			//FIXME: see notes in CacheMetadata_OrderCalled test
			var pubChild1PubChild1 = GetMetadataWriter ("pubChild1PubChild1");
			var pubChild1PubChild2 = GetMetadataWriter ("pubChild1PubChild2");
			var pubChild1ImpChild1 = GetMetadataWriter ("pubChild1ImpChild1");
			var pubChild1ImpChild2 = GetMetadataWriter ("pubChild1ImpChild2");
			var pubChild2PubChild1 = GetMetadataWriter ("pubChild2PubChild1");
			var pubChild2PubChild2 = GetMetadataWriter ("pubChild2PubChild2");
			var pubChild2ImpChild1 = GetMetadataWriter ("pubChild2ImpChild1");
			var pubChild2ImpChild2 = GetMetadataWriter ("pubChild2ImpChild2");
			var impChild1PubChild1 = GetMetadataWriter ("impChild1PubChild1");
			var impChild1PubChild2 = GetMetadataWriter ("impChild1PubChild2");
			var impChild1ImpChild1 = GetMetadataWriter ("impChild1ImpChild1");
			var impChild1ImpChild2 = GetMetadataWriter ("impChild1ImpChild2");
			var impChild2PubChild1 = GetMetadataWriter ("impChild2PubChild1");
			var impChild2PubChild2 = GetMetadataWriter ("impChild2PubChild2");
			var impChild2ImpChild1 = GetMetadataWriter ("impChild2ImpChild1");
			var impChild2ImpChild2 = GetMetadataWriter ("impChild2ImpChild2");

			var pubChild2 = new NativeActivityRunner ((metadata) => {
				Console.WriteLine ("pubChild2");
				metadata.AddImplementationChild (pubChild2ImpChild2);
				metadata.AddChild (pubChild2PubChild2);
				metadata.AddImplementationChild (pubChild2ImpChild1);
				metadata.AddChild (pubChild2PubChild1);
			}, (context) => {
				context.ScheduleActivity (pubChild2ImpChild2);
				context.ScheduleActivity (pubChild2PubChild2);
				context.ScheduleActivity (pubChild2ImpChild1);
				context.ScheduleActivity (pubChild2PubChild1);
			});

			var impChild2 = new NativeActivityRunner (metadata => {
				Console.WriteLine ("impChild2");
				metadata.AddImplementationChild (impChild2ImpChild2);
				metadata.AddChild (impChild2PubChild2);
				metadata.AddImplementationChild (impChild2ImpChild1);
				metadata.AddChild (impChild2PubChild1);
			}, (context) => {
				context.ScheduleActivity (impChild2ImpChild2);
				context.ScheduleActivity (impChild2PubChild2);
				context.ScheduleActivity (impChild2ImpChild1);
				context.ScheduleActivity (impChild2PubChild1);
			});

			var pubChild1 = new NativeActivityRunner ((metadata) => {
				Console.WriteLine ("pubChild1");
				metadata.AddImplementationChild (pubChild1ImpChild2);
				metadata.AddChild (pubChild1PubChild2);
				metadata.AddImplementationChild (pubChild1ImpChild1);
				metadata.AddChild (pubChild1PubChild1);
			}, (context) => {
				context.ScheduleActivity (pubChild1ImpChild2);
				context.ScheduleActivity (pubChild1PubChild2);
				context.ScheduleActivity (pubChild1ImpChild1);
				context.ScheduleActivity (pubChild1PubChild1);
			});

			var impChild1 = new NativeActivityRunner (metadata => {
				Console.WriteLine ("impChild1");
				metadata.AddImplementationChild (impChild1ImpChild2);
				metadata.AddChild (impChild1PubChild2);
				metadata.AddImplementationChild (impChild1ImpChild1);
				metadata.AddChild (impChild1PubChild1);
			}, (context) => {
				context.ScheduleActivity (impChild1ImpChild2);
				context.ScheduleActivity (impChild1PubChild2);
				context.ScheduleActivity (impChild1ImpChild1);
				context.ScheduleActivity (impChild1PubChild1);
			});

			var wf = new NativeActivityRunner (metadata => {
				Console.WriteLine ("wf");
				metadata.AddImplementationChild (impChild2);
				metadata.AddChild (pubChild2);
				metadata.AddImplementationChild (impChild1);
				metadata.AddChild (pubChild1);
			}, (context) => {
				context.ScheduleActivity (impChild2);
				context.ScheduleActivity (pubChild2);
				context.ScheduleActivity (impChild1);
				context.ScheduleActivity (pubChild1);
			});

			var app = new WFAppWrapper (wf);
			app.Run ();
			//Test Order Called
			var split = app.ConsoleOut.Split (new string [] { Environment.NewLine }, StringSplitOptions.None);
			//remove trailing empty string
			var actualOrder = new string [split.Length - 1];
			for (int i = 0; i < split.Length - 1; i++)
				actualOrder [i] = split [i];
			var expected = new string [] {
				"wf",
				"impChild1", "impChild1ImpChild1", "impChild1ImpChild2", "impChild1PubChild1", "impChild1PubChild2",
				"impChild2", "impChild2ImpChild1", "impChild2ImpChild2", "impChild2PubChild1", "impChild2PubChild2",
				"pubChild1", "pubChild1ImpChild1", "pubChild1ImpChild2", "pubChild1PubChild1", "pubChild1PubChild2",
				"pubChild2", "pubChild2ImpChild1", "pubChild2ImpChild2", "pubChild2PubChild1", "pubChild2PubChild2",
				};
			Assert.AreEqual (expected, actualOrder);

			// Test Activity Ids Generated
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", pubChild1.Id);
			Assert.AreEqual ("3", pubChild1PubChild1.Id);
			Assert.AreEqual ("4", pubChild1PubChild2.Id);
			Assert.AreEqual ("2.1", pubChild1ImpChild1.Id);
			Assert.AreEqual ("2.2", pubChild1ImpChild2.Id);
			Assert.AreEqual ("1.1", impChild1.Id);
			Assert.AreEqual ("1.2", impChild1PubChild1.Id);
			Assert.AreEqual ("1.3", impChild1PubChild2.Id);
			Assert.AreEqual ("1.1.1", impChild1ImpChild1.Id);
			Assert.AreEqual ("1.1.2", impChild1ImpChild2.Id);
			Assert.AreEqual ("5", pubChild2.Id);
			Assert.AreEqual ("6", pubChild2PubChild1.Id);
			Assert.AreEqual ("7", pubChild2PubChild2.Id);
			Assert.AreEqual ("5.1", pubChild2ImpChild1.Id);
			Assert.AreEqual ("5.2", pubChild2ImpChild2.Id);
			Assert.AreEqual ("1.4", impChild2.Id);
			Assert.AreEqual ("1.5", impChild2PubChild1.Id);
			Assert.AreEqual ("1.6", impChild2PubChild2.Id);
			Assert.AreEqual ("1.4.1", impChild2ImpChild1.Id);
			Assert.AreEqual ("1.4.2", impChild2ImpChild2.Id);
		}
		[Test]
		[Ignore ("Argument Evaluation Order")]
		public void ArgExpression_AndVarDefault_OrderExecuted ()
		{
			//seems Argument.Expressions are scheduled before variable.Defaults
			//Variable defaults executed in reverse order they are added to metadata,
			//since LIFO, means they are scheduled in order added to metadata
			var wf = new VarDefAndArgEvalOrder ();
			wf.InArg1 = new InArgument<string> (new VarDefAndArgEvalOrder.GetString ("InArg1"));
			wf.PubVar1 = new Variable<string> ();
			wf.PubVar1.Default = new VarDefAndArgEvalOrder.GetString ("PubVar1");
			wf.InOutArg1 = new InOutArgument<string> (new VarDefAndArgEvalOrder.GetLocationString ("InOutArg1"));
			wf.ImpVar1 = new Variable<string> ();
			wf.ImpVar1.Default = new VarDefAndArgEvalOrder.GetString ("ImpVar1");
			wf.OutArg1 = new OutArgument<string> (new VarDefAndArgEvalOrder.GetLocationString ("OutArg1"));
			wf.PubVar2 = new Variable<string> ();
			wf.PubVar2.Default = new VarDefAndArgEvalOrder.GetString ("PubVar2");
			wf.InArg2 = new InArgument<string> (new VarDefAndArgEvalOrder.GetString ("InArg2"));
			wf.ImpVar2 = new Variable<string> ();
			wf.ImpVar2.Default = new VarDefAndArgEvalOrder.GetString ("ImpVar2");
			wf.InOutArg2 = new InOutArgument<string> (new VarDefAndArgEvalOrder.GetLocationString ("InOutArg2"));
			wf.OutArg2 = new OutArgument<string> (new VarDefAndArgEvalOrder.GetLocationString ("OutArg2"));
			var ap = new WFAppWrapper (wf);
			ap.Run ();
			Assert.AreEqual (String.Format (
				"InOutArg2{0}InOutArg1{0}OutArg2{0}InArg1{0}InArg2{0}OutArg1{0}" +
				"PubVar2{0}PubVar1{0}ImpVar2{0}ImpVar1{0}ExEvExecute{0}", Environment.NewLine),
			        ap.ConsoleOut);
		}
		[Test]
		public void Increment1_SingleActivity ()
		{
			var textValue = new Literal<string> ("Hello World");
			var inArg = new InArgument<string> (textValue);
			var writeLine = new WriteLine ();
			writeLine.Text = inArg;
			RunAndCompare (writeLine, "Hello World" + Environment.NewLine);
			// test Ids
			Assert.AreEqual ("1", writeLine.Id);
			Assert.AreEqual ("2", textValue.Id);
		}
		[Test]
		public void Increment2_MultiActivity ()
		{
			var textValue = new Literal<string> ("Hello World");
			var inArg = new InArgument<string> (textValue);
			var writeLine = new WriteLine ();
			writeLine.Text = inArg;

			var wf = new ActivityRunner (() => {return writeLine;});

			RunAndCompare (wf, "Hello World" + Environment.NewLine);
			// test Ids
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("1.1", writeLine.Id);
			Assert.AreEqual ("1.2", textValue.Id);
		}
		[Test]
		public void Increment3_NativeActivity_ImpAndPubChildScheduling ()
		{
			var writeLineChild = new WriteLine {
				Text = new InArgument<string> ("ChildWrite")
			};
			var writeLineImpChild = new WriteLine {
				Text = new InArgument<string> ("ImpChildWrite")
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLineChild);
				metadata.AddImplementationChild (writeLineImpChild);
			}, (context) => {
				context.ScheduleActivity (writeLineChild);
				context.ScheduleActivity (writeLineImpChild);
			});

			RunAndCompare (wf, "ImpChildWrite" + Environment.NewLine +
				       "ChildWrite" + Environment.NewLine);
			// test Ids
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", writeLineChild.Id);
			Assert.AreEqual ("1.1", writeLineImpChild.Id);
		}
		class ImportsActivity : Activity {
			public Activity MyActivity { get; set; }
			public ImportsActivity ()
			{
				MyActivity = new WriteLine {
					Text = new InArgument<string> ("Hello\nWorld")
				};
				Implementation = () => {
					return new Sequence {
						Activities = {
							MyActivity,
							MyActivity
						}
					};
				};
			}
			protected override void CacheMetadata (ActivityMetadata metadata)
			{
				metadata.AddImportedChild (MyActivity);
			}
		}
		[Test]
		[Ignore ("ImportedChildren")]
		public void ImportedChild ()
		{
			var wf = new ImportsActivity ();
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine + 
				       "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Activity / Scheduling Exceptions / Validation")]
		public void ReuseInstancesEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 * 'ActivityRunner': The private implementation of activity '1: ActivityRunner' has the following validation error:   
			 * The activity 'Sequence' cannot reference activity 'TrackIdWrite' because activity 'TrackIdWrite' is already referenced 
			 * elsewhere in the workflow and that reference is not visible to activity 'Sequence'.  In order for activity 
			 * 'TrackIdWrite' to be visible to activity 'Sequence', it would have to be a child or imported child (but not 
			 * an implementation child) of activity 'ActivityRunner'.  Activity 'TrackIdWrite' is originally referenced by 
			 * activity 'Sequence' and activity 'Sequence' is in the implementation of activity 'ActivityRunner'.
			 */ 
			Func<Activity> implementation = () => {
				var trackIdWrite = new TrackIdWrite ();
				return new Sequence {
					Activities = {
						trackIdWrite,
						trackIdWrite
					}
				};
			};
			WorkflowInvoker.Invoke (new ActivityRunner (implementation));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Activity / Scheduling Exceptions / Validation")]
		public void ReuseInstancesDifLevelsEx ()
		{
			/*  System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 * 'ActivityRunner': The private implementation of activity '1: ActivityRunner' has the following validation error:   
			 *  The activity 'Sequence' cannot reference activity 'TrackIdWrite' because activity 'TrackIdWrite' is already referenced 
			 *  elsewhere in the workflow and that reference is not visible to activity 'Sequence'.  In order for activity 
			 *  'TrackIdWrite' to be visible to activity 'Sequence', it would have to be a child or imported child (but not 
			 *  an implementation child) of activity 'ActivityRunner'.  Activity 'TrackIdWrite' is originally referenced by 
			 *  activity 'Sequence' and activity 'Sequence' is in the implementation of activity 'ActivityRunner'.
			 */ 
			Func<Activity> implementation = () => {
				var trackIdWrite = new TrackIdWrite ();
				return new Sequence {
					Activities = {
						trackIdWrite,
						new Sequence {
							Activities = {
								trackIdWrite
							}
						}
					}
				};
			};
			WorkflowInvoker.Invoke (new ActivityRunner (implementation));
		}
		[Test]
		public void CannotScheduleGrandchild ()
		{
			//System.InvalidOperationException : An Activity can only schedule its direct children. Activity 'NativeActivityRunner' is attempting to schedule 'WriteLine' which is a child of activity 'NativeActivityRunner'.
			Exception exception = null;
			var grandChild = new WriteLine ();
			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (grandChild);
			}, null);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				try {
					context.ScheduleActivity (grandChild);
				} catch (Exception ex) {
					exception = ex;
				}
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), exception);
		}
		[Test]
		public void CannotScheduleSibling ()
		{
			//System.InvalidOperationException : An Activity can only schedule its direct children. Activity 'NativeActivityRunner' is attempting to schedule 'WriteLine' which is a child of activity 'NativeActivityRunner'.
			Exception exception = null;
			var child2 = new WriteLine ();
			var child1 = new NativeActivityRunner (null, (context) => {
				try {
					context.ScheduleActivity (child2);
				} catch (Exception ex) {
					exception = ex;
				}
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child1);
				metadata.AddChild (child2);
			}, (context) => {
				context.ScheduleActivity (child1);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), exception);
		}
		[Test]
		[Ignore ("Doesnt return .NET's odd exception type choice")]
		public void CannotScheduleParent ()
		{
			Exception exception = null;

			var child = new NativeActivityRunner (null, null);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
			child.ExecuteAction = (context) => {
				try {
					Assert.IsNotNull (wf);
					context.ScheduleActivity (wf);
				} catch (Exception ex) {
					exception = ex;
				}
			};
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (NullReferenceException), exception);
		}
		[Test]
		[Ignore ("Doesnt return .NET's odd exception type choice")]
		public void CannotScheduleSelf ()
		{
			Exception exception = null;

			var wf = new NativeActivityRunner (null, null);
			wf.ExecuteAction = (context) => {
				try {
					Assert.IsNotNull (wf);
					context.ScheduleActivity (wf);
				} catch (Exception ex) {
					exception = ex;
				}
			};
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (NullReferenceException), exception);
		}
	}
}

