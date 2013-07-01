using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;

namespace Tests.System.Activities {
	// Mostly from Increment4
	[TestFixture]
	public class VariableHandlingRuntimeTest : WFTest {
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
			}, null);
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
			}, null);
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
			}, null);
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
			}, null);
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
			}, null);
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
			}, null);
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
			}, null);
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
			}, null);
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
		#region Tests for Var access from Activities being used as Argument.Expression
		//FIXME: limited grandchild tests?
		[Test]
		public void ExpressionActivity_PubVarAccessFromPubChild ()
		{
			var vStr = new Variable<string> ("","O");
			var appendToVar = new Assign { Value = new InArgument<string> (new Concat {String1 = vStr, String2 = "M"}),
				To = new OutArgument<string> (vStr)
			};
			var wf = new Sequence {
				Variables = { vStr },
				Activities = { 
					new WriteLine { Text = vStr }, 
					appendToVar, 
					new WriteLine { Text = vStr } }
			};
			RunAndCompare (wf, String.Format ("O{0}OM{0}",Environment.NewLine));
		}
		[Test]
		public void ExpressionActivity_PubVarAccessFromPubGrandChild ()
		{
			var vStr = new Variable<string> ("","O");
			var appendToVar = new Assign { Value = new InArgument<string> (new Concat {String1 = vStr, String2 = "M"}),
				To = new OutArgument<string> (vStr)
			};
			var wf = new Sequence {
				Variables = { vStr },
				Activities = { 
					new WriteLine { Text = vStr }, 
					new Sequence {Activities = { appendToVar }}, 
					new WriteLine { Text = vStr } }
			};
			RunAndCompare (wf, String.Format ("O{0}OM{0}",Environment.NewLine));
		}
		[Test]
		public void ExpressionActivity_ImpVarAccessFromImpChild ()
		{
			var vStr = new Variable<string> ("","O");
			var writeLine = new WriteLine { Text = vStr };
			var appendToVar = new Assign { Value = new InArgument<string> (new Concat {String1 = vStr, String2 = "M"}),
				To = new OutArgument<string> (vStr)
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationChild (appendToVar);
			}, (context) => {
				context.ScheduleActivity (writeLine);
				context.ScheduleActivity (appendToVar);
				context.ScheduleActivity (writeLine);
			});
			RunAndCompare (wf, String.Format ("O{0}OM{0}",Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ExpressionActivity_ImpVarAccessFromPubChildEx ()
		{
			var vStr = new Variable<string> ("","O");
			var appendToVar = new Assign { Value = new InArgument<string> (new Concat {String1 = vStr, String2 = "M"}),
				To = new OutArgument<string> (vStr)
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
				metadata.AddChild (appendToVar);
			}, (context) => {
				context.ScheduleActivity (appendToVar);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ExpressionActivity_PubVarAccessFromImpChildEx ()
		{
			var vStr = new Variable<string> ("","O");
			var appendToVar = new Assign { Value = new InArgument<string> (new Concat {String1 = vStr, String2 = "M"}),
				To = new OutArgument<string> (vStr)
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (vStr);
				metadata.AddImplementationChild (appendToVar);
			}, (context) => {
				context.ScheduleActivity (appendToVar);
			});
			WorkflowInvoker.Invoke (wf);
		}
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
	}
}

