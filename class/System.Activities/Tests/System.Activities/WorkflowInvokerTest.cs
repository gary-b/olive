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

		#region Increment4 Exception Tests

		class WriteLineHolder : NativeActivity {
			public WriteLine ImplementationWriteLine { get; set; }
			
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddImplementationChild (ImplementationWriteLine);
			}
			
			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleActivity (ImplementationWriteLine);
			}
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Increment4_PubVarAccessFromExecuteEx ()
		{
			/*
			System.InvalidOperationException : Activity '1: NativeRunnerMock' cannot access this variable because 
			it is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own 
			implementation variables.
			 */
			var PublicVariable = new Variable<string> ("", "HelloPublic");

			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddVariable (PublicVariable);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.GetValue (PublicVariable); // should raise error
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Increment4_PubVarAccessFromPubChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '3: NativeRunnerMock' cannot access this variable because
			 * it is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own 
			 * implementation variables.
			 */
			var PublicVariable = new Variable<string> ("", "HelloPublic");

			Action<NativeActivityMetadata> cacheMetadataChild = (metadata) => {
			};
			
			Action<NativeActivityContext> executeChild = (context) => {
				context.GetValue (PublicVariable); // should raise error
			};

			var PublicChild = new NativeRunnerMock (cacheMetadataChild, executeChild);

			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicChild);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				context.ScheduleActivity (PublicChild);
			};

			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Increment4_ImpVarAccessFromPubChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '2: NativeRunnerMock' cannot access this variable because it is 
			 * declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own implementation variables.
			 */
			var ImplementationVariable = new Variable<string> ("", "HelloImplementationVariable");
			
			Action<NativeActivityMetadata> cacheMetadataChild = (metadata) => {
			};
			
			Action<NativeActivityContext> executeChild = (context) => {
				context.GetValue (ImplementationVariable); // should raise error
			};
			
			var PublicChild = new NativeRunnerMock (cacheMetadataChild, executeChild);
			
			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicChild);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				context.ScheduleActivity (PublicChild);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Increment4_PubVarAccessFromImpChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '1.1: NativeRunnerMock' cannot access this variable because 
			 * it is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own 
			 * implementation variables.
			 */
			var PublicVariable = new Variable<string> ("", "HelloPublic");
			
			Action<NativeActivityMetadata> cacheMetadataChild = (metadata) => {
			};
			
			Action<NativeActivityContext> executeChild = (context) => {
				context.GetValue (PublicVariable); // should raise error
			};
			
			var ImplementationChild = new NativeRunnerMock (cacheMetadataChild, executeChild);
			
			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationChild);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				context.ScheduleActivity (ImplementationChild);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Increment4_ImpVarAccessFromImpChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '1.2: NativeRunnerMock' cannot access this variable because it 
			 * is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own implementation
			 * variables.
			 */
			var ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			
			Action<NativeActivityMetadata> cacheMetadataChild = (metadata) => {
			};
			
			Action<NativeActivityContext> executeChild = (context) => {
				context.GetValue (ImplementationVariable); // should raise error
			};
			
			var ImplementationChild = new NativeRunnerMock (cacheMetadataChild, executeChild);
			
			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationChild);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				context.ScheduleActivity (ImplementationChild);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_ImpVarAccessFromPubChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another 
			location reference with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var PublicWrite = new WriteLine {
				Text = ImplementationVariable
			};

			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicWrite);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (PublicWrite);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_PubVarAccessFromImpChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var PublicVariable = new Variable<string> ("", "HelloPublic");
			var ImplementationWrite = new WriteLine {
				Text = PublicVariable
			};

			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationWrite);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (ImplementationWrite);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_ImpVarAccessFromPubGrandchildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another
			location reference with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var PublicSequence = new Sequence {
				Activities = {
					new WriteLine {
						Text = ImplementationVariable
					}
				}
			};

			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicSequence);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (PublicSequence);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_ImpVarAccessFromPubChildsImpChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'WriteLineHolder': The private implementation of activity '2: WriteLineHolder' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var PublicWriteLineHolder = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = ImplementationVariable
				}
			};

			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicWriteLineHolder);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (PublicWriteLineHolder);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_PubVarAccessFromPubChildsImpChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'WriteLineHolder': The private implementation of activity '3: WriteLineHolder' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var PublicVariable = new Variable<string> ("", "HelloPublic");
			var PublicWriteLineHolder = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = PublicVariable
				}
			};
			
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicWriteLineHolder);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (PublicWriteLineHolder);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_PubVarAccessFromImpChildsPubChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with
			the same name that is visible at this scope, but it does not reference the same location.
			 */
			var PublicVariable = new Variable<string> ("", "HelloPublic");
			var ImplementationSequence = new Sequence {
				Activities = {
					new WriteLine {
						Text = new InArgument<string> (PublicVariable)
					},
				}
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationSequence);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (ImplementationSequence);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_ImpVarAccessFromImpGrandchildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with
			the same name that is visible at this scope, but it does not reference the same location.
			 */
			var ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var ImplementationWriteLineHolder = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = ImplementationVariable
				}
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationWriteLineHolder);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (ImplementationWriteLineHolder);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment4_PubVarAccessFromImpGrandchildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   The 
			referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the 
			same name that is visible at this scope, but it does not reference the same location.
			 */
			var PublicVariable = new Variable<string> ("","HelloPublic");
			var ImplementationWriteLineHolder = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = PublicVariable
				}
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationWriteLineHolder);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (ImplementationWriteLineHolder);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		#endregion

		[Test]
		public void Increment4_ImpVarAccessFromExecute ()
		{
			var ImplementationVariable = new Variable<string> ("name","HelloImplementation");
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				string temp = context.GetValue (ImplementationVariable);
				Assert.AreEqual ("HelloImplementation", temp);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
		}

		[Test]
		public void Increment4_ImpVarAccessFromImpChild ()
		{
			Variable<string> ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var ImplementationWrite = new WriteLine {
				Text = ImplementationVariable
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationWrite);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (ImplementationWrite);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}

		[Test]
		public void Increment4_PubVarAccessFromPubChild ()
		{
			var PublicVariable = new Variable<string> ("", "HelloPublic");
			var PublicWrite = new WriteLine {
				Text = PublicVariable
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicWrite);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (PublicWrite);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			RunAndCompare (wf, "HelloPublic" + Environment.NewLine);
		}

		[Test]
		public void Increment4_ImpVarAccessFromImpChildsPubChild ()
		{
			var ImplementationVariable = new Variable<string> ("name","HelloImplementation");
			var ImplementationSequence = new Sequence {
				Activities = {
					new WriteLine {
						Text = new InArgument<string> (ImplementationVariable)
					},
				}
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationSequence);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (ImplementationSequence);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}

		[Test]
		public void Increment4_PubVarAccessFromPubGrandchild ()
		{
			var PublicVariable = new Variable<string> ("","HelloPublic");
			var PublicSequence = new Sequence {
				Activities = {
					new WriteLine {
						Text = PublicVariable
					}
				}
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicSequence);
			};

			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (PublicSequence);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			RunAndCompare (wf, "HelloPublic" + Environment.NewLine);
		}

		[Test]
		public void Increment4_VariableDefaultHandlingImp ()
		{
			var ImpVar = new Variable<string> ("", "HelloImplementation");

			Action<NativeActivityMetadata> cacheMetadataChild = (metadata) => {
				metadata.AddImplementationVariable (ImpVar);
			};
			
			Action<NativeActivityContext> executeChild = (context) => {
				Assert.AreEqual ("HelloImplementation", ImpVar.Get (context));
				Assert.AreEqual ("HelloImplementation", ImpVar.GetLocation (context).Value);
				ImpVar.Set (context, "AnotherValue");
				Assert.AreEqual ("AnotherValue", ImpVar.Get (context));
				Assert.AreEqual ("AnotherValue", ImpVar.GetLocation (context).Value);
			};
			var PubChild = new NativeRunnerMock (cacheMetadataChild, executeChild);
			// create another activity to schedule the above twice, ensuring variable value 
			// from execution 1st time round isnt held and default is used again
			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddChild (PubChild);
			};
		
			Action<NativeActivityContext> executeParent = (context) => {
				context.ScheduleActivity (PubChild);
				context.ScheduleActivity (PubChild);
			};

			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}

		[Test]
		public void Increment4_VariableDefaultHandlingPub ()
		{
			var PubVar = new Variable<string> ("", "HelloPublic");

			var wf = new Sequence {
				Variables = {
					PubVar
				},
				Activities = {
					new WriteLine {
						Text = PubVar
					},
					new Assign {
						Value = new InArgument<string> ("AnotherValue"),
						To = new OutArgument<string> (PubVar)
					},
					new WriteLine {
						Text = PubVar
					}
				}
			};
			RunAndCompare (wf, String.Format ("HelloPublic{0}AnotherValue{0}", Environment.NewLine));
		}

		//FIXME: move to InArgumentT/OutArgumentT tests
		[Test]
		public void Increment4_ImplicitVarToArgConversion ()
		{
			var v1 = new Variable<string> ("name","value");
			InArgument<string> IA = v1;
			//string type = IA.Expression.GetType ().FullName;
			Assert.IsInstanceOfType (typeof (VariableValue<string>),IA.Expression);
			OutArgument<string> OA = v1;
			//var OType = OA.Expression.GetType ().FullName;
			Assert.IsInstanceOfType (typeof (VariableReference<string>),OA.Expression);
		}
		/*
		[Test]
		public void LinqTimeTest ()
		{
			var list = new global::System.Collections.ObjectModel.Collection<string> { "1","2","3","4","5","6","7","8","9",
											"10","11","12","13","14","15","16" };
			var linqTimer = new global::System.Diagnostics.Stopwatch ();
			var enumTimer = new global::System.Diagnostics.Stopwatch ();
			enumTimer.Start ();
			string result;
			foreach (var l in list) {
				if (l == "16")
					result = l;
			}
			enumTimer.Stop ();
			linqTimer.Start ();
			result = list.First (l => l == "16");
			linqTimer.Stop ();

			long linq = linqTimer.ElapsedTicks;
			long enumd = enumTimer.ElapsedTicks;
		}
		*/
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
