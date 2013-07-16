using System;
using System.Collections.Generic;
using System.Activities.Hosting;

namespace System.Activities
{
	public class WorkflowApplicationCompletedEventArgs : WorkflowApplicationEventArgs
	{
		internal WorkflowApplicationCompletedEventArgs (WorkflowApplication application,
								ActivityInstanceState completionState,
								IDictionary<string, object> outputs, 
								Exception terminationException) 
			: base (application)
		{
			CompletionState = completionState;
			Outputs = outputs ?? new Dictionary<string, object> ();
			TerminationException = terminationException;
		}

		public ActivityInstanceState CompletionState { get; private set; }
		public IDictionary<string, object> Outputs { get; private set; }
		public Exception TerminationException { get; private set; }
	}
}
