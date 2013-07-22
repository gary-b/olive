using System;

namespace System.Activities
{
	public sealed class NativeActivityFaultContext : NativeActivityContext
	{
		internal Boolean Handled { get; private set; }
		internal NativeActivityFaultContext (ActivityInstance instance, WorkflowRuntime runtime) : 
			base (instance, runtime)
		{
		}
		public void HandleFault() 
		{
			Handled = true;
		}
	}
}
