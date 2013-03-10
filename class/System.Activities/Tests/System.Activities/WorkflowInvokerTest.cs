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
		[Ignore ("Extensions")]
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

		class TrackIdWrite : CodeActivity {
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
			}
			protected override void Execute (CodeActivityContext context)
			{
				Console.WriteLine ("CacheId: {0} ActivityInstanceId: {1} Id: {2}",
						this.CacheId, context.ActivityInstanceId, this.Id);

			}
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
			var WriteLineChild = new WriteLine {
				Text = new InArgument<string> ("ChildWrite")
			};
			var WriteLineImpChild = new WriteLine {
				Text = new InArgument<string> ("ImpChildWrite")
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddChild (WriteLineChild);
				metadata.AddImplementationChild (WriteLineImpChild);
			}, (context) => {
				context.ScheduleActivity (WriteLineChild);
				context.ScheduleActivity (WriteLineImpChild);
			});
		
			RunAndCompare (wf, "ImpChildWrite" + Environment.NewLine +
					"ChildWrite" + Environment.NewLine);
			// test Ids
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", WriteLineChild.Id);
			Assert.AreEqual ("1.1", WriteLineImpChild.Id);
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
			}, (context) => {
				context.GetValue (PublicVariable); // should raise error
			});
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

			var PublicChild = new NativeRunnerMock (null, (context) => {
				context.GetValue (PublicVariable); // should raise error
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicChild);
			}, (context) => {
				context.ScheduleActivity (PublicChild);
			});
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

			var PublicChild = new NativeRunnerMock (null, (context) => {
				context.GetValue (ImplementationVariable); // should raise error
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicChild);
			}, (context) => {
				context.ScheduleActivity (PublicChild);
			});
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

			var ImplementationChild = new NativeRunnerMock (null, (context) => {
				context.GetValue (PublicVariable); // should raise error
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationChild);
			}, (context) => {
				context.ScheduleActivity (ImplementationChild);
			});
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

			var ImplementationChild = new NativeRunnerMock (null, (context) => {
				ImplementationVariable.Get (context); // should raise error
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationChild);
			}, (context) => {
				context.ScheduleActivity (ImplementationChild);
			});
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void Increment4_ImpVarAccessFromOwnInArgEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 *'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location 
			 * reference with the same name that is visible at this scope, but it does not reference the same location.
			*/
			var ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var InString = new InArgument<string> (ImplementationVariable);
			var rtInString = new RuntimeArgument ("InString", typeof (string), ArgumentDirection.In);

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddArgument (rtInString);
				metadata.Bind (InString, rtInString);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void Increment4_PubVarAccessFromOwnInArgEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 * 'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location 
			 * reference with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var PublicVariable = new Variable<string> ("", "PublicImplementation");
			var InString = new InArgument<string> (PublicVariable);
			var rtInString = new RuntimeArgument ("InString", typeof (string), ArgumentDirection.In);

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddArgument (rtInString);
				metadata.Bind (InString, rtInString);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicWrite);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationWrite);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicSequence);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddChild (PublicWriteLineHolder);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicWriteLineHolder);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationSequence);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationWriteLineHolder);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddImplementationChild (ImplementationWriteLineHolder);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}

		#endregion

		[Test]
		public void Increment4_ImpVarAccessFromExecute ()
		{
			var ImplementationVariable = new Variable<string> ("name","HelloImplementation");

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
			}, (context) => {
				string temp = context.GetValue (ImplementationVariable);
				Assert.AreEqual ("HelloImplementation", temp);
			});
		}

		[Test]
		public void Increment4_ImpVarAccessFromImpChild ()
		{
			Variable<string> ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var ImplementationWrite = new WriteLine {
				Text = ImplementationVariable
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationWrite);
			}, (context) => {
				context.ScheduleActivity (ImplementationWrite);
			});
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}

		[Test]
		public void Increment4_PubVarAccessFromPubChild ()
		{
			var PublicVariable = new Variable<string> ("", "HelloPublic");
			var PublicWrite = new WriteLine {
				Text = PublicVariable
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicWrite);
			}, (context) => {
				context.ScheduleActivity (PublicWrite);
			});
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationSequence);
			}, (context) => {
				context.ScheduleActivity (ImplementationSequence);
			});
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

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicSequence);
			}, (context) => {
				context.ScheduleActivity (PublicSequence);
			});
			RunAndCompare (wf, "HelloPublic" + Environment.NewLine);
		}

		[Test]
		public void Increment4_VariableDefaultHandlingImp ()
		{
			var ImpVar = new Variable<string> ("", "HelloImplementation");

			var PubChild = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImpVar);
			}, (context) => {
				Assert.AreEqual ("HelloImplementation", ImpVar.Get (context));
				Assert.AreEqual ("HelloImplementation", ImpVar.GetLocation (context).Value);
				ImpVar.Set (context, "AnotherValue");
				Assert.AreEqual ("AnotherValue", ImpVar.Get (context));
				Assert.AreEqual ("AnotherValue", ImpVar.GetLocation (context).Value);
			});
			// create another activity to schedule the above twice, ensuring variable value 
			// from execution 1st time round isnt held and default is used again

			ActivityInstance ai1 = null, ai2 = null;

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddChild (PubChild);
			}, (context) => {
				ai1 = context.ScheduleActivity (PubChild);
				ai2 = context.ScheduleActivity (PubChild);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreNotSame (ai1, ai2); //FIXME: Added check on refactoring
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
		[Ignore ("Argument Implicit Casts")]
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
		[Test]
		public void Increment4_DoubleScheduleChildWithPubVariable ()
		{
			var PublicVariable = new Variable<string> ("", "Default");
			var PublicWriteLine = new WriteLine { Text = PublicVariable };
			var PublicAssign = new Assign { 
				To = new OutArgument<string> (PublicVariable), 
				Value = new InArgument<string> ("Changed") 
			};

			var activityWithPubVar = new NativeRunnerMock ((metadata) => {
				metadata.AddVariable (PublicVariable);
				metadata.AddChild (PublicAssign);
				metadata.AddChild (PublicWriteLine);
			}, (context) => {
				// so variable will always be changed after PublicWriteLine runs
				// ie we only see Change in output if variable persisted between 1st 
				// and 2nd call
				context.ScheduleActivity (PublicAssign);
				context.ScheduleActivity (PublicWriteLine);
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddChild (activityWithPubVar);
			}, (context) => {
				context.ScheduleActivity (activityWithPubVar);
				context.ScheduleActivity (activityWithPubVar);
			});
			RunAndCompare (wf, String.Format ("Default{0}Default{0}", Environment.NewLine));
		}
		[Test]
		public void Increment4_DoubleScheduleChildWithImpVariable ()
		{
			var ImpVariable = new Variable<string> ("", "DefaultValue");
			var ImpWriteLine = new WriteLine { Text = ImpVariable };
			bool ran = false;

			var activityWithImpVar = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationVariable (ImpVariable);
				metadata.AddImplementationChild (ImpWriteLine);
			},(context) => {
				context.ScheduleActivity (ImpWriteLine);
				if (!ran) {// hack to ensure variable only set on first run
					ran = true;
					ImpVariable.Set (context, "Changed");
				}
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddChild (activityWithImpVar);
			}, (context) => {
				context.ScheduleActivity (activityWithImpVar);
				context.ScheduleActivity (activityWithImpVar);
			});
			RunAndCompare (wf, String.Format ("Changed{0}DefaultValue{0}", Environment.NewLine));
		}
		/*
		[Test]
		public void LinqTime ()
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
		public void Increment5_ActivityAction ()
		{
			// want to allow user to supply activity to which a string will be passed
			var inArg = new DelegateInArgument<string> ();
			ActivityAction<string> CustomActivity = new ActivityAction<string> {
				Argument = inArg,
				Handler = new WriteLine {
					Text = new InArgument<string> (inArg)
				}
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction<string>(CustomActivity, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test]
		public void Increment5_PubVarAccessFromPubDelegate ()
		{
			var varStr = new Variable<string> ("", "Hello\nWorld");

			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void Increment5_PubVarAccessFromPubChildPubDelegate ()
		{
			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});
			var wf = new Sequence {
				Variables = { varStr },
				Activities = { child }
			};
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_PubVarAccessFromPubChildImpDelegateEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '3: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.

			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});
			var wf = new Sequence {
				Variables = { varStr },
				Activities = { child }
			};
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_PubVarAccessFromImpChildPubDelegateEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.

			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleActivity(child);
			});

			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_PubVarAccessFromImpChildImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.
			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};
						
			var child = new NativeRunnerMock ( (metadata) => {
				metadata.AddImplementationDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleActivity(child);
			});
			
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void Increment5_ImpVarAccessFromImpChildPubDelegate ()
		{
			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity(child);
			});
			
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_ImpVarAccessFromPubChildPubDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be 
			// another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity(child);
			});
			
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_ImpVarAccessFromPubChildImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '2: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.
			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};
						
			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity(child);
			});
			
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_ImpVarAccessFromImpChildImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:  
			//The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			//with the same name that is visible at this scope, but it does not reference the same location.

			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity(child);
			});
			
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_PubVarAccessFromImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location

			var varStr = new Variable<string> ();
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationDelegate (CustomActivity);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void Increment5_ImpVarAccessFromImpDelegate ()
		{
			var varStr = new Variable<string> ("", "Hello\nWorld");
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddImplementationDelegate (CustomActivity);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_ImpVarAccessFromPubDelegateEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be 
			// another location reference with the same name that is visible at this scope, but it does not reference the same location.

			var varStr = new Variable<string> ();
			
			ActivityAction CustomActivity = new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};
						
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleAction(CustomActivity);
			});
			WorkflowInvoker.Invoke (wf);
		}

		class PublicDelegateRunnerMock<T> : NativeActivity {	
			ActivityAction<T> aAction;
			T value;
			public PublicDelegateRunnerMock (ActivityAction<T> action, T value)
			{
				aAction = action;
				this.value = value;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddDelegate (aAction);
			}
			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleAction<T> (aAction, value);
			}			
		}
		class ImplementationDelegateRunnerMock<T> : NativeActivity {	
			ActivityAction<T> aAction;
			T value;
			public ImplementationDelegateRunnerMock (ActivityAction<T> action, T value)
			{
				aAction = action;
				this.value = value;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddImplementationDelegate (aAction);
			}
			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleAction<T> (aAction, value);
			}			
		}
		class ImplementationHolder<T> : NativeActivity where T:Activity {
			public T Activity { get; set; }
			
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddImplementationChild (Activity);
			}
			
			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleActivity (Activity);
			}
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_AccessDelArgFromHndlrImpChildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			   'WriteLineHolder': The private implementation of activity '2: WriteLineHolder' has the following validation error
			   The referenced DelegateArgument object ('') is not visible at this scope.
			*/
			var delArg = new DelegateInArgument<string> ();

			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new WriteLineHolder {
					ImplementationWriteLine = new WriteLine {
						Text = new InArgument<string> (delArg)
					}
				}
			};

			var wf = new PublicDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_AccessDelArgFromHndlrImpChildsImpChildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			   'ImplementationHolder<ImplementationHolder<WriteLine>>': The private implementation of activity 
			   '2: ImplementationHolder<ImplementationHolder<WriteLine>>' has the following validation error:
			   The referenced DelegateArgument object ('') is not visible at this scope.
			 */ 
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<ImplementationHolder<WriteLine>> {
					Activity = new ImplementationHolder<WriteLine> {
						Activity = new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};
			
			var wf = new PublicDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_AccessDelArgFromHndlrImpChildsPubChildEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'ImplementationHolder<Sequence>': The private implementation of activity '2: ImplementationHolder<Sequence>' has the following validation error:
			  The referenced DelegateArgument object ('') is not visible at this scope.
			 */ 

			var delArg = new DelegateInArgument<string> ();

			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<Sequence> {
					Activity = new Sequence {
						Activities = {
							new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new PublicDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test]
		public void Increment5_AccessDelArgFromHndlrPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};
			
			var wf = new PublicDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test]
		public void Increment5_AccessDelArgFromHndlrPubChildsPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new Sequence {
							Activities = {
								new WriteLine {
									Text = new InArgument<string> (delArg)
								}
							}
						}
					}
				}
			};
			
			var wf = new PublicDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_AccessDelArgFromHndlrPubChildsImpChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ImplementationHolder<WriteLine>': The private implementation of activity '3: ImplementationHolder<WriteLine>' has the 
			// following validation error:   The referenced DelegateArgument object ('') is not visible at this scope.
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new ImplementationHolder<WriteLine> {
							Activity = new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new PublicDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Increment5_AccessDelArgFromExecuteEx ()
		{
			//System.InvalidOperationException : DelegateArgument 'Argument' does not exist in this environment.
			var delArg = new DelegateInArgument<string> ();
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new NativeRunnerMock (null, (context) => {
					Console.WriteLine ((string) delArg.Get (context));
				})
			};
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction (CustomActivity, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test]
		public void Increment5_NativeActivity_ScheduleDelegate ()
		{
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var delArg3 = new DelegateInArgument<string> ();
			var CustomActivity = new ActivityAction<string, string, string> {
				Argument1 = delArg1,
				Argument2 = delArg2,
				Argument3 = delArg3,
				Handler = new Sequence {
					Activities = {
						new WriteLine {
							Text = new InArgument<string> (delArg1)
						},
						new WriteLine {
							Text = new InArgument<string> (delArg2)
						},
						new WriteLine {
							Text = new InArgument<string> (delArg3)
						},
					}
				}
			};
			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				var param = new Dictionary<string, object> { {"Argument1", "Arg1"},
									{"Argument2", "Arg2"},
									{"Argument3", "Arg3"}};
				context.ScheduleDelegate (CustomActivity, param);
			});
			RunAndCompare (wf, String.Format ("Arg1{0}Arg2{0}Arg3{0}", Environment.NewLine));
		}

		[Test]
		public void Increment5_ScheduleMultipleActions ()
		{
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var CustomActivity1 = new ActivityAction<string> {
				Argument = delArg1,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg1)
				}
			};
			var CustomActivity2 = new ActivityAction<string> {
				Argument = delArg2,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg2)
				}
			};
			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity1);
				metadata.AddDelegate (CustomActivity2);
			}, (context) => {
				context.ScheduleAction (CustomActivity2, "Arg2");
				context.ScheduleAction (CustomActivity1, "Arg1");
			});
			RunAndCompare (wf, String.Format ("Arg1{0}Arg2{0}", Environment.NewLine));
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_ScheduleMultipleActionsCrossedArgsEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'DelegateArgumentValue<String>': DelegateArgument '' must be included in an activity's ActivityDelegate before it is used.
			  'DelegateArgumentValue<String>': The referenced DelegateArgument object ('') is not visible at this scope.
			 */ 
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var CustomActivity1 = new ActivityAction<string> {
				Argument = delArg1,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg1)
				}
			};
			var CustomActivity2 = new ActivityAction<string> {
				Argument = delArg2,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg1) // should cause error
				}
			};
			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity1);
				metadata.AddDelegate (CustomActivity2);
			}, (context) => {
				context.ScheduleAction (CustomActivity2, "Arg2");
				context.ScheduleAction (CustomActivity1, "Arg1");
			});
			WorkflowInvoker.Invoke (wf);
		}

		[Test]
		public void Increment5_ScheduleActionsMultipleTimesDifArgs ()
		{

			var delArg = new DelegateInArgument<string> ();
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg)
				}
			};

			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);
			}, (context) => {
				context.ScheduleAction (CustomActivity, "Run2");
				context.ScheduleAction (CustomActivity, "Run1");
			});
			RunAndCompare (wf, String.Format ("Run1{0}Run2{0}", Environment.NewLine));
		}

		#region ------------MAYBE DONT KEEP THESE TESTS ---------------------
		/*just show setting delegate public or implemenetation doesnt affect scoping rules for 
		 * arguments passed in*/

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_Implementation_AccessDelArgFromHndlrImpChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new WriteLineHolder {
					ImplementationWriteLine = new WriteLine {
						Text = new InArgument<string> (delArg)
					}
				}
			};
			
			var wf = new ImplementationDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_Implementation_AccessDelArgFromHndlrImpChildsImpChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<ImplementationHolder<WriteLine>> {
					Activity = new ImplementationHolder<WriteLine> {
						Activity = new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};
			
			var wf = new ImplementationDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_Implementation_AccessDelArgFromHndlrImpChildsPubChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<Sequence> {
					Activity = new Sequence {
						Activities = {
							new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		
		[Test]
		public void Increment5_Implementation_AccessDelArgFromHndlrPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};
			
			var wf = new ImplementationDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		[Test]
		public void Increment5_Implementation_AccessDelArgFromHndlrPubChildsPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new Sequence {
							Activities = {
								new WriteLine {
									Text = new InArgument<string> (delArg)
								}
							}
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_Implementation_AccessDelArgFromHndlrPubChildsImpChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();
			
			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new ImplementationHolder<WriteLine> {
							Activity = new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunnerMock<string> (CustomActivity, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		#endregion

		class Concat : CodeActivity<string> {
			public InArgument<string> String1 { get; set; }
			public InArgument<string> String2 { get; set; }
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				RuntimeArgument rtString1 = new RuntimeArgument ("String1", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtString1);
				metadata.Bind (String1, rtString1);
				RuntimeArgument rtString2 = new RuntimeArgument ("String2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtString2);
				metadata.Bind (String2, rtString2);
			}
			protected override string Execute (CodeActivityContext context)
			{
				//FIXME: no need for generic type param on .NET
				return String1.Get<string> (context) + String2.Get<string> (context);
			}
		}

		[Test]
		public void Increment5_ActivityFuncAndInvokeFunc ()
		{
			// want to allow user to supply activity to which a string will be passed

			var inArg1 = new DelegateInArgument<string> ();
			var inArg2 = new DelegateInArgument<string> ();
			var CustomActivity = new ActivityFunc<string, string, string> {
				Argument1 = inArg1,
				Argument2 = inArg2,
				Handler = new Concat {
					String1 = new InArgument<string> (inArg1),
					String2 = new InArgument<string> (inArg2),
				}
			};
			var resultVar = new Variable<string> ();
			var wf = new Sequence {
				Variables = {
					resultVar,
				},
				Activities = {
					new InvokeFunc <string, string, string> {
						Func = CustomActivity,
						Argument1 = new InArgument<string> ("Hello\n"),
						Argument2 = new InArgument<string> ("World"),
						Result = new OutArgument<string> (resultVar)
					},
					new WriteLine {
						Text = new InArgument<string> (resultVar),
					}
				}
			};
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void Increment5_DelegateIds ()
		{
			var CustomActivity1 = new ActivityAction {
				Handler = new TrackIdWrite ()
			};
			var CustomActivity2 = new ActivityAction {
				Handler = new TrackIdWrite ()
			};
			var child = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity1);
				metadata.AddImplementationDelegate (CustomActivity2);
			}, (context) => {
				context.ScheduleAction(CustomActivity1);
				context.ScheduleAction(CustomActivity2);});

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddChild (child);}, (context) => {
				context.ScheduleActivity(child);});

			RunAndCompare (wf, "CacheId: 1 ActivityInstanceId: 4 Id: 2.1" + Environment.NewLine +
			               "CacheId: 1 ActivityInstanceId: 3 Id: 3" + Environment.NewLine);
		}

		[Test]
		public void Increment5_DelegateInArgValueChanged ()
		{
			var delArg = new DelegateInArgument<string> ();

			var CustomActivity = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new WriteLine { Text = new InArgument<string> (delArg) },
						new Assign { 
							To = new OutArgument<string> (delArg),
							Value = new InArgument<string> ("Changed")
						},
						new WriteLine { Text = new InArgument<string> (delArg) },
						new Sequence {
							Activities = {
								new WriteLine { Text = new InArgument<string> (delArg) },
							}
						},
					}
				}
			};
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (CustomActivity);}, (context) => {
				context.ScheduleAction (CustomActivity, "Hello\nWorld");});

			RunAndCompare (wf, String.Format ("Hello\nWorld{0}Changed{0}Changed{0}", Environment.NewLine));
		}

		[Test]
		public void Increment5_AccessDelArgFromHndlrsPubDelegateHndlr ()
		{
			var delArg = new DelegateInArgument<string> ();
			var ChildDelegate = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> (delArg) }
			};
			var ParentDelegate = new ActivityAction<string> {
				Argument = delArg,
				Handler = new NativeRunnerMock ((metadata) => {
					metadata.AddDelegate (ChildDelegate);
				}, (context) => {
					context.ScheduleAction (ChildDelegate);
				})
			};

			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (ParentDelegate);
			}, (context) => {
				context.ScheduleAction (ParentDelegate, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Increment5_AccessDelArgFromHndlrsImpDelegateHndlrEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '2: NativeRunnerMock' has the following validation error:  
			// The referenced DelegateArgument object ('') is not visible at this scope.
			var delArg = new DelegateInArgument<string> ();
			var ChildDelegate = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> (delArg) }
			};
			var ParentDelegate = new ActivityAction<string> {
				Argument = delArg,
				Handler = new NativeRunnerMock ((metadata) => {
					metadata.AddImplementationDelegate (ChildDelegate);
				}, (context) => {
					context.ScheduleAction (ChildDelegate);
				})
			};
			
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (ParentDelegate);
			}, (context) => {
				context.ScheduleAction (ParentDelegate, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		/*
		class Args : Activity {
			public InArgument<string> Text { get; set; }
			public Args ()
			{
				Implementation = () => {
					return new WriteLine { Text = new ArgumentValue<string> ("Text")};
				};
			}
			protected override void CacheMetadata (ActivityMetadata metadata)
			{
				var rt = new RuntimeArgument ("Text", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rt);
				metadata.Bind (Text, rt);
			}
		}
		
		[Test]
		public void ArgumentScope ()
		{
			var a = new Args { Text = "hello" };
			RunAndCompare (a, "hello" + Environment.NewLine);
		}
		*/
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Invoke_workflow_NullEx ()
		{
			WorkflowInvoker.Invoke ((Activity) null);
		}

		[Test]
		[Ignore ("Not Implemented")]
		public void Invoke_workflow ()
		{
			throw new NotImplementedException ();
		}
		class WorkflowInvokerMoreTestsClass {
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
}
