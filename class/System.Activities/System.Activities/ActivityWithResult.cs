using System;
using System.Linq;
using System.Runtime.Serialization;

namespace System.Activities
{
	public abstract class ActivityWithResult : Activity
	{
		internal ActivityWithResult (Type resultType) : base ()
		{
			ResultType = resultType;
		}

		[IgnoreDataMemberAttribute]
		public OutArgument Result { 
			get { return ResultToUse; } 
			set { ResultToUse = value; } 
		}
		public Type ResultType { get; private set; }

		protected abstract OutArgument ResultToUse { get; set; }

		internal void DeclareResultArg (Metadata metadata) {
			if (Result != null && Result.BoundRuntimeArgumentName != null)
				return;

			var rtResult = metadata.Environment.RuntimeArguments
								.Where ((r) => r.Name == Argument.ResultValue)
								.SingleOrDefault ();
			if (rtResult == null) {
				var rtArg = new RuntimeArgument (Argument.ResultValue, ResultType, ArgumentDirection.Out);
				metadata.AddArgument (rtArg);	//AddArgument intialises Result Arg if nec, and binds automatically
			} else if (rtResult.Type == ResultType && rtResult.Direction == ArgumentDirection.Out) {
				throw new Exception ("The existing RuntimeArgument should already " +
							"be bound to " + Argument.ResultValue);
			} else  {
				throw new InvalidWorkflowException (String.Format ("The activity author supplied " +
					   "RuntimeArgument named '{0}' must have ArgumentDirection.Out and type " +
					   "{1}.  Instead, it has ArgumentDirection {2} and type {3}.",
				    		Argument.ResultValue, ResultType.ToString (), rtResult.Direction.ToString (), 
						rtResult.Type.ToString ()));
			}
		}
	}
}
