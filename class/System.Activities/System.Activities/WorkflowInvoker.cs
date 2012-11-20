using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Transactions;
using System.Windows.Markup;
using System.Xaml;
using System.Xml.Linq;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Activities.Tracking;
using System.Activities.Validation;

namespace System.Activities
{
	public sealed class WorkflowInvoker
	{
		public WorkflowInvoker (Activity workflow)
		{
			if (workflow == null)
				throw new ArgumentNullException ("workflow");

			WorkflowDefinition = workflow;
		}
		Activity WorkflowDefinition { get; set; }

		public event EventHandler<InvokeCompletedEventArgs> InvokeCompleted;
		
		public WorkflowInstanceExtensionManager Extensions { get { throw new NotImplementedException (); } }

		public IAsyncResult BeginInvoke (AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginInvoke (IDictionary<string, Object> inputs,AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginInvoke (TimeSpan timeout,AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IAsyncResult BeginInvoke (IDictionary<string, Object> inputs,TimeSpan timeout,AsyncCallback callback,object state)
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, Object> EndInvoke (IAsyncResult result)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow)
		{
			var wf = new WorkflowInvoker (workflow);
			return wf.Invoke ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow)
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, Object> Invoke (IDictionary<string, Object> inputs)
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, Object> Invoke (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,IDictionary<string, Object> inputs)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow,IDictionary<string, Object> inputs)
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, Object> Invoke (IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow,IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow,IDictionary<string, Object> inputs,out IDictionary<string, Object> additionalOutputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (object userState)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs,object userState)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (TimeSpan timeout,object userState)
		{
			throw new NotImplementedException ();
		}
		public void InvokeAsync (IDictionary<string, Object> inputs,TimeSpan timeout,object userState)
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, Object> Invoke ()
		{
			var WorkflowExecutor = new WorkflowRuntime (WorkflowDefinition);
			WorkflowExecutor.Run ();
			return null;
		}
	}

	internal class WorkflowRuntime {
		Activity WorkflowDefinition { get; set; }
		List<Task> TaskList { get; set; }
		ICollection<Metadata> AllMetadata { get; set; }
		List<ActivityInstance> ActivityInstances { get; set; }
		int CurrentInstanceId { get; set; }

		internal WorkflowRuntime (Activity baseActivity)
		{
			if (baseActivity == null)
				throw new ArgumentNullException ("baseActivity");

			WorkflowDefinition = baseActivity;
			AllMetadata = new Collection<Metadata> ();
			TaskList = new List<Task> ();
			ActivityInstances = new List<ActivityInstance> ();

			BuildCache (WorkflowDefinition, String.Empty, 1, null);
			AddNext (new Task (WorkflowDefinition));
		}

		public ActivityInstance ScheduleActivity (Activity activity)
		{
			var task = new Task (activity);
			return AddNext (task);
		}

		ActivityInstance AddNext (Task task)
		{
			// will be the next run
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Uninitialized)
				throw new InvalidOperationException ("task already initialized");

			TaskList.Add (task);
			return Initialise (task);
		}
		void Remove (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Ran)
				throw new InvalidOperationException ("task not ran");

			TaskList.Remove (task);
		}
		Task GetNext ()
		{
			return TaskList.LastOrDefault ();
		}
		internal void Run ()
		{
			Task task = GetNext ();
			while (task != null) {
				switch (task.State) {
				case TaskState.Uninitialized:
					throw new Exception ("Tasks should be intialised when added to TaskList");
					break;
				case TaskState.Initialized:
					Execute (task);
					break;
				case TaskState.Ran:
					Teardown (task);
					Remove (task);
					break;
				default:
					throw new Exception ("Invalid TaskState found in TaskList");
				}
				task = GetNext ();
			}
		}

		ActivityInstance Initialise (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Uninitialized)
				throw new InvalidOperationException ("Initialized");

			CurrentInstanceId++;
			var instance = new ActivityInstance (task.Activity, CurrentInstanceId.ToString (), false, 
			                                     ActivityInstanceState.Executing);

			ActivityInstances.Add (instance);
			var metadata = AllMetadata.Single (m => m.Environment.Root == task.Activity);

