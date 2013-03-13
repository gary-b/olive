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
	public class ActivityActionTest {
		public void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Exceptions")]
		public void ActivityAction_ReuseDelegateArgsEx ()
		{
			// FIXME: is this the best place for this test?

			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'Sequence': DelegateArgument '' can not be used on Activity 'Sequence' because it is already in use by Activity 'Sequence'.

			var argStr1 = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string, string> {
				Argument1 = argStr1,
				Argument2 = argStr1,
				Handler = new Sequence {
					Activities = {
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						},
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						},
					}
				}
			};
			
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1", "2");
			});
			RunAndCompare (wf, "1" + Environment.NewLine + 
			               "2" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Exceptions")]
		public void ActivityActionT_NotDeclaredDelegateArgsEx ()
		{
			// FIXME: is this the best place for this test?
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'DelegateArgumentValue<String>': DelegateArgument '' must be included in an activity's ActivityDelegate before it is used.
			// 'DelegateArgumentValue<String>': The referenced DelegateArgument object ('') is not visible at this scope.
			var argStr1 = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string> {
				Handler = new Sequence {
					Activities = {
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						}
					}
				}
			};
			
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1");
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ActivityAction ()
		{
			var writeAction = new ActivityAction {
				Handler = new WriteLine { Text = "Hello\nWorld" }
			};
			
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]	
		public void ActivityActionT ()
		{
			var argStr = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string> {
				Argument = argStr,
				Handler = new WriteLine { Text = new InArgument<string> (argStr)}
			};
			
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1");
			});
			RunAndCompare (wf, "1" + Environment.NewLine);
		}
		[Test]
		public void ActivityActionT1T2 ()
		{
			var argStr1 = new DelegateInArgument<string> ();
			var argStr2 = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string, string> {
				Argument1 = argStr1,
				Argument2 = argStr2,
				Handler = new Sequence {
					Activities = {
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						},
						new WriteLine { 
							Text = new InArgument<string> (argStr2)
						},
					}
				}
			};
						
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1", "2");
			});
			RunAndCompare (wf, "1" + Environment.NewLine + 
			               	"2" + Environment.NewLine);
		}
		[Test]
		public void ActivityActionT1T2T3 ()
		{
			var argStr1 = new DelegateInArgument<string> ();
			var argStr2 = new DelegateInArgument<string> ();
			var argStr3 = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string, string, string> {
				Argument1 = argStr1,
				Argument2 = argStr2,
				Argument3 = argStr3,
				Handler = new Sequence {
					Activities = {
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						},
						new WriteLine { 
							Text = new InArgument<string> (argStr2)
						},
						new WriteLine { 
							Text = new InArgument<string> (argStr3)
						},
					}
				}
			};
			
			var wf = new NativeRunnerMock ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1", "2", "3");
			});
			RunAndCompare (wf, "1" + Environment.NewLine + 
			               		"2" + Environment.NewLine + 
			               		"3" + Environment.NewLine);
		}
		// FIXME: test rest of ActivityAction classes - up to 16 generic params
	}
}

