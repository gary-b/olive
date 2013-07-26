using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;

namespace Tests.System.Activities {
	public partial class ActivityDelegateTestHelper {
		protected ActivityAction GetActivityActionConcatMany0 ()
		{
			return new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (new ConcatMany {
							String1 = "noArgs",
						})
				}
			};
		}
		protected ActivityAction<string> GetActivityActionConcatMany1 ()
		{
			var arg = new DelegateInArgument<string> ();
			return new ActivityAction<string> {
				Argument = arg,
				Handler = new WriteLine {
					Text = new InArgument<string> (new ConcatMany {
						String1 = arg,
					})
				}
			};
		}
		protected ActivityAction<string, string> GetActivityActionConcatMany2 ()
		{
			var arg1 = new DelegateInArgument<string> ();
			var arg2 = new DelegateInArgument<string> ();
			return new ActivityAction<string, string> {
				Argument1 = arg1,
				Argument2 = arg2,
				Handler = new WriteLine {
					Text = new InArgument<string> (new ConcatMany {
						String1 = arg1,
						String2 = arg2,
					})
				}
			};
		}
		protected ActivityAction<string, string, string> GetActivityActionConcatMany3 ()
		{
			var arg1 = new DelegateInArgument<string> ();
			var arg2 = new DelegateInArgument<string> ();
			var arg3 = new DelegateInArgument<string> ();
			return new ActivityAction<string, string, string> {
				Argument1 = arg1,
				Argument2 = arg2,
				Argument3 = arg3,
				Handler = new WriteLine {
					Text = new InArgument<string> (new ConcatMany {
						String1 = arg1,
						String2 = arg2,
						String3 = arg3,
					})
				}
			};
		}

	}
}