			foreach (var locRef in metadata.Environment.LocationReferences) {
				Location loc;
				if (task.Type == TaskType.Initialization && locRef.Name == "Result") {
					loc = task.ReturnLocation;
				} else {
					//FIXME: how can i pass locRef.Type at runtime?
					loc = new Location<object> (); 
					var aEnv = metadata.Environment as ActivityEnvironment; 
					var rtArg = locRef as RuntimeArgument;
					//FIXME: ugly
					if (aEnv != null && rtArg != null && aEnv.Bindings.ContainsKey (rtArg)) {
						if ( aEnv.Bindings [rtArg] != null && aEnv.Bindings [rtArg].Expression != null) {
							var initializeTask = new Task (aEnv.Bindings [rtArg].Expression, loc);
							AddNext (initializeTask);
						}
					}
				}
				instance.Data.Add (locRef, loc);
			}
			task.State = TaskState.Initialized;
			return instance;
		}
		void Execute (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State == TaskState.Uninitialized)
				throw new InvalidOperationException ("Uninitialized");

			var instance = ActivityInstances.Single (i => i.Activity == task.Activity);
			task.Activity.RuntimeExecute (instance, this);
			task.State = TaskState.Ran;
		}
		void Teardown (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State == TaskState.Uninitialized)
				throw new InvalidOperationException ("Uninitialized");

			var instance = ActivityInstances.Single (i => i.Activity == task.Activity);
			instance.State = ActivityInstanceState.Closed;
			instance.IsCompleted = true;
			ActivityInstances.Remove (instance);
		}

		void BuildCache (Activity activity, string baseOfId, int no, LocationReferenceEnvironment parentEnv)
		{
			if (activity == null)
				throw new NullReferenceException ("activity");
			if (baseOfId == null)
				throw new NullReferenceException ("baseId");
			
			activity.Id = (baseOfId == String.Empty) ? no.ToString () : baseOfId + "." + no;
			
			var metadata = activity.GetEnvironment (parentEnv);
			
			AllMetadata.Add (metadata);

			foreach (var item in metadata.Environment.Bindings) {
				//locref is key, arg value
				if (item.Value != null && item.Value.Expression != null) {
					//FIXME: should parentEnv be new env, current parentenv, or null?
					BuildCache (item.Value.Expression, baseOfId, ++no, parentEnv); 
				}
			}
			int childNo = 0;
			foreach (var child in metadata.ImplementationChildren)
				BuildCache (child, activity.Id, ++childNo, metadata.Environment);

			foreach (var child in metadata.Children)
				BuildCache (child, baseOfId, ++no, parentEnv);

		}
	}

	internal class Metadata {
		internal ICollection<Activity> Children { get; set; }
		internal ICollection<Activity> ImportedChildren { get; set; }
		internal ICollection<Activity> ImplementationChildren { get; set; }
		internal ActivityEnvironment Environment { get; set; }

		internal Metadata (Activity activity, LocationReferenceEnvironment parentEnv)
		{
			Children = new Collection<Activity> ();
			ImportedChildren = new Collection<Activity> ();
			ImplementationChildren = new Collection<Activity> ();
			Environment = new ActivityEnvironment (activity, parentEnv);
		}

		public void AddArgument (RuntimeArgument argument)
		{
			// FIXME: add support for automatic initialisation of args, and binding of runtime args, (see tests)
			
			// .NET doesnt throw error
			if (argument == null)
				return; 
			
			if (Environment.LocationReferences.Any (arg => arg.Name == argument.Name))
				throw new InvalidWorkflowException ("A Variable, RuntimeArgument or DelegateArgument of that name already exists");
			//Note: .NET throws a slightly different worded error if same arg instance passed in twice
			
			Environment.LocationReferences.Add (argument);
		}

		public void AddImplementationChild (Activity child)
		{
			// .NET doesnt raise error
			if (child == null)
				return;
			ImplementationChildren.Add (child); // FIXME: handle dupes
		}

		public void AddChild (Activity child)
		{
			// .NET doesnt raise error
			if (child == null)
				return;
			Children.Add (child); // FIXME: handle dupes
		}

		public void Bind (Argument binding, RuntimeArgument argument)
		{
			if (argument == null)
				throw new ArgumentNullException ("argument");
			
			if (binding != null) {
				if (binding.ArgumentType != argument.Type) {
					throw new InvalidWorkflowException (
						String.Format ("The Argument provided for the RuntimeArgument '{0}' "+
					               "cannot be bound because of a type mismatch.  The " +
					               "RuntimeArgument declares the type to be {1} and the " +
					               "Argument has a type of {2}.  Both types must be the same.",
					               argument.Name,argument.Type.FullName, binding.ArgumentType.FullName));
				} else {
					binding.BoundRuntimeArgumentName = argument.Name;
				}
			}
			
			Environment.Bindings [argument] = binding;
		}

		public void SetArgumentsCollection (Collection<RuntimeArgument> arguments)
		{
			Environment.LocationReferences.Clear ();

			foreach (var arg in arguments) {
				if (Environment.LocationReferences.Any (a=> a.Name == arg.Name))
					throw new InvalidWorkflowException (String.Format (
						"A variable, RuntimeArgument or a "+
						"DelegateArgument already exists with"+
						" the name '{0}'. Names must be unique "+
						"within an environment scope.", arg.Name));
				else
					Environment.LocationReferences.Add (arg);
			}
		}
	}

	internal class Task {
		internal TaskState State { get; set; }
		internal Activity Activity { get; private set; }
		internal TaskType Type { get; private set; }
		internal Location ReturnLocation { get; private set; }

		internal Task (Activity activity)
		{
			if (activity == null)
				throw new ArgumentNullException ();

			Activity = activity;
			State = TaskState.Uninitialized;
			Type = TaskType.Normal;
			ReturnLocation = null;
		}
		// ctor chaining here results in Type / ReturnLocation being set twice
		internal Task (Activity activity, Location returnLocation) : this (activity)
		{
			if (returnLocation == null)
				throw new ArgumentNullException ("returnLocation");

			Type = TaskType.Initialization;
			ReturnLocation = returnLocation;
		}
	}
	internal enum TaskState {
		Uninitialized,
		Initialized,
		Ran
	}
	internal enum TaskType {
		Initialization,
		Normal
	}
}
