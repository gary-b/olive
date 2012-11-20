using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	[TestFixture]
	class WorkflowInvokerTest {
		WriteLine writeline;

		class ActivityRunner : Activity {
			new public int CacheId {
				get { return base.CacheId; }
			}
			public ActivityRunner (Func<Activity> implementation)
			{
				this.Implementation = implementation;
			}
		}

		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}

		[TestFixtureSetUp]		
		public void TestFixtureSetUp ()
		{
			writeline = new WriteLine () {Text = "Hello\nWorld"};
		}

		[Test]
		public void Ctor ()
		{
			var wi = new WorkflowInvoker (writeline);
			Assert.IsNotNull (wi.Extensions);
		}

		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor__NullEx ()
		{
			var wi = new WorkflowInvoker (null);
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

		class TrackIdWrite : CodeActivity {
			protected override void Execute (CodeActivityContext context)
			{
				Console.WriteLine ("CacheId: {0} ActivityInstanceId: {1} Id: {2}",
						this.CacheId, context.ActivityInstanceId, this.Id);

			}
		}

		[Test]
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
			// FIXME: cacheId changes when mutliple tests run together <- investigate
			string expected = String.Format ("CacheId: {1} ActivityInstanceId: 3 Id: 1.8{0}" +
			                                 "CacheId: {1} ActivityInstanceId: 5 Id: 1.7{0}" +
			                                 "CacheId: {1} ActivityInstanceId: 7 Id: 1.5{0}", 
			                                 Environment.NewLine, wf.CacheId);

			Assert.AreEqual (expected, sw.ToString ());
		}

		[Test]
		public void Runtime_ActivityId ()
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
			
			//presumably a Sequence would return 1.1, 1.2, 1.3
		}
		/*
		[Test]
		public void Runtime_CacheId ()
		{
			// covered by Runtime_CacheId_ActivityInstanceId_ActivityId
			var childActivity = new ActivityRunner (null);
			var activity = new ActivityRunner (() => childActivity);
			Assert.AreEqual (0, activity.CacheId);
			Assert.AreEqual (0, childActivity.CacheId);

			WorkflowInvoker.Invoke (activity);
			Assert.AreEqual (childActivity.CacheId, activity.CacheId); //.NET = both CacheIds seem to be equal but value changes each run
			// could possibly test that cacheId is non 0 as well?
		}
		*/
		[Test]
		public void Increment1Test ()
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
		public void Increment2Test ()
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

		class Increment3Mock : NativeActivity {
			public Activity writeLineChild { get; set;}
			public Activity writeLineImpChild { get; set;}

			public Increment3Mock ()
			{
				writeLineChild = new WriteLine {
					Text = new InArgument<string> ("ChildWrite")
				};
				writeLineImpChild = new WriteLine {
					Text = new InArgument<string> ("ImpChildWrite")
				};
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddChild (writeLineChild);
				metadata.AddImplementationChild (writeLineImpChild);
			}
			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleActivity (writeLineChild);
				context.ScheduleActivity (writeLineImpChild);
				// context.ScheduleActivity (writeLineImpChild);
				// FIXME: scheduling activities multiple times is allowed, add test
			}
		}

		[Test]
		public void Increment3Test ()
		{
			var wf = new Increment3Mock ();
			RunAndCompare (wf, "ImpChildWrite" + Environment.NewLine +
					"ChildWrite" + Environment.NewLine);
			// test Ids
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", wf.writeLineChild.Id);
			Assert.AreEqual ("1.1", wf.writeLineImpChild.Id);
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
				base.CacheMetadata (metadata);
			}
		}
		[Test]
		public void ImportedChildTest ()
		{
			var wf = new ImportsActivity ();
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine + 
			                 "Hello\nWorld" + Environment.NewLine);
		}

		[Test] //FIXME: ExpectedException (typeof (InvalidWorkflowInstance)) <- study error message
		public void ReuseInstancesTest ()
		{
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
			throw new NotImplementedException (); // throw NIE so doesnt look like passed
		}

		[Test] //FIXME: ExpectedException (typeof (InvalidWorkflowInstance)) <- study error message
		public void ReuseInstancesDifLevelsTest ()
		{
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
			throw new NotImplementedException (); // throw NIE so doesnt look like passed
		}

		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Invoke_workflow_NullEx ()
		{
			WorkflowInvoker.Invoke ((Activity) null);
		}

		[Test]
		public void Invoke_workflow ()
		{
			throw new NotImplementedException ();
		}

		public void InvokeCompleted ()
		{
			throw new NotImplementedException ();
		}
		
		public void Extensions ()
		{ 
			throw new NotImplementedException ();
		}

		public void BeginInvoke_callback_state ()
		{
			throw new NotImplementedException ();
		}
		public void BeginInvoke_inputs_callback_state ()
		{
			throw new NotImplementedException ();
		}
		public void BeginInvoke_timeout_callback_state ()
		{
			throw new NotImplementedException ();
		}
		public void BeginInvoke_inputs_timeout_callback_state ()
		{
			throw new NotImplementedException ();
		}
		public void EndInvoke_result ()
		{
			throw new NotImplementedException ();
		}

		public void InvokeT_workflow ()
		{
			throw new NotImplementedException ();
		}
		public void Invoke_inputs ()
		{
			throw new NotImplementedException ();
		}
		public void Invoke_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void Invoke_workflow_inputs ()
		{
			throw new NotImplementedException ();
		}
		public void Invoke_workflow_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeT_workflow_inputs ()
		{
			throw new NotImplementedException ();
		}
		public void Invoke_inputs_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void Invoke_workflow_inputs_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeT_workflow_inputs_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeT_workflow_inputs_additionalOutputs_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync_userState ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync_inputs ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync_inputs_userState ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync_inputs_timeout ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync_timeout_userState ()
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync_inputs_timeout_userState ()
		{
			throw new NotImplementedException ();
		}
	}
}
