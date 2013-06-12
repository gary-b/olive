using System;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;
using System.Activities;

namespace Tests.System.Activities {
	[TestFixture]
	public class ActivityHandlingRuntimeTest : WFTest {
		// These were early and in some cases exploratory tests.
		class ActivityRunner : Activity {
			new public int CacheId {
				get { return base.CacheId; }
			}
			public ActivityRunner (Func<Activity> implementation)
			{
				this.Implementation = implementation;
			}
		}
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
		[Test]
		public void Activity_ActivityIdGeneration ()
		{
			var thirdLevelActivity = new ActivityRunner (null);
			var secondLevelActivity = new ActivityRunner (() => thirdLevelActivity);
			var firstLevelActivity = new ActivityRunner (() => secondLevelActivity);

			Assert.IsNull (firstLevelActivity.Id);
			Assert.IsNull (secondLevelActivity.Id);
			Assert.IsNull (thirdLevelActivity.Id);

			WorkflowInvoker.Invoke (firstLevelActivity);
			Assert.AreEqual ("1", firstLevelActivity.Id);
			Assert.AreEqual ("1.1", secondLevelActivity.Id);
			Assert.AreEqual ("1.1.1", thirdLevelActivity.Id);
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

			var wf = new NativeRunnerMock ((metadata) => {
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
	}
}

