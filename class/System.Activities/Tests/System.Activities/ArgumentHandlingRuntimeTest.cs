using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	public class ArgumentHandlingRuntimeTest : WFTest {
		class NativeRunnerWithArgStr : NativeActivityRunner {
			InArgument<string> ArgStr = new InArgument<string> ("Hello\nWorld");
			public NativeRunnerWithArgStr (Action<NativeActivityMetadata> cacheMetadata, Action<NativeActivityContext> execute)
																					:base (cacheMetadata, execute)
			{
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				var rtStr = new RuntimeArgument ("ArgStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtStr);
				metadata.Bind (ArgStr, rtStr);
				base.CacheMetadata (metadata); //allow cacheMetadata delegate provided by user to be run
			}
		}
		#region ArgumentValue
		[Test]
		public void AccessArgFromImpChild ()
		{
			var impChild = new WriteLine { Text = new ArgumentValue<string> ("ArgStr") };

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AccessArgFromImpChildsPubChild ()
		{
			var impChild = new Sequence { 
				Activities = { new WriteLine { Text = new ArgumentValue<string> ("ArgStr") } } 
			};

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		//not checking all the great grandchildren
		[Test]
		public void AccessArgFromImpChildsPubGrandchild ()
		{
			var impChild = new Sequence { 
				Activities = { new Sequence { 
						Activities = { new WriteLine { Text = new ArgumentValue<string> ("ArgStr") } } 
					}
				}
			};

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessArgOnSameActivityEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  
			  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */ 
			var argStr2 = new InArgument<string> (new ArgumentValue<string> ("ArgStr"));
			var impChild = new WriteLine { Text = new ArgumentValue<string> ("argStr2")};

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
			var impChild = new WriteLineHolder { 
				ImplementationWriteLine = new WriteLine { Text = new ArgumentValue<string> ("ArgStr") } 
			};

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
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
			var pubChild = new WriteLine { Text = new ArgumentValue<string> ("ArgStr") };

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
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
			var pubChild = new Sequence { 
				Activities = { new WriteLine { Text = new ArgumentValue<string> ("ArgStr") } } 
			};

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
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
			var pubChild = new WriteLineHolder { 
				ImplementationWriteLine = new WriteLine { Text = new ArgumentValue<string> ("ArgStr") } 
			};

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
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
			var impChild = new WriteLine { Text = new ArgumentValue<string> ("argStr") };

			var wf = new NativeRunnerWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		#endregion

		#region ArgumentReference
		[Test]
		public void UpdateArgFromImpChildren ()
		{
			var impWrite = new WriteLine { Text = new ArgumentValue<string> ("ArgStr") };
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

			var wf = new NativeActivityRunner((metadata) => {
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

	}
}

