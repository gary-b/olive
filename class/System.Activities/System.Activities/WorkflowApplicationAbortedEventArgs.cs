using System;

namespace System.Activities {
	public class WorkflowApplicationAbortedEventArgs : WorkflowApplicationEventArgs	{
		internal WorkflowApplicationAbortedEventArgs (WorkflowApplication application, 
							      Exception reason)
			:base (application)
		{
			Reason = reason;
		}

		public Exception Reason { get; private set; }
	}
}

