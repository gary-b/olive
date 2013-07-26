using System;
using NUnit.Framework;
using System.Activities;

namespace Tests.System.Activities {
	public partial class ActivityDelegateTestHelper: WFTestHelper {
		protected string ExpectedConcatManyConsoleOutput (int noParams)
		{
			string expected = ExpectedConcatManyOutput (noParams);
			expected += Environment.NewLine;
			return expected;
		}
		protected string ExpectedConcatManyOutput (int noParams)
		{
			// this presumes that each argument is passed "1", "2", "3" etc
			// the activityfund which takes no args is hard coded to return "noArgs"
			string expected = String.Empty;
			if (noParams == 0) {
				expected = "noArgs";
			}
			else {
				for (int i = 1; i < (noParams + 1); i++)
					expected += i;
			}
			return expected;
		}
		protected ActivityFunc<string> GetActivityFuncConcatMany0 ()
		{
			var delOut = new DelegateOutArgument<string> ();
			return new ActivityFunc<string> {
				Result = delOut,
				Handler = new ConcatMany {
					String1 = new InArgument<string> ("noArgs"),
					Result = delOut
				}
			};
		}
		protected ActivityFunc<string, string> GetActivityFuncConcatMany1 ()
		{
			var arg = new DelegateInArgument<string> ();
			var delOut = new DelegateOutArgument<string> ();
			return new ActivityFunc<string, string> {
				Argument = arg,
				Result = delOut,
				Handler = new ConcatMany {
					String1 = new InArgument<string> (arg),
					Result = delOut
				}
			};
		}
		protected ActivityFunc<string, string, string> GetActivityFuncConcatMany2 ()
		{
			var arg1 = new DelegateInArgument<string> ();
			var arg2 = new DelegateInArgument<string> ();
			var delOut = new DelegateOutArgument<string> ();
			return new ActivityFunc<string, string, string> {
				Argument1 = arg1,
				Argument2 = arg2,
				Result = delOut,
				Handler = new ConcatMany {
					String1 = new InArgument<string> (arg1),
					String2 = new InArgument<string> (arg2),
					Result = delOut
				}
			};
		}
		protected ActivityFunc<string, string, string, string> 
			GetActivityFuncConcatMany3 ()
		{
			var arg1 = new DelegateInArgument<string> ();
			var arg2 = new DelegateInArgument<string> ();
			var arg3 = new DelegateInArgument<string> ();
			var delOut = new DelegateOutArgument<string> ();
			return new ActivityFunc<string, string, string, string> {
				Argument1 = arg1,
				Argument2 = arg2,
				Argument3 = arg3,
				Result = delOut,
				Handler = new ConcatMany {
					String1 = new InArgument<string> (arg1),
					String2 = new InArgument<string> (arg2),
					String3 = new InArgument<string> (arg3),
					Result = delOut
				}
			};
		}
	}
}

