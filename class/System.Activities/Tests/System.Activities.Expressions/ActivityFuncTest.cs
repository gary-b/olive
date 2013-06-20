using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;
using System.IO;

namespace Tests.System.Activities {
	[TestFixture]
	[Ignore ("ActivityFunc")]
	public class ActivityFuncTest : WFTest {
		//FIXME: ActivityAction Tests uses ScheduleAction to test instead of InvokeAction, 
		// same would make more sense here
		[Test]
		public void ActivityFuncAndInvokeFunc_TResult ()
		{
			var CustomActivity = new ActivityFunc<string> {
				Handler = new Concat {
					String1 = new InArgument<string> ("noArgs"),
				}
			};
			var resultVar = new Variable<string> ();
			var wf = new Sequence {
				Variables = {
					resultVar,
				},
				Activities = {
					new InvokeFunc <string> {
						Func = CustomActivity,
						Result = new OutArgument<string> (resultVar)
					},
					new WriteLine {
						Text = new InArgument<string> (resultVar),
					}
				}
			};
			RunAndCompare (wf, "noArgs" + Environment.NewLine);
		}
		[Test]
		public void ActivityFuncAndInvokeFunc_T1TResult ()
		{
			var inArg1 = new DelegateInArgument<string> ();
			var CustomActivity = new ActivityFunc<string, string> {
				Argument = inArg1,
				Handler = new Concat {
					String1 = new InArgument<string> (inArg1),
				}
			};
			var resultVar = new Variable<string> ();
			var wf = new Sequence {
				Variables = {
					resultVar,
				},
				Activities = {
					new InvokeFunc <string, string> {
						Func = CustomActivity,
						Argument = new InArgument<string> ("1"),
						Result = new OutArgument<string> (resultVar)
					},
					new WriteLine {
						Text = new InArgument<string> (resultVar),
					}
				}
			};
			RunAndCompare (wf, "1" + Environment.NewLine);
		}
		[Test]
		public void ActivityFuncAndInvokeFunc_T1T2TResult ()
		{
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
						Argument1 = new InArgument<string> ("1"),
						Argument2 = new InArgument<string> ("2"),
						Result = new OutArgument<string> (resultVar)
					},
					new WriteLine {
						Text = new InArgument<string> (resultVar),
					}
				}
			};
			RunAndCompare (wf, "12" + Environment.NewLine);
		}
		// FIXME: test rest of ActivityFunc classes - up to 16 generic params
	}
}

