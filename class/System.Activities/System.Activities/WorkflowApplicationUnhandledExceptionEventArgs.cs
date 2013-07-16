using System;
using System.Runtime.Serialization;

namespace System.Activities
{
	public class WorkflowApplicationUnhandledExceptionEventArgs : WorkflowApplicationEventArgs
	{
		internal WorkflowApplicationUnhandledExceptionEventArgs (WorkflowApplication application,
									 Activity exceptionSource,
									 string exceptionSourceInstanceId,
									 Exception unhandledException)
			:base (application)
		{
			ExceptionSource = exceptionSource;
			ExceptionSourceInstanceId = exceptionSourceInstanceId;
			UnhandledException = unhandledException;
		}

		public Activity ExceptionSource { get; private set; }
		public string ExceptionSourceInstanceId { get; private set; }
		public Exception UnhandledException { get; private set; }
	}
}
