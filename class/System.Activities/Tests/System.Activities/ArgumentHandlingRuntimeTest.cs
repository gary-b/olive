using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	public class ArgumentHandlingRuntimeTest : WFTest {
		class RunnerMockWithArgStr : NativeRunnerMock {
			InArgument<string> ArgStr = new InArgument<string> ("Hello\nWorld");
			public RunnerMockWithArgStr (Action<NativeActivityMetadata> cacheMetadata, Action<NativeActivityContext> execute)
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

			var wf = new RunnerMockWithArgStr ((metadata) => {
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

			var wf = new RunnerMockWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AccessArgFromImpChildsPubGrandchild ()
		{
			var impChild = new Sequence { 
				Activities = { new Sequence { 
						Activities = { new WriteLine { Text = new ArgumentValue<string> ("ArgStr") } } 
					}
				}
			};

			var wf = new RunnerMockWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AccessArgOnSameActivity ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  
			  ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */ 
			var argStr2 = new InArgument<string> (new ArgumentValue<string> ("ArgStr"));
			var impChild = new WriteLine { Text = new ArgumentValue<string> ("argStr2")};

			var wf = new RunnerMockWithArgStr ((metadata) => {
				var rtArgStr2 = new RuntimeArgument ("argStr2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArgStr2);
				metadata.Bind (argStr2, rtArgStr2);
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		//not checking all the great grandchildren
		[Test, /*ExpectedException (typeof (WorkflowException))*/]
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

			var wf = new RunnerMockWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, /*ExpectedException (typeof (WorkflowException))*/]
		public void AccessArgFromPubChildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 * 'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.
			 * ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */
			var pubChild = new WriteLine { Text = new ArgumentValue<string> ("ArgStr") };

			var wf = new RunnerMockWithArgStr ((metadata) => {
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, /*ExpectedException (typeof (WorkflowException))*/]
		public void AccessArgFromPubGrandchildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			   'ArgumentValue<String>': The argument named 'ArgStr' could not be found on the activity owning these private children.  
			   ArgumentReference and ArgumentValue should only be used in the body of an Activity definition.
			 */
			var pubChild = new Sequence { 
				Activities = { new WriteLine { Text = new ArgumentValue<string> ("ArgStr") } } 
			};

			var wf = new RunnerMockWithArgStr ((metadata) => {
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, /*ExpectedException (typeof (WorkflowException))*/]
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

			var wf = new RunnerMockWithArgStr ((metadata) => {
				metadata.AddChild (pubChild);
			}, (context) => {
				context.ScheduleActivity (pubChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, /*ExpectedException (typeof (InvalidWorkflowException))*/]
		public void AccessArgInWrongCaseEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'RunnerMockWithArgStr': The private implementation of activity '1: RunnerMockWithArgStr' has the following validation error:   
			  The argument named 'argStr' could not be found on the activity owning these private children.  ArgumentReference and 
			  ArgumentValue should only be used in the body of an Activity definition.
			 */ 
			var impChild = new WriteLine { Text = new ArgumentValue<string> ("argStr") };

			var wf = new RunnerMockWithArgStr ((metadata) => {
				metadata.AddImplementationChild (impChild);
			}, (context) => {
				context.ScheduleActivity (impChild);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		#endregion

		#region ArgumentReference

		[Test]
		public void ArgumentReferenceTest ()
		{
			var impWrite = new WriteLine { Text = new ArgumentValue<string> ("ArgStr") };
			var impAssign = new Assign<string> { Value = "Changed", To = new ArgumentReference<string> ("ArgStr") };

			var wf = new RunnerMockWithArgStr ((metadata) => {
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
	}
}

