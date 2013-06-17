using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.IO;
using System.Activities.Statements;

namespace Tests.System.Activities.Statements {
	[TestFixture]
	class AssignTest : WFTest {
		[Test]
		public void Execute ()
		{
			var impVar = new Variable<string> ("", "DefaultVar"); 
			var assign = new Assign {
				To = new OutArgument<string> (impVar),
				Value = new InArgument<string> ("NewValue")
			};
			var write = new WriteLine {
				Text = impVar
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (assign);
				metadata.AddImplementationChild (write);
			}, (context) => {
				Assert.AreEqual ("DefaultVar", impVar.Get (context));
				context.ScheduleActivity (write);
				context.ScheduleActivity (assign);
				context.ScheduleActivity (write);
			});
			RunAndCompare (wf, String.Format ("DefaultVar{0}NewValue{0}", Environment.NewLine));
		}
		//FIXME: ******When implementing validation sort these test out*********

		//FIXME: add tests for when different generic type args used for To and Value Arguments
		[Test]
		public void To_EmptyWhileRoot_NoError ()
		{
			//FIXME: When parameters arnt supplied errors are not consistent on .net, ie, fail to supply both params 
			//to assign while its the root workflow and it says Value param is required. Supply this value
			//param and the workflow will run. When its not root passing assign no params will return
			//an error advising both params are required.

			var assign = new Assign {
				To = new OutArgument<string> (),
				Value = new InArgument<string> ("NewValue")
			};
			WorkflowInvoker.Invoke (assign);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void To_EmptyWhileNotRootEx () 
		{
			// System.Activities.InvalidWorkflowException : The following errors were 
			// encountered while processing the workflow tree:
			// 'Assign': Value for a required activity argument 'To' was not supplied.
			var wf = new Sequence {
				Activities = { 
					new Assign {
						To = new OutArgument<string> (),
						Value = new InArgument<string> ("NewValue")
					},
				}
			};
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Value_To_EmptyWhileNotRootEx () 
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'Assign': Value for a required activity argument 'Value' was not supplied.
			//'Assign': Value for a required activity argument 'To' was not supplied.
			var wf = new Sequence {
				Activities = { 
					new Assign{
						To = new OutArgument<string>(),
						Value = new InArgument<string>()
					},
				}
			};
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Value_EmptyWhileNotRootEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			// Value for a required activity argument 'Value' was not supplied.
			var impVar = new Variable<string> ("", "DefaultVar"); 
			var assign = new Assign {
				To = new OutArgument<string> (impVar),
				Value = new InArgument<string> ()
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (assign);
			}, (context) => {
				Assert.AreEqual ("DefaultVar", impVar.Get (context));
				context.ScheduleActivity (assign);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (NullReferenceException))]
		public void To_NullWhileRootEx ()
		{
			//why nre?
			var assign = new Assign {
				Value = new InArgument<string> ("NewValue")
			};
			Assert.IsNull (assign.To);
			WorkflowInvoker.Invoke (assign);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("Validation")]
		public void Value_NullWhileRootEx ()
		{
			// Different exceptions are raised depending
			// on whether the invalid argument is supplied on the root / non root activity

			//System.ArgumentException : The root activity's argument settings are incorrect.  Either fix the workflow definition or supply input values to fix these errors:
			//'Assign': Value for a required activity argument 'Value' was not supplied.
			//Parameter name: program

			var assign = new Assign {
				To = new OutArgument<string> (),
			};
			Assert.IsNull (assign.Value);
			WorkflowInvoker.Invoke (assign);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Value_NullWhileNotRootEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			// Value for a required activity argument 'Value' was not supplied.
			var impVar = new Variable<string> ("", "DefaultVar"); 
			var assign = new Assign {
				To = new OutArgument<string> (impVar),
			};
			Assert.IsNull (assign.Value);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (assign);
			}, (context) => {
				Assert.AreEqual ("DefaultVar", impVar.Get (context));
				context.ScheduleActivity (assign);
			});
			WorkflowInvoker.Invoke (wf);
		}
	}
	[TestFixture]
	class AssignT_Test : WFTest {
		[Test]
		public void Execute ()
		{
			var impVar = new Variable<string> ("", "DefaultVar"); 
			var assign = new Assign<string> {
				To = new OutArgument<string> (impVar),
				Value = new InArgument<string> ("NewValue")
			};
			var write = new WriteLine {
				Text = impVar
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (impVar);
				metadata.AddImplementationChild (assign);
				metadata.AddImplementationChild (write);
			}, (context) => {
				Assert.AreEqual ("DefaultVar", impVar.Get (context));
				context.ScheduleActivity (write);
				context.ScheduleActivity (assign);
				context.ScheduleActivity (write);
			});
			RunAndCompare (wf, String.Format ("DefaultVar{0}NewValue{0}", Environment.NewLine));
		}
		//FIXME: ******When implementing validation perform same tests as performed on Assign*********
	}
}
