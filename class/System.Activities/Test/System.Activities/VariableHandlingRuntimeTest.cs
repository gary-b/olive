using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class VariableHandlingRuntimeTest : WFTestHelper {
		Activity<string> GetMetadataWriter (string name)
		{
			return new CodeActivityTRunner<string> ((metadata)=> { 
				Console.WriteLine (name); 
			}, (context) => {
				return name;
			});
		}
		[Test]
		public void Default_Cachemetadata_OrderCalled_ActivityIdGeneration ()
		{
			//children executed in lifo manner
			//but implementation children executed first

			var impV1 = new Variable<string> ();
			var impV2 = new Variable<string> ();
			var pubV1 = new Variable<string> ();
			var pubV2 = new Variable<string> ();

			impV1.Default = GetMetadataWriter ("impV1");
			impV2.Default = GetMetadataWriter ("impV2");
			pubV1.Default = GetMetadataWriter ("pubV1");
			pubV2.Default = GetMetadataWriter ("pubV2");

			var wf = new NativeActivityRunner (metadata => {
				Console.WriteLine ("wf");
				metadata.AddImplementationVariable (impV2);
				metadata.AddVariable (pubV2);
				metadata.AddImplementationVariable (impV1);
				metadata.AddVariable (pubV1);
			}, null);

			var app = new WFAppWrapper (wf);
			app.Run ();

			//Test Order Called
			var split = app.ConsoleOut.Split (new string [] { Environment.NewLine }, StringSplitOptions.None);
			//remove trailing empty string
			var actualOrder = new string [split.Length - 1];
			for (int i = 0; i < split.Length - 1; i++)
				actualOrder [i] = split [i];
			var expected = new string [] { "wf", "impV1", "impV2", "pubV1", "pubV2" };
			Assert.AreEqual (expected, actualOrder);

			// Test Activity Ids Generated
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", pubV1.Default.Id);
			Assert.AreEqual ("3", pubV2.Default.Id);
			Assert.AreEqual ("1.1", impV1.Default.Id);
			Assert.AreEqual ("1.2", impV2.Default.Id);
		}
		[Test]
		public void Default_Cachemetadata_OrderCalled_ActivityIdGeneration2 ()
		{
			var pubDefV1PubChild1 = GetMetadataWriter ("pubDefV1PubChild1");
			var pubDefV1PubChild2 = GetMetadataWriter ("pubDefV1PubChild2");
			var pubDefV1ImpChild1 = GetMetadataWriter ("pubDefV1ImpChild1");
			var pubDefV1ImpChild2 = GetMetadataWriter ("pubDefV1ImpChild2");
			var pubDefV2PubChild1 = GetMetadataWriter ("pubDefV2PubChild1");
			var pubDefV2PubChild2 = GetMetadataWriter ("pubDefV2PubChild2");
			var pubDefV2ImpChild1 = GetMetadataWriter ("pubDefV2ImpChild1");
			var pubDefV2ImpChild2 = GetMetadataWriter ("pubDefV2ImpChild2");
			var impDefV1PubChild1 = GetMetadataWriter ("impDefV1PubChild1");
			var impDefV1PubChild2 = GetMetadataWriter ("impDefV1PubChild2");
			var impDefV1ImpChild1 = GetMetadataWriter ("impDefV1ImpChild1");
			var impDefV1ImpChild2 = GetMetadataWriter ("impDefV1ImpChild2");
			var impDefV2PubChild1 = GetMetadataWriter ("impDefV2PubChild1");
			var impDefV2PubChild2 = GetMetadataWriter ("impDefV2PubChild2");
			var impDefV2ImpChild1 = GetMetadataWriter ("impDefV2ImpChild1");
			var impDefV2ImpChild2 = GetMetadataWriter ("impDefV2ImpChild2");

			var pubDefV2 = new NativeActivityRunner<string> ((metadata) => {
				Console.WriteLine ("pubDefV2");
				metadata.AddImplementationChild (pubDefV2ImpChild2);
				metadata.AddChild (pubDefV2PubChild2);
				metadata.AddImplementationChild (pubDefV2ImpChild1);
				metadata.AddChild (pubDefV2PubChild1);
			}, null);

			var pubDefV1 = new NativeActivityRunner<string> ((metadata) => {
				Console.WriteLine ("pubDefV1");
				metadata.AddImplementationChild (pubDefV1ImpChild2);
				metadata.AddChild (pubDefV1PubChild2);
				metadata.AddImplementationChild (pubDefV1ImpChild1);
				metadata.AddChild (pubDefV1PubChild1);
			}, null);

			var impDefV2 = new NativeActivityRunner<string> ((metadata) => {
				Console.WriteLine ("impDefV2");
				metadata.AddImplementationChild (impDefV2ImpChild2);
				metadata.AddChild (impDefV2PubChild2);
				metadata.AddImplementationChild (impDefV2ImpChild1);
				metadata.AddChild (impDefV2PubChild1);
			}, null);

			var impDefV1 = new NativeActivityRunner<string> ((metadata) => {
				Console.WriteLine ("impDefV1");
				metadata.AddImplementationChild (impDefV1ImpChild2);
				metadata.AddChild (impDefV1PubChild2);
				metadata.AddImplementationChild (impDefV1ImpChild1);
				metadata.AddChild (impDefV1PubChild1);
			}, null);

			var impV1 = new Variable<string> ();
			var impV2 = new Variable<string> ();
			var pubV1 = new Variable<string> ();
			var pubV2 = new Variable<string> ();

			impV1.Default = impDefV1;
			impV2.Default = impDefV2;
			pubV1.Default = pubDefV1;
			pubV2.Default = pubDefV2;

			var wf = new NativeActivityRunner (metadata => {
				Console.WriteLine ("wf");
				metadata.AddImplementationVariable (impV2);
				metadata.AddVariable (pubV2);
				metadata.AddImplementationVariable (impV1);
				metadata.AddVariable (pubV1);
			}, null);

			var app = new WFAppWrapper (wf);
			app.Run ();

			//Test Order Called
			var split = app.ConsoleOut.Split (new string [] { Environment.NewLine }, StringSplitOptions.None);
			//remove trailing empty string
			var actualOrder = new string [split.Length - 1];
			for (int i = 0; i < split.Length - 1; i++)
				actualOrder [i] = split [i];
			var expected = new string [] { "wf",
				"impDefV1", "impDefV1ImpChild1", "impDefV1ImpChild2", "impDefV1PubChild1", "impDefV1PubChild2", 
				"impDefV2", "impDefV2ImpChild1", "impDefV2ImpChild2", "impDefV2PubChild1", "impDefV2PubChild2", 
				"pubDefV1", "pubDefV1ImpChild1", "pubDefV1ImpChild2", "pubDefV1PubChild1", "pubDefV1PubChild2", 
				"pubDefV2", "pubDefV2ImpChild1", "pubDefV2ImpChild2", "pubDefV2PubChild1", "pubDefV2PubChild2"};
			Assert.AreEqual (expected, actualOrder);

			// Test Activity Ids Generated
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", pubDefV1.Id);
			Assert.AreEqual ("3", pubDefV1PubChild1.Id);
			Assert.AreEqual ("4", pubDefV1PubChild2.Id);
			Assert.AreEqual ("2.1", pubDefV1ImpChild1.Id);
			Assert.AreEqual ("2.2", pubDefV1ImpChild2.Id);
			Assert.AreEqual ("1.1", impDefV1.Id);
			Assert.AreEqual ("1.2", impDefV1PubChild1.Id);
			Assert.AreEqual ("1.3", impDefV1PubChild2.Id);
			Assert.AreEqual ("1.1.1", impDefV1ImpChild1.Id);
			Assert.AreEqual ("1.1.2", impDefV1ImpChild2.Id);
			Assert.AreEqual ("5", pubDefV2.Id);
			Assert.AreEqual ("6", pubDefV2PubChild1.Id);
			Assert.AreEqual ("7", pubDefV2PubChild2.Id);
			Assert.AreEqual ("5.1", pubDefV2ImpChild1.Id);
			Assert.AreEqual ("5.2", pubDefV2ImpChild2.Id);
			Assert.AreEqual ("1.4", impDefV2.Id);
			Assert.AreEqual ("1.5", impDefV2PubChild1.Id);
			Assert.AreEqual ("1.6", impDefV2PubChild2.Id);
			Assert.AreEqual ("1.4.1", impDefV2ImpChild1.Id);
			Assert.AreEqual ("1.4.2", impDefV2ImpChild2.Id);
		}
		[Test]
		public void CanNotScheduleVariableDefaultFromSelf ()
		{
			//System.InvalidOperationException : An Activity can only schedule its direct children. 
			//Activity 'NativeActivityRunner' is attempting to schedule 'VariableValue<String>' which 
			//is a child of activity 'WriteLine'.
			Exception ex = null;
			var pubVar = new Variable<string> ("", "HelloPub");
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
			}, (context) => {
				try {
					context.ScheduleActivity (pubVar.Default);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		#region Increment4 Exception Tests
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void PubVarAccessFromExecuteEx ()
		{
			/*
			System.InvalidOperationException : Activity '1: NativeRunnerMock' cannot access this variable because 
			it is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own 
			implementation variables.
			 */
			var pubVar = new Variable<string> ("", "HelloPublic");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
			}, (context) => {
				context.GetValue (pubVar); // should raise error
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void PubVarAccessFromPubChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '3: NativeRunnerMock' cannot access this variable because
			 * it is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own 
			 * implementation variables.
			 */
			var pubVar = new Variable<string> ("", "HelloPublic");

			var pubChild = new NativeActivityRunner (null, (context) => {
				context.GetValue (pubVar); // should raise error
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void ImpVarAccessFromPubChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '2: NativeRunnerMock' cannot access this variable because it is 
			 * declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own implementation variables.
			 */
			var impVar = new Variable<string> ("", "HelloImplementationVariable");

			var pubChild = new NativeActivityRunner (null, (context) => {
				context.GetValue (impVar); // should raise error
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void PubVarAccessFromImpChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '1.1: NativeRunnerMock' cannot access this variable because 
			 * it is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own 
			 * implementation variables.
			 */
			var pubVar = new Variable<string> ("", "HelloPublic");

			var impChild = new NativeActivityRunner (null, (context) => {
				context.GetValue (pubVar); // should raise error
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void ImpVarAccessFromImpChildsExecuteEx ()
		{
			/*
			 * System.InvalidOperationException : Activity '1.2: NativeRunnerMock' cannot access this variable because it 
			 * is declared at the scope of activity '1: NativeRunnerMock'.  An activity can only access its own implementation
			 * variables.
			 */
			var impVar = new Variable<string> ("", "HelloImplementation");

			var impChild = new NativeActivityRunner (null, (context) => {
				impVar.Get (context); // should raise error
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void ImpVarAccessFromOwnInArgEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 *'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location 
			 * reference with the same name that is visible at this scope, but it does not reference the same location.
			*/
			var impVar = new Variable<string> ("", "HelloImplementation");
			var inString = new InArgument<string> (impVar);
			var rtInString = new RuntimeArgument ("inString", typeof (string), ArgumentDirection.In);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddArgument (rtInString);
				metadata.Bind (inString, rtInString);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void PubVarAccessFromOwnInArgEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 * 'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location 
			 * reference with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var pubVar = new Variable<string> ("", "PublicImplementation");
			var inString = new InArgument<string> (pubVar);
			var rtInString = new RuntimeArgument ("inString", typeof (string), ArgumentDirection.In);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddArgument (rtInString);
				metadata.Bind (inString, rtInString);
			}, null);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void ImpVarAccessFromPubChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another 
			location reference with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var impVar = new Variable<string> ("", "HelloImplementation");
			var pubChild = new WriteLine {
				Text = impVar
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void PubVarAccessFromImpChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var pubVar = new Variable<string> ("", "HelloPublic");
			var impChild = new WriteLine {
				Text = pubVar
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void ImpVarAccessFromPubGrandchildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another
			location reference with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var impVar = new Variable<string> ("", "HelloImplementation");
			var pubChild = new Sequence {
				Activities = {
					new WriteLine {
						Text = impVar
					}
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void ImpVarAccessFromPubChildsImpChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'WriteLineHolder': The private implementation of activity '2: WriteLineHolder' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var impVar = new Variable<string> ("", "HelloImplementation");
			var pubChild = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = impVar
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void PubVarAccessFromPubChildsImpChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'WriteLineHolder': The private implementation of activity '3: WriteLineHolder' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			with the same name that is visible at this scope, but it does not reference the same location.
			 */
			var pubVar = new Variable<string> ("", "HelloPublic");
			var pubChild = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = pubVar
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void PubVarAccessFromImpChildsPubChildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with
			the same name that is visible at this scope, but it does not reference the same location.
			 */
			var pubVar = new Variable<string> ("", "HelloPublic");
			var impChild = new Sequence {
				Activities = {
					new WriteLine {
						Text = new InArgument<string> (pubVar)
					},
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void ImpVarAccessFromImpGrandchildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with
			the same name that is visible at this scope, but it does not reference the same location.
			 */
			var impVar = new Variable<string> ("", "HelloImplementation");
			var impChild = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = impVar
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Variables Exception / Validations")]
		public void PubVarAccessFromImpGrandchildEx ()
		{
			/*
			System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   The 
			referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the 
			same name that is visible at this scope, but it does not reference the same location.
			 */
			var pubVar = new Variable<string> ("","HelloPublic");
			var impChild = new WriteLineHolder {
				ImplementationWriteLine = new WriteLine {
					Text = pubVar
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			WorkflowInvoker.Invoke (wf);
		}
		#endregion

		[Test]
		public void ImpVarAccessFromExecute ()
		{
			var impVar = new Variable<string> ("name","HelloImplementation");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, (context) => {
				string temp = context.GetValue (impVar);
				Assert.AreEqual ("HelloImplementation", temp);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ImpVarAccessFromImpChild ()
		{
			var impVar = new Variable<string> ("", "HelloImplementation");
			var impChild = new WriteLine {
				Text = impVar
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		[Test]
		public void PubVarAccessFromPubChild ()
		{
			var pubVar = new Variable<string> ("", "HelloPublic");
			var pubChild = new WriteLine {
				Text = pubVar
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			RunAndCompare (wf, "HelloPublic" + Environment.NewLine);
		}
		[Test]
		public void ImpVarAccessFromImpChildsPubChild ()
		{
			var impVar = new Variable<string> ("name","HelloImplementation");
			var impChild = new Sequence {
				Activities = {
					new WriteLine {
						Text = new InArgument<string> (impVar)
					},
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ImpVarAccessFromImpChildsPubChildImpChildEx ()
		{
			var impVar = new Variable<string> ("name","HelloImplementation");
			var impChildPubChildImpChild = new WriteLine { Text = impVar };

			var impChildPubChild = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (impChildPubChildImpChild);
			}, (context) => {
				context.ScheduleActivity (impChildPubChildImpChild);
			});
			var impChild = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (impChildPubChild);
			}, (context) => {
				context.ScheduleActivity (impChildPubChild);
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		[Test]
		public void PubVarAccessFromPubGrandchild ()
		{
			var pubVar = new Variable<string> ("","HelloPublic");
			var pubChild = new Sequence {
				Activities = {
					new WriteLine {
						Text = pubVar
					}
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			RunAndCompare (wf, "HelloPublic" + Environment.NewLine);
		}
		[Test]
		public void ImpVarDefaultHandling ()
		{
			var impVar = new Variable<string> ("", "HelloImplementation");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, (context) => {
				Assert.AreEqual ("HelloImplementation", impVar.Get (context));
				Assert.AreEqual ("HelloImplementation", impVar.GetLocation (context).Value);
				impVar.Set (context, "AnotherValue");
				Assert.AreEqual ("AnotherValue", impVar.Get (context));
				Assert.AreEqual ("AnotherValue", impVar.GetLocation (context).Value);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void PubVarDefaultHandling ()
		{
			var pubVar = new Variable<string> ("", "HelloPublic");

			var wf = new Sequence {
				Variables = {
					pubVar
				},
				Activities = {
					new WriteLine {
						Text = pubVar
					},
					new Assign {
						Value = new InArgument<string> ("AnotherValue"),
						To = new OutArgument<string> (pubVar)
					},
					new WriteLine {
						Text = pubVar
					}
				}
			};
			RunAndCompare (wf, String.Format ("HelloPublic{0}AnotherValue{0}", Environment.NewLine));
		}
		[Test]
		public void DoubleScheduleChildWithPubVariable ()
		{
			var pubVar = new Variable<string> ("", "Default");
			var pubWriteLine = new WriteLine { Text = pubVar };
			var pubAssign = new Assign { 
				To = new OutArgument<string> (pubVar), 
				Value = new InArgument<string> ("Changed") 
			};

			var activityWithPubVar = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddChild (pubAssign);
				metadata.AddChild (pubWriteLine);
			}, (context) => {
				// so variable will always be changed after pubAssign runs
				// ie we only see Change in output if variable value persists between 1st 
				// and 2nd call
				context.ScheduleActivity (pubAssign); // will be executed 2nd
				context.ScheduleActivity (pubWriteLine); // last scheduled = first to be executed
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (activityWithPubVar);
			}, (context) => {
				context.ScheduleActivity (activityWithPubVar); 
				context.ScheduleActivity (activityWithPubVar); 
			});
			RunAndCompare (wf, String.Format ("Default{0}Default{0}", Environment.NewLine));
		}
		[Test]
		public void DoubleScheduleChildWithImpVariable ()
		{
			var impVar = new Variable<string> ("", "DefaultValue");
			var impWriteLine = new WriteLine { Text = impVar };
			bool ran = false;

			var activityWithImpVar = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impWriteLine);
			},(context) => {
				context.ScheduleActivity (impWriteLine);
				if (!ran) {// hack to ensure variable only set on first run
					ran = true;
					impVar.Set (context, "Changed");
				}
			});
			// also checking new ActivityInstance generated each time
			ActivityInstance ai1 = null, ai2 = null;

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (activityWithImpVar);
			}, (context) => {
				ai1 = context.ScheduleActivity (activityWithImpVar);
				ai2 = context.ScheduleActivity (activityWithImpVar);
			});
			RunAndCompare (wf, String.Format ("Changed{0}DefaultValue{0}", Environment.NewLine));
			Assert.AreNotSame (ai1, ai2);
		}
		#region Tests for Var access from Activities being used as Variable.Default
		class ConcatWriter : Concat {
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				Console.WriteLine ("concatMd");
				base.CacheMetadata (metadata);
			}
			protected override string Execute (CodeActivityContext context)
			{
				Console.WriteLine ("concatEx");
				return String1.Get (context) + String2.Get (context);
			}
		}
		Activity<string> GetLiteralWriter (string literal)
		{
			return new CodeActivityTRunner<string> ((metadata)=> { 
				Console.WriteLine ("literalMd"); 
			}, (context) => {
				Console.WriteLine ("literalEx"); 
				return literal;
			});
		}
		#region Access Variables on same Activity from Default's args
		Activity GetActWithPubVarPubVarWritesLast (Variable<string> var1, Variable<string> varToWrite)
		{
			var writeLine = new WriteLine { Text = varToWrite };
			return new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (var1);
				metadata.AddVariable (varToWrite);
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
		}
		Activity GetActWithImpVarImpVarWritesLast (Variable<string> var1, Variable<string> varToWrite)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (var1);
				metadata.AddImplementationVariable (varToWrite);
			}, (context) => {
				Console.WriteLine (varToWrite.Get (context));
			});
		}
		Activity GetActWithPubVarImpVarWritesLast (Variable<string> var1, Variable<string> varToWrite)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (var1);
				metadata.AddImplementationVariable (varToWrite);
			}, (context) => {
				Console.WriteLine (varToWrite.Get (context));
			});
		}
		Activity GetActWithImpVarPubVarWritesLast (Variable<string> var1, Variable<string> varToWrite)
		{
			var writeLine = new WriteLine { Text = varToWrite };
			return new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (var1);
				metadata.AddVariable (varToWrite);
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
		}
		Activity GetActWithImpVarSchedulesImpChild (Variable<string> var1, Activity child)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (var1);
				metadata.AddImplementationChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		Activity GetActWithImpVarSchedulesPubChild (Variable<string> var1, Activity child)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (var1);
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		Activity GetActWithPubVarSchedulesPubChild (Variable<string> var1, Activity child)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (var1);
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		Activity GetActWithPubVarSchedulesImpChild (Variable<string> var1, Activity child)
		{
			return new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (var1);
				metadata.AddImplementationChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		[Test]
		public void Default_PubVarAccessFromSiblingPubVar ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var wf = GetActWithPubVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromSiblingImpVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '1: NativeActivityRunner' has the following validation error:   
			//The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same
			//name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var wf = GetActWithImpVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromSiblingPubVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var wf = GetActWithImpVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test]
		public void Default_PubVarAccessFromSiblingImpVar ()
		{
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var wf = GetActWithPubVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		#endregion

		#region Access Variable from Sibling Variable Defaults Children
		[Test]
		public void Default_PubVarAccessFromSiblingImpVarPubChild ()
		{
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfPubChild (concat);
			var wf = GetActWithPubVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_PubVarAccessFromSiblingImpVarImpChildEx ()
		{
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfImpChild (concat);
			var wf = GetActWithPubVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_PubVarAccessFromSiblingPubVarImpChildEx ()
		{
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfImpChild (concat);
			var wf = GetActWithPubVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test]
		public void Default_PubVarAccessFromSiblingPubVarPubChild ()
		{

			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			var writer = new WriteLine { Text = vTest };
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfPubChild (concat);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (vTest); //vTest needs added first
				metadata.AddVariable (vStr);
				metadata.AddChild (writer);
			}, (context) => {
				context.ScheduleActivity (writer);
			});
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromSiblingImpVarImpChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '1: NativeActivityRunner' has the following validation error:   The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfImpChild (concat);
			var wf = GetActWithImpVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromSiblingImpVarPubChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '1: NativeActivityRunner' has the following validation error:   The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.

			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfPubChild (concat);
			var wf = GetActWithImpVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromSiblingPubVarPubChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.

			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfPubChild (concat);
			var wf = GetActWithImpVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromSiblingPubVarImpChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActWithCBResultSetter<String>': The private implementation of activity '2: NativeActWithCBResultSetter<String>' has the following validation error:   The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("","O");
			var vTest = new Variable<string> ();
			var concat = new Concat { String1 = new VariableValue<string> (vStr), String2 = "M" };
			vTest.Default = GetActReturningResultOfImpChild (concat);
			var wf = GetActWithImpVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		#endregion
		#region Access other Variables on the same Activity when Default set to VariableValue
		[Test]
		[Ignore ("LiteralT value retrieved early?")]
		public void Default_PubVarAccessFromVVAsSiblingPubVarDef ()
		{
			//only passes if vStr added to metadata first
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var wf = GetActWithPubVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		//not usual error
		public void Default_PubVarAccessFromVVAsSiblingImpVarDefEx ()
		{
			//Note validation does not catch the error
			//System.InvalidOperationException : Variable '' does not exist in this environment.
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var wf = GetActWithPubVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromVVAsSiblingPubVarDefEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var wf = GetActWithImpVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromVVAsSiblingImpVarDefEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '1: NativeActivityRunner' has the following validation error:   The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var wf = GetActWithImpVarImpVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		#endregion
		#region Access Variables from child Activity's Variable's Default when set to VariableValue
		[Test]
		public void Default_PubVarAccessFromVVAsWhenPubChildsPubVarDef ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActWithPubVarSchedulesPubChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test]
		public void Default_PubVarAccessFromVVAsWhenPubChildsImpVarDef ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithPubVarSchedulesPubChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test]
		public void Default_ImpVarAccessFromVVAsWhenImpChildsPubVarDef ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test]
		public void Default_ImpVarAccessFromVVAsWhenImpChildsImpVarDef ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromVVAsWhenPubChildsImpVarDefEx ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesPubChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromVVAsWhenPubChildsPubVarDefEx ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesPubChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_PubVarAccessFromVVAsWhenImpChildsPubVarDefEx ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActWithPubVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_PubVarAccessFromVVAsWhenImpChildsImpVarDefEx ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new VariableValue<string> (vStr);
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithPubVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("O{0}", Environment.NewLine), "#1");
		}
		#endregion
		[Test]
		[Ignore ("LiteralT value retrieved early - does this happen for InArgument.Expression too?")]
		public void Default_LiteralTValueRetrievedBeforeOthers1 ()
		{
			//since Variables are evaluated in LIFO order, adding vStr before vTest
			//should mean that vStr's value is null when vTest's Default activity
			//retrieves it, but when vStr is a Literal, its value is already there
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var wf = GetActWithPubVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine), "#2");
		}
		[Test]
		public void Default_LiteralTValueRetrievedBeforeOthers_Control ()
		{
			//shows order that non Literal<T> Defaults are processed
			var vStr = new Variable<string> ();
			vStr.Default = GetLiteralWriter ("O");
			var vTest = new Variable<string> ();
			vTest.Default = new ConcatWriter { String1 = vStr, String2 = "M" };
			var writer = new WriteLine { Text = vTest };
			//adding vTest first
			var wf1 = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (vTest);
				metadata.AddVariable (vStr);
				metadata.AddChild (writer);
			}, (context) => {
				context.ScheduleActivity (writer);
			});
			RunAndCompare (wf1, String.Format ("literalMd{0}concatMd{0}literalEx{0}concatEx{0}OM{0}", Environment.NewLine), "#1");

			var wf2 = GetActWithPubVarPubVarWritesLast (vStr, vTest);
			RunAndCompare (wf2, String.Format ("concatMd{0}literalMd{0}concatEx{0}literalEx{0}M{0}", Environment.NewLine), "#2");
		}
		#region Access Variable from childs Variable.Default's args
		[Test]
		public void Default_PubVarAccessFromPubChildsPubVar ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };

			var wf = new Sequence { 
				Variables = { vStr },
				Activities =  { 
					new Sequence { 
						Variables = { vTest },
						Activities = { new WriteLine { Text = vTest } }
					}
				}
			};

			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromPubChildsPubVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another 
			//location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesPubChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromPubChildsImpVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '2: NativeActivityRunner' has the following validation error:   
			//The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesPubChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test]
		public void Default_PubVarAccessFromPubChildsImpVar ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithPubVarSchedulesPubChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_PubVarAccessFromImpChildsPubVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '1: NativeActivityRunner' has the following validation error:   
			//The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActWithPubVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test]
		public void Default_ImpVarAccessFromImpChildsPubVar ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test]
		public void Default_ImpVarAccessFromImpChildsImpVar ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithImpVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_PubVarAccessFromImpChildsImpVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '1: NativeActivityRunner' has the following validation error:   
			//The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActWithPubVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		#endregion
		#region Access Variables from Variables on grandchilds Default
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Default_ImpVarAccessFromImpGrandChildsImpVarEx ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var childChild = GetActWithImpVarWrites (vTest);
			var child = GetActSchedulesImpChild (childChild);
			var wf = GetActWithImpVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		//FIXME: havnt tested all grandchild scenarios
		[Test]
		public void Default_ImpVarAccessFromImpChildsPubChildsImpVar ()
		{
			var vStr = new Variable<string> ("", "O");
			var vTest = new Variable<string> ();
			vTest.Default = new Concat { String1 = vStr, String2 = "M" };
			var childChild = GetActWithImpVarWrites (vTest);
			var child = GetActSchedulesPubChild (childChild);
			var wf = GetActWithImpVarSchedulesImpChild (vStr, child);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine));
		}
		#endregion
		[Test]
		public void VarDefault_OrderExecuted ()
		{
			//seems Argument.Expressions are scheduled before variable.Defaults
			//Variable defaults executed in reverse order they are added to metadata,
			//since LIFO, means they are scheduled in order added to metadata
			var wf = new VarDefAndArgEvalOrder ();
			wf.PubVar1 = new Variable<string> ();
			wf.PubVar1.Default = new VarDefAndArgEvalOrder.GetString ("PubVar1");
			wf.ImpVar1 = new Variable<string> ();
			wf.ImpVar1.Default = new VarDefAndArgEvalOrder.GetString ("ImpVar1");
			wf.PubVar2 = new Variable<string> ();
			wf.PubVar2.Default = new VarDefAndArgEvalOrder.GetString ("PubVar2");
			wf.ImpVar2 = new Variable<string> ();
			wf.ImpVar2.Default = new VarDefAndArgEvalOrder.GetString ("ImpVar2");
			var ap = new WFAppWrapper (wf);
			ap.Run ();
			Assert.AreEqual (String.Format (
				"PubVar2{0}PubVar1{0}ImpVar2{0}ImpVar1{0}ExEvExecute{0}", Environment.NewLine),
			                 ap.ConsoleOut);
		}
		#endregion
		#region Tests for Var access from Activities being used as Argument.Expression
		#region Access Variable from Argument.Expression's arg
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_ImpVarAccessEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.

			var impVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			arg.Expression = new Concat { String1 = impVar, String2 = "M" };
			var wf1 = new NativeActivityRunnerTakesArg ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, null, arg);
			RunAndCompare (wf1, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_PubVarAccessEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be 
			//another location reference with the same name that is visible at this scope, but it does not reference the same location.
			var pubVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			arg.Expression = new Concat { String1 = pubVar, String2 = "M" };
			var wf1 = new NativeActivityRunnerTakesArg ((metadata) => {
				metadata.AddVariable (pubVar);
			}, null, arg);
			WorkflowInvoker.Invoke (wf1);
		}
		#endregion
		#region Tests for var access from Argument.Expressions child's args
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_ImpVarAccessFromArgsPubChildEx ()
		{
			var impVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			var concat = new Concat { String1 = impVar, String2 = "M" };
			arg.Expression = GetActReturningResultOfPubChild (concat);

			var wf1 = new NativeActivityRunnerTakesArg ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, null, arg);
			RunAndCompare (wf1, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_ImpVarAccessFromArgsImpChildEx ()
		{
			var impVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			var concat = new Concat { String1 = impVar, String2 = "M" };
			arg.Expression = GetActReturningResultOfImpChild (concat);

			var wf1 = new NativeActivityRunnerTakesArg ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, null, arg);
			RunAndCompare (wf1, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_PubVarAccessFromArgsPubChildEx ()
		{
			var pubVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			var concat = new Concat { String1 = pubVar, String2 = "M" };
			arg.Expression = GetActReturningResultOfPubChild (concat);

			var wf1 = new NativeActivityRunnerTakesArg ((metadata) => {
				metadata.AddVariable (pubVar);
			}, null, arg);
			RunAndCompare (wf1, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_PubVarAccessFromArgsImpChildEx ()
		{
			var pubVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			var concat = new Concat { String1 = pubVar, String2 = "M" };
			arg.Expression = GetActReturningResultOfImpChild (concat);

			var wf1 = new NativeActivityRunnerTakesArg ((metadata) => {
				metadata.AddVariable (pubVar);
			}, null, arg);
			RunAndCompare (wf1, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		#endregion
		#region Access Variable on parent from an Argument.Expression's arg on child
		[Test]
		public void Expression_ImpVarAccessFromImpChildsArg ()
		{
			var impVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			arg.Expression = new Concat { String1 = impVar, String2 = "M" };
			var impChild = GetActWithArgWrites (arg);
			var wf = GetActWithImpVarSchedulesImpChild (impVar, impChild);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_ImpVarAccessFromPubChildsArgEx ()
		{
			var impVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			arg.Expression = new Concat { String1 = impVar, String2 = "M" };
			var pubChild = GetActWithArgWrites (arg);
			var wf = GetActWithImpVarSchedulesPubChild (impVar, pubChild);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test]
		public void Expression_PubVarAccessFromPubChildsArg ()
		{
			var pubVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			arg.Expression = new Concat { String1 = pubVar, String2 = "M" };
			var pubChild = GetActWithArgWrites (arg);
			var wf = GetActWithPubVarSchedulesPubChild (pubVar, pubChild);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_PubVarAccessFromImpChildsArgEx ()
		{
			var pubVar = new Variable<string> ("", "O");
			var arg = new InArgument<string> ();
			arg.Expression = new Concat { String1 = pubVar, String2 = "M" };
			var impChild = GetActWithArgWrites (arg);
			var wf = GetActWithPubVarSchedulesImpChild (pubVar, impChild);
			RunAndCompare (wf, String.Format ("OM{0}", Environment.NewLine), "#1");
		}
		#endregion
		#endregion
		#region VariableModifiers
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetReadOnlyVarFromOwnExecuteEx ()
		{
			//System.InvalidOperationException : This location is marked as const, so its value cannot be modified.
			var impVar = new Variable<string> ("","Hello\nWorld");
			impVar.Modifiers = VariableModifiers.ReadOnly;

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, (context) => {
				context.SetValue (impVar, "Another");
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetReadOnlyVarWithNoDefaultValueFromOwnExecuteEx ()
		{
			//System.InvalidOperationException : This location is marked as const, so its value cannot be modified.
			var impVar = new Variable<string> ();
			impVar.Modifiers = VariableModifiers.ReadOnly;

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, (context) => {
				context.SetValue (impVar, "Another");
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation (also demonstrates Resetting variables passed to OutArgs issue)")]
		public void SetReadOnlyVarFromImpChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '1: NativeActivityRunner' has the following validation error:
			//Variable '' is read only and cannot be modified.
			var impVar = new Variable<string> ("","Hello\nWorld");
			impVar.Modifiers = VariableModifiers.ReadOnly;
			var impAssign = new Assign<string> { To = new OutArgument<string> (impVar), Value = "Another" };

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impAssign);
			}, (context) => {
				context.ScheduleActivity (impAssign);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetReadOnlyAndMappedVarFromOwnExecuteEx ()
		{
			//System.InvalidOperationException : This location is marked as const, so its value cannot be modified.
			var impVar = new Variable<string> ("","Hello\nWorld");
			impVar.Modifiers = VariableModifiers.ReadOnly | VariableModifiers.Mapped;

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, (context) => {
				context.SetValue (impVar, "Another");
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetReadOnlyVarViaLocationEx ()
		{
			//System.InvalidOperationException : This location is marked as const, so its value cannot be modified.
			var impVar = new Variable<string> ("","Hello\nWorld");
			impVar.Modifiers = VariableModifiers.ReadOnly;

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
			}, (context) => {
				var loc = context.GetLocation<string> (impVar);
				loc.Value = "AnotherValue";
			});
			WorkflowInvoker.Invoke (wf);
		}
		#endregion
		#region Scheduling VariableValue Directly
		[Test]
		public void VariableValue_AccessImpVarWhileImpChild ()
		{
			var impVar = new Variable<string> ("", "HelloImplementation");
			var vv = new VariableValue<string> (impVar);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (vv);
			}, (context) => {
				context.ScheduleActivity (vv, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		[Test]
		public void VariableValue_AccessPubVarWhilePubChild ()
		{
			var pubVar = new Variable<string> ("", "HelloPub");
			var vv = new VariableValue<string> (pubVar);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddChild (vv);
			}, (context) => {
				context.ScheduleActivity (vv, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, "HelloPub" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableValue_AccessPubVarWhileImpChildEx ()
		{
			var pubVar = new Variable<string> ("", "HelloPub");
			var vv = new VariableValue<string> (pubVar);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddImplementationChild (vv);
			}, (context) => {
				context.ScheduleActivity (vv, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, "HelloPub" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableValue_AccessImpVarWhilePubChildEx ()
		{
			var impVar = new Variable<string> ("", "HelloImp");
			var vv = new VariableValue<string> (impVar);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddChild (vv);
			}, (context) => {
				context.ScheduleActivity (vv, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, "HelloImp" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableValue_AccessImpVarWhilePubChildImpChildEx ()
		{
			var impVar = new Variable<string> ("", "HelloImp");
			var vv = new VariableValue<string> (impVar);
			var pubChild = GetActSchedulesImpExpAndWrites (vv);
			var wf = GetActWithImpVarSchedulesPubChild (impVar, pubChild);
			RunAndCompare (wf, "HelloImp" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableValue_AccessImpVarWhileImpGrandchildEx ()
		{
			var impVar = new Variable<string> ("", "HelloImplementation");
			var vv = new VariableValue<string> (impVar);
			var impChild = GetActSchedulesImpExpAndWrites (vv);
			var wf = GetActWithImpVarSchedulesImpChild (impVar, impChild);
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		[Test]
		public void VariableValue_AccessImpVarWhileImpChildPubChild ()
		{
			var impVar = new Variable<string> ("", "HelloImplementation");
			var vv = new VariableValue<string> (impVar);
			var impChild = GetActSchedulesPubExpAndWrites (vv);
			var wf = GetActWithImpVarSchedulesImpChild (impVar, impChild);
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableValue_AccessPubVarWhileImpGrandchildEx ()
		{
			var pubVar = new Variable<string> ("", "HelloPub");
			var vv = new VariableValue<string> (pubVar);
			var impChild = GetActSchedulesImpExpAndWrites (vv);
			var wf = GetActWithPubVarSchedulesImpChild (pubVar, impChild);
			RunAndCompare (wf, "HelloPub" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableValue_AccessPubVarWhileImpChildPubChildEx ()
		{
			var pubVar = new Variable<string> ("", "HelloPub");
			var vv = new VariableValue<string> (pubVar);
			var impChild = GetActSchedulesPubExpAndWrites (vv);
			var wf = GetActWithPubVarSchedulesImpChild (pubVar, impChild);
			RunAndCompare (wf, "HelloPub" + Environment.NewLine);
		}
		[Test]
		public void VariableValue_AccessPubVarWhilePubGrandchild ()
		{
			var pubVar = new Variable<string> ("", "HelloPub");
			var vv = new VariableValue<string> (pubVar);
			var pubChild = GetActSchedulesPubExpAndWrites (vv);
			var wf = GetActWithPubVarSchedulesPubChild (pubVar, pubChild);
			RunAndCompare (wf, "HelloPub" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableValue_AccessPubVarWhilePubChildImpChildEx ()
		{
			var pubVar = new Variable<string> ("", "HelloPub");
			var vv = new VariableValue<string> (pubVar);
			var pubChild = GetActSchedulesImpExpAndWrites (vv);
			var wf = GetActWithPubVarSchedulesPubChild (pubVar, pubChild);
			RunAndCompare (wf, "HelloPub" + Environment.NewLine);
		}
		#endregion
		//FIXME: minimal VariableReference tests to show scoping same as VariableValue
		[Test]
		public void VariableReference_AccessImpVarWhileImpChild ()
		{
			var impVar = new Variable<string> ("", "HelloImplementation");
			var vr = new VariableReference<string> (impVar);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (vr);
			}, (context) => {
				context.ScheduleActivity (vr, (ctx, compAI, value) => {
					Console.WriteLine (((Location<string>) value).Value);
				});
			});
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableReference_AccessImpVarWhilePubChildEx ()
		{
			var impVar = new Variable<string> ("", "HelloImplementation");
			var vr = new VariableReference<string> (impVar);
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddChild (vr);
			}, (context) => {
				context.ScheduleActivity (vr, (ctx, compAI, value) => {
					Console.WriteLine (((Location<string>) value).Value);
				});
			});
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
	}
}

