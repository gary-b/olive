using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ArgumentHandlingRuntimeTest : WFTestHelper {
		Activity<string> GetMetadataWriter (string name)
		{
			return new CodeActivityTRunner<string> ((metadata)=> { 
				Console.WriteLine (name); 
			}, (context) => {
				return name;
			});
		}
		[Test]
		public void CacheMetadata_Expression_OrderCalled_ActivityIdGeneration ()
		{
			var arg1 = new InArgument<string> (GetMetadataWriter ("arg1"));
			var arg2 = new InArgument<string> (GetMetadataWriter ("arg2"));
			var wf = new NativeActivityRunner ((metadata) => {  
				var rtArg1 = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg1);
				var rtArg2 = new RuntimeArgument ("arg2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg2);
				metadata.Bind (arg2, rtArg2);
				metadata.Bind (arg1, rtArg1);
			}, null);

			var app = new WFAppWrapper (wf);
			app.Run ();
			//Test Order Called
			var split = app.ConsoleOut.Split (new string [] { Environment.NewLine }, StringSplitOptions.None);
			//remove trailing empty string
			var actualOrder = new string [split.Length - 1];
			for (int i = 0; i < split.Length - 1; i++)
				actualOrder [i] = split [i];
			var expected = new string [] {
				"arg1",	"arg2"
			};
			Assert.AreEqual (expected, actualOrder);

			// Test Activity Ids Generated
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", arg1.Expression.Id);
			Assert.AreEqual ("3", arg2.Expression.Id);
		}
		#region ArgumentValue as Argument.Expression
		[Test]
		public void AccessArgFromImpChild ()
		{
			var impChild = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AccessArgFromImpChildsPubChild ()
		{
			var writeLine = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var impChild = GetActSchedulesPubChild (writeLine);
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		//not checking all the great grandchildren
		[Test]
		public void AccessArgFromImpChildsPubGrandchild ()
		{
			var writeLine = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var impChildPubChild = GetActSchedulesPubChild (writeLine);
			var impChild = GetActSchedulesPubChild (impChildPubChild);
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessArgOnSameActivity_FromArgExpressionEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  
			  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */ 
			var argStr2 = new InArgument<string> (new ArgumentValue<string> ("ArgStr"));
			var impChild = GetWriteLine (new ArgumentValue<string> ("argStr2"));

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				var rtArgStr2 = new RuntimeArgument ("argStr2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr2);
				metadata.Bind (argStr2, rtArgStr2);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessArgFromImpGrandchildEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'RunnerMockWithArgStr': The private implementation of activity '1: RunnerMockWithArgStr' has the following validation error:
			  The argument named 'ArgStr' could not be found on the activity owning these private children.  
			  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */ 

			var writeLine = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var impChild = GetActSchedulesImpChild (writeLine);
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessArgFromPubChildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 * 'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.
			 * ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */
			var pubChild = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var wf = GetActHasArgSchedulesPubChild (pubChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessArgFromPubGrandchildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			   'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  
			   ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */
			var writeLine = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var pubChild = GetActSchedulesPubChild (writeLine);
			var wf = GetActHasArgSchedulesPubChild (pubChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessArgFromPubChildImpChildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			   'WriteLineHolder': The private implementation of activity '3: WriteLineHolder' has the following validation error:   
			   The argument named 'ArgStr' could not be found on the activity owning these private children.  
			   ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */ 
			var writeLine = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var pubChild = GetActSchedulesImpChild (writeLine);

			var wf = GetActHasArgSchedulesPubChild (pubChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessArgInWrongCaseEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'RunnerMockWithArgStr': The private implementation of activity '1: RunnerMockWithArgStr' has the following validation error:   
			  The argument named 'argStr' could not be found on the activity owning these private children.  ArgumentReference and 
			  ArgumentValue should only be used in the body of an Activity definition.
			 */ 
			var impChild = GetWriteLine (new ArgumentValue<string> ("argStr"));

			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		#endregion
		#region Scheduling ArgumentValue Directly
		Activity GetActHasArgSchedulesImpChild (Activity child)
		{
			return new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		Activity GetActHasArgSchedulesPubChild (Activity child)
		{
			return new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity (child);
			});
		}
		Activity GetActHasArgSchedulesPubExpAndWrites (Activity<string> exp)
		{
			return new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddChild (exp);
			}, (context) => {
				context.ScheduleActivity (exp, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
		}
		Activity GetActHasArgSchedulesImpExpAndWrites (Activity<string> exp)
		{
			return new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (exp);
			}, (context) => {
				context.ScheduleActivity (exp, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
		}
		Activity GetActHasArgWithPubVarWrites (Variable<string> varToWrite)
		{
			var writeLine = new WriteLine { Text = varToWrite };
			return new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddVariable (varToWrite);
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
		}
		Activity GetActHasArgWithImpVarWrites (Variable<string> varToWrite)
		{
			return new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationVariable (varToWrite);
			}, (context) => {
				Console.WriteLine (varToWrite.Get (context));
			});
		}
		[Test]
		public void ArgumentValue_AccessArgWhileNormalImpChild ()
		{
			var av = new ArgumentValue<string> ("ArgStr");
			var wf = GetActHasArgSchedulesImpExpAndWrites (av);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void ArgumentValue_AccessArgWhileNormalImpChildPubChild ()
		{
			var av = new ArgumentValue<string> ("ArgStr");
			var impChild = GetActSchedulesPubExpAndWrites (av);
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void ArgumentValue_AccessArgWhileNormalImpChildPubGrandchild ()
		{
			var av = new ArgumentValue<string> ("ArgStr");
			var grandChild = GetActSchedulesPubExpAndWrites (av);
			var impChild = new Sequence { Activities = { grandChild  } };
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ArgumentValue_AccessArgWhileNormalImpGrandChildEx ()
		{
			var av = new ArgumentValue<string> ("ArgStr");
			var impChild = GetActSchedulesImpExpAndWrites (av);
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ArgumentValue_AccessArgWhileNormalPubChildEx ()
		{
			var av = new ArgumentValue<string> ("ArgStr");
			var wf = GetActHasArgSchedulesPubExpAndWrites (av);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ArgumentValue_AccessArgWhileNormalPubChildImpChildEx ()
		{
			var av = new ArgumentValue<string> ("ArgStr");
			var pubChild = GetActSchedulesImpExpAndWrites (av);
			var wf = GetActHasArgSchedulesPubChild  (pubChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ArgumentValue_AccessArgWhileNormalImpChildPubChildImpChildEx ()
		{
			var av = new ArgumentValue<string> ("ArgStr");
			var impChildPubChild = GetActSchedulesImpExpAndWrites (av);
			var impChild = GetActSchedulesPubChild (impChildPubChild);
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ArgumentValue_AccessArgWhileNormalPubGrandChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.
			//ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var av = new ArgumentValue<string> ("ArgStr");
			var pubChild = GetActSchedulesPubExpAndWrites (av);
			var wf = GetActHasArgSchedulesPubChild (pubChild);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		#endregion

		#region ArgumentReference
		//FIXME: minimal testing done to show scoping same as ArgumentValue
		[Test]
		public void ArgumentReference_AccessArgWhileNormalImpChild ()
		{
			var ar = new ArgumentReference<string> ("ArgStr");
			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (ar);
			}, (context) => {
				context.ScheduleActivity (ar, (ctx, compAI, value) => {
					Console.WriteLine (((Location<string>)value).Value);
				});
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ArgumentReference_AccessArgWhileNormalPubChildEx ()
		{
			var ar = new ArgumentReference<string> ("ArgStr");
			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddChild (ar);
			}, (context) => {
				context.ScheduleActivity (ar, (ctx, compAI, value) => {
					Console.WriteLine (((Location<string>)value).Value);
				});
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void UpdateArgFromImpChildren ()
		{
			var impWrite = GetWriteLine (new ArgumentValue<string> ("ArgStr"));
			var impAssign = new Assign<string> { 
				Value = "Changed", 
				To = new OutArgument<string> (new ArgumentReference<string> ("ArgStr")) 
			};

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impWrite);
				metadata.AddImplementationChild (impAssign);
			}, (context) => {
				context.ScheduleActivity (impWrite);
				context.ScheduleActivity (impAssign);
				context.ScheduleActivity (impWrite);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine + "Changed" + Environment.NewLine );
		}
		#endregion

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void AccessArgFromImpChildsExecuteEx ()
		{
			/*System.InvalidOperationException : An Activity can only get the location of arguments which it owns.  Activity 
			 * 'NativeRunnerMock' is trying to get the location of argument 'argStr' which is owned by activity 'NativeRunnerMock'.
			 */ 
			var argStr = new InArgument<string> ("Hello\nWorld");

			var impChild = new NativeActivityRunner (null, (context) => {
				Console.WriteLine (argStr.Get (context));
			});

			var wf = new NativeActivityRunner ((metadata) => {
				var rtArgStr = new RuntimeArgument ("argStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr);
				metadata.Bind (argStr, rtArgStr);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void AccessArgFromPubChildsExecuteEx ()
		{
			/*System.InvalidOperationException : An Activity can only get the location of arguments which it owns.  Activity 
			 * 'NativeRunnerMock' is trying to get the location of argument 'argStr' which is owned by activity 'NativeRunnerMock'.
			 */ 
			var argStr = new InArgument<string> ("Hello\nWorld");

			var pubChild = new NativeActivityRunner (null, (context) => {
				Console.WriteLine (argStr.Get (context));
			});

			var wf = new NativeActivityRunner ((metadata) => {
				var rtArgStr = new RuntimeArgument ("argStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr);
				metadata.Bind (argStr, rtArgStr);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}

		#region tests show how arg access works when Argument.Expression not set to a ArgumentValue/Reference
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void AccessArgFromOwnArgThroughArgActivityTCtorEx ()
		{
			//System.InvalidOperationException : An Activity can only get the location of arguments which it owns.  Activity 
			// 'CodeTRunnerMock<String>' is trying to get the location of argument 'argStr' which is owned by activity 'NativeRunnerMock'.
			var argStr = new InArgument<string> ("Hello\nWorld");

			var expression = new CodeActivityTRunner<string> (null, (context) => {
				return argStr.Get (context);
			});

			var argStr2 = new InArgument<string> (expression);

			var wf = new NativeActivityRunner ((metadata) => {
				var rtArgStr = new RuntimeArgument ("argStr", typeof (string), ArgumentDirection.In);
				var rtArgStr2 = new RuntimeArgument ("argStr2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr);
				metadata.Bind (argStr, rtArgStr);

				metadata.AddArgument (rtArgStr2);
				metadata.Bind (argStr2, rtArgStr2);
			}, (context) => {
				Console.WriteLine (argStr2.Get (context));
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void AccessArgFromImpChildThroughArgActivityTCtorEx ()
		{
			/*System.InvalidOperationException : An Activity can only get the location of arguments which it owns.  Activity 
			 * 'CodeTRunnerMock<String>' is trying to get the location of argument 'argStr' which is owned by activity 'NativeRunnerMock'.
			*/
			var argStr = new InArgument<string> ("Hello\nWorld");

			var expression = new CodeActivityTRunner<string> (null, (context) => {
				return argStr.Get (context);
			});

			var impChild = new WriteLine { Text = new InArgument<string> (expression) };

			var wf = new NativeActivityRunner ((metadata) => {
				var rtArgStr = new RuntimeArgument ("argStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr);
				metadata.Bind (argStr, rtArgStr);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		[Ignore ("Expressions")]
		public void AccessArgFromImpChildThroughArgExpressionTCtor ()
		{
			var argStr = new InArgument<string> ("Hello\nWorld");

			var impChild = new WriteLine { Text = new InArgument<string> ((context) => argStr.Get (context)) };

			var wf = new NativeActivityRunner ((metadata) => {
				var rtArgStr = new RuntimeArgument ("argStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr);
				metadata.Bind (argStr, rtArgStr);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		#endregion
		#region Expression Execution Characteristics
		[Test]
		public void CanNotScheduleArgumentExpressionFromSelf ()
		{
			//System.InvalidOperationException : An Activity can only schedule its direct children. 
			//Activity 'impChild' is attempting to schedule 'VariableValue<String>' which is a child of activity 'impChild'.
			Exception ex = null;
			var impVar = new Variable<string> ("", "");
			var arg = new InArgument<string> (impVar);
			var impChild = new NativeActivityRunner ((metadata) => {
				var rtArg = new RuntimeArgument ("arg", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg);
				metadata.Bind (arg, rtArg);
			}, (context) => {
				try {
					context.ScheduleActivity (
						((VariableValue<string>)(arg.Expression)), 
						(ctx, compAI, value) => {
						Console.WriteLine (value);
					});
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			impChild.DisplayName = "impChild";
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			wf.DisplayName = "root";
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		[Test]
		public void CanNotScheduleArgumentExpressionFromParent ()
		{
			//System.InvalidOperationException : An Activity can only schedule its direct children. 
			//Activity 'NativeActivityRunner' is attempting to schedule 'VariableValue<String>' which 
			//is a child of activity 'WriteLine'.
			Exception ex = null;
			var pubVar = new Variable<string> ("", "");
			var writeLine = new WriteLine { Text = pubVar };
			var pubChild = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context) => {
				try {
					context.ScheduleActivity (
						((VariableValue<string>)(writeLine.Text.Expression)), 
						(ctx, compAI, value) => {
						Console.WriteLine (value);
					});
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddVariable (pubVar);
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), ex);
		}
		#endregion
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
		#region Test Access to Arguments From Variable.Default
		#region Access to Arguments when Variable.Default set to ArgumentValue
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromAVWhilePubVarDefaultEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.
			//ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.

			var vTest = new Variable<string> ();
			vTest.Default = new ArgumentValue<string> ("ArgStr");
			var wf = GetActHasArgWithPubVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test]
		public void VariableDefault_AccessArgFromAVWhileImpVarDefault ()
		{
			var vTest = new Variable<string> ();
			vTest.Default = new ArgumentValue<string> ("ArgStr");
			var wf = GetActHasArgWithImpVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine), "#1");
		}
		#endregion
		#region Access to Arguments when childs Variable.Default set to ArgumentValue
		[Test]
		public void VariableDefault_AccessArgFromAVWhileImpChildsPubVarDefault ()
		{
			var vTest = new Variable<string> ();
			vTest.Default = new ArgumentValue<string> ("ArgStr");
			var impChild = GetActWithPubVarWrites (vTest);
			var wf = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromAVWhileImpChildsImpVarDefaultEx ()
		{
			var vTest = new Variable<string> ();
			vTest.Default = new ArgumentValue<string> ("ArgStr");
			var impChild = GetActWithImpVarWrites (vTest);
			var wf1 = GetActHasArgSchedulesImpChild (impChild);
			RunAndCompare (wf1, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromAVWhilePubChildsPubVarDefaultEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.

			var vTest = new Variable<string> ();
			vTest.Default = new ArgumentValue<string> ("ArgStr");
			var child = GetActWithPubVarWrites (vTest);
			var wf1 = GetActHasArgSchedulesPubChild (child);
			RunAndCompare (wf1, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromAVWhilePubChildsImpVarDefaultEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '3: NativeActivityRunner' has the following validation error:   The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var vTest = new Variable<string> ();
			vTest.Default = new ArgumentValue<string> ("ArgStr");
			var child = GetActWithImpVarWrites (vTest);
			var wf1 = GetActHasArgSchedulesPubChild (child);
			RunAndCompare (wf1, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		#endregion
		#region Access to Arguments from Variable.Default's args
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromPubVarEx ()
		{
			var vTest = new Variable<string> ();
			vTest.Default = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var wf = GetActHasArgWithPubVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		[Test]
		public void VariableDefault_AccessArgFromImpVar ()
		{
			var vTest = new Variable<string> ();
			vTest.Default = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var wf = GetActHasArgWithImpVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		#endregion
		#region Access to Arguments from childs Variable.Default's args
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromPubChildsPubVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.

			var vTest = new Variable<string> ();
			vTest.Default = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActHasArgSchedulesPubChild (child);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromPubChildsImpVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The private implementation of activity '3: NativeActivityRunner' has the following validation error:   The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.

			var vTest = new Variable<string> ();
			vTest.Default = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActHasArgSchedulesPubChild (child);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromImpChildsImpVarEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerWithArgStr': The private implementation of activity '1: NativeRunnerWithArgStr' has the following validation error:   The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var vTest = new Variable<string> ();
			vTest.Default = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var child = GetActWithImpVarWrites (vTest);
			var wf = GetActHasArgSchedulesImpChild (child);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		[Test]
		public void VariableDefault_AccessArgFromImpChildsPubVar ()
		{
			var vTest = new Variable<string> ();
			vTest.Default = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var child = GetActWithPubVarWrites (vTest);
			var wf = GetActHasArgSchedulesImpChild (child);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		#endregion
		#region Access to Arguments from Variable.Default's child's args
		[Test]
		public void VariableDefault_AccessArgFromImpVarPubChild ()
		{
			var vTest = new Variable<string> ();
			var concat = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			vTest.Default = GetActReturningResultOfPubChild (concat);
			var wf = GetActHasArgWithImpVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromImpVarImpChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerWithArgStr': The private implementation of activity '1: NativeRunnerWithArgStr' has the following validation error:   The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var vTest = new Variable<string> ();
			var concat = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			vTest.Default = GetActReturningResultOfImpChild (concat);
			var wf = GetActHasArgWithImpVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromPubVarPubChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var vTest = new Variable<string> ();
			var concat = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			vTest.Default = GetActReturningResultOfPubChild (concat);
			var wf = GetActHasArgWithPubVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void VariableDefault_AccessArgFromPubVarImpChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActWithCBResultSetter<String>': The private implementation of activity '2: NativeActWithCBResultSetter<String>' has the following validation error:   The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var vTest = new Variable<string> ();
			var concat = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			vTest.Default = GetActReturningResultOfImpChild (concat);
			var wf = GetActHasArgWithPubVarWrites (vTest);
			RunAndCompare (wf, String.Format ("Hello\nWorld2{0}", Environment.NewLine));
		}
		#endregion
		#endregion
		#region ArgumentValue Being used on arguments of Expression activity and its children
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_AccessArgEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var argStr2 = new InArgument<string> ();
			argStr2.Expression = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				var rtArgStr2 = new RuntimeArgument ("argStr2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr2);
				metadata.Bind (argStr2, rtArgStr2);
			}, (context) => {
				Console.WriteLine (argStr2.Get (context));
			});
			RunAndCompare (wf, "Hello\nWorld2" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_AccessArgFromPubChildsArgExpEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			var arg = new InArgument<string> ();
			arg.Expression = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var child = GetActWithArgWrites (arg);
			var wf = GetActHasArgSchedulesPubChild (child);
			RunAndCompare (wf, "Hello\nWorld2" + Environment.NewLine);
		}
		[Test]
		public void Expression_AccessArgFromImpChildsArgExp ()
		{
			var arg = new InArgument<string> ();
			arg.Expression = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			var child = GetActWithArgWrites (arg);
			var wf = GetActHasArgSchedulesImpChild (child);
			RunAndCompare (wf, "Hello\nWorld2" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_AccessArgFromArgExpPubChildEx ()
		{
			var argStr2 = new InArgument<string> ();
			var concat = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			argStr2.Expression = GetActReturningResultOfPubChild (concat);

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				var rtArgStr2 = new RuntimeArgument ("argStr2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr2);
				metadata.Bind (argStr2, rtArgStr2);
			}, (context) => {
				Console.WriteLine (argStr2.Get (context));
			});
			RunAndCompare (wf, "Hello\nWorld2" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Expression_AccessArgFromArgExpImpChildEx ()
		{
			var argStr2 = new InArgument<string> ();
			var concat = GetConcat (new ArgumentValue<string> ("ArgStr"), "2");
			argStr2.Expression = GetActReturningResultOfImpChild (concat);

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				var rtArgStr2 = new RuntimeArgument ("argStr2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr2);
				metadata.Bind (argStr2, rtArgStr2);
			}, (context) => {
				Console.WriteLine (argStr2.Get (context));
			});
			RunAndCompare (wf, "Hello\nWorld2" + Environment.NewLine);
		}
		#endregion
	}
}

