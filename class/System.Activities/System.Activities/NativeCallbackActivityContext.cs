using System;

namespace System.Activities {
	/*FIXME: ?THIS IS NOT IN THE .NET SPEC, A NativeActivityContext IS HOWEVER
	  This class will be provided to CompletedCallback and CompletionCallback<T> delegates as context param. 
	  .NET allows anonymouse methods to run but exceptions if the context methods are called advising that the 
	  method used as delegate should be an instance method of Activity that scheduled the child. This class replicates 
	  that behavior, which is very convenient when setting callbacks for Expression Activities used to initialize 
	  Arguments and Variables
	*/
	public class NativeCallbackActivityContext : NativeActivityContext {
		Delegate OnCompleted { get; set; }
		protected override ActivityInstance Instance {
			get {
				if (OnCompleted.Target != base.Instance.Activity)
					throw new ArgumentException ();
				return base.Instance;
			}
			set {
				base.Instance = value;
			}
		}
		internal override WorkflowRuntime Runtime {
			get {
				if (OnCompleted.Target != base.Instance.Activity)
					throw new ArgumentException ();
				return base.Runtime;
			}
			set {
				base.Runtime = value;
			}
		}
		internal NativeCallbackActivityContext (ActivityInstance instance, WorkflowRuntime runtime, 
		                                        Delegate onCompleted) : base (instance, runtime)
		{
			OnCompleted = onCompleted;
		}
	}
}

