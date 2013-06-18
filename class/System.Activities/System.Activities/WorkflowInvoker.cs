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
using System.IO;

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
			var runtime = new WorkflowRuntime (WorkflowDefinition);
			runtime.Run ();
			return null;
		}
	}
	public static class Logger {
		//use this class to output ad hoc logging info you provide by calling Logger.Log(..)
		static Logger ()
		{
			Log ("\nExecution Started on {0} at {1} \n{2}{2}", DateTime.Now.ToShortDateString (),
			     DateTime.Now.ToShortTimeString (),
			     "----------------------------------------------------------------------------");
		}
		public static void Log (string format, params object[] args)
		{
			var sw = new StreamWriter (@"WFLog.txt", true);
			sw.WriteLine (String.Format(format, args));
			sw.Close();
		}
	}
	internal class WorkflowRuntime {
		Activity WorkflowDefinition { get; set; }
		List<Task> TaskList { get; set; }
		ICollection<Metadata> AllMetadata { get; set; }
		int CurrentInstanceId { get; set; }

		internal WorkflowRuntime (Activity baseActivity)
		{
			if (baseActivity == null)
				throw new ArgumentNullException ("baseActivity");

			WorkflowDefinition = baseActivity;
			AllMetadata = new Collection<Metadata> ();
			TaskList = new List<Task> ();

			BuildCache (WorkflowDefinition, String.Empty, 1, null, false);
			AddNextAndInitialise (new Task (WorkflowDefinition), null);
		}

		public ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			if (parentInstance == null)
				throw new ArgumentNullException ("parentInstance");

			var task = new Task (activity);
			return AddNextAndInitialise (task, parentInstance);
		}

		public ActivityInstance ScheduleDelegate (ActivityDelegate activityDelegate, 
		                                          IDictionary<string, object> param,
		                                          CompletionCallback onCompleted,
		                                          FaultCallback onFaulted,
		                                          ActivityInstance parentInstance)
		{
			// FIXME: test how .net handles nulls, param entries that dont match, are omitted etc
			if (activityDelegate == null)
				throw new ArgumentNullException ("activityDelegate");
			if (parentInstance == null)
				throw new ArgumentNullException ("parentInstance");

			/* still return activityInstance when handler empty like .NET
			// (only tested this with ScheduleAction)
			if (activityDelegate.Handler == null) {
				return new ActivityInstance (AnActivityGoesHere, "0", true, 
				                             ActivityInstanceState.Closed, parentInstance);
			}*/

			var task = new Task (activityDelegate.Handler);
			var instance = AddNextAndInitialise (task, parentInstance);
			int pCount = (param == null) ? 0 : param.Count;
			int expectedCount = instance.RuntimeDelegateArguments.Count;

			if (pCount != expectedCount) {
				throw new ArgumentException (String.Format (
					"The supplied input parameter count {0} does not match the expected count of {1}.",
					pCount, expectedCount), "param");
			}
			foreach (var expectedKvp in instance.RuntimeDelegateArguments) {
				try {
					var pPair = param.Where (pKvp => pKvp.Key == expectedKvp.Key.Name).Single ();
					if (!expectedKvp.Key.Type.IsAssignableFrom (pPair.Value.GetType ())) {
						throw new ArgumentException (String.Format (
							"Expected an input parameter value of type '{0}' for parameter named '{1}'.",
							expectedKvp.Key.Type.Name, expectedKvp.Key.Name), "param");
					}
					expectedKvp.Value.Value = pPair.Value; //expectedKvp.Value is a Location, set its Value
				} catch (InvalidOperationException ex) {
					throw new ArgumentException (String.Format (
						"Expected input parameter named '{0}' was not found.",
						expectedKvp.Key.Name), "param");
				}
			}
			return instance;
		}

		ActivityInstance AddNextAndInitialise (Task task, ActivityInstance parentInstance)
		{
			// will be the next run
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Uninitialized)
				throw new InvalidOperationException ("task already initialized");
			TaskList.Add (task);
			return Initialise (task, parentInstance);
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

		ActivityInstance Initialise (Task task, ActivityInstance parentInstance)
		{
			Logger.Log ("Initializing {0}", task.Activity.DisplayName);

			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Uninitialized)
				throw new InvalidOperationException ("Initialized");
			CurrentInstanceId++;
			var metadata = AllMetadata.Single (m => m.Environment.Root == task.Activity);
			var instance = new ActivityInstance (task.Activity, CurrentInstanceId.ToString (), false, 
			                                     ActivityInstanceState.Executing, parentInstance, 
			                                     metadata.Environment.IsImplementation);
			task.ActivityInstance = instance;

			// these need to be created before any DelegateArgumentValues initialised
			foreach (var rda in metadata.Environment.RuntimeDelegateArguments) {
				var loc = ConstructLocationT (rda.Type);
				instance.RuntimeDelegateArguments.Add (rda, loc);
			}
			Logger.Log ("Initializing {0}\tRuntimeDelegateArguments Initialised", task.Activity.DisplayName);
			foreach (var rtArg in metadata.Environment.RuntimeArguments) {
				//FIXME: ugly
				if (rtArg.Direction == ArgumentDirection.Out && task.Type == TaskType.Initialization 
				    && rtArg.Name == Argument.ResultValue) {
					Location loc = task.ReturnLocation;
					instance.RuntimeArguments.Add (rtArg, loc);
					instance.CanSetConstResult = task.OverrideConstResult;
				} else if (rtArg.Direction == ArgumentDirection.Out || 
				           rtArg.Direction == ArgumentDirection.InOut) {
					var aEnv = metadata.Environment as ActivityEnvironment; 
					if (aEnv != null && aEnv.Bindings.ContainsKey (rtArg) &&
					    aEnv.Bindings [rtArg] != null && aEnv.Bindings [rtArg].Expression != null) {
						// create task to get location to be used as I Value
						var loc = new Location<Location> ();
						var getLocTask = new Task (aEnv.Bindings [rtArg].Expression, loc, false);
						AddNextAndInitialise (getLocTask, instance);

						if (rtArg.Direction == ArgumentDirection.Out)
							instance.RefOutRuntimeArguments.Add (rtArg, loc);
						else if (rtArg.Direction == ArgumentDirection.InOut)
							instance.RefInOutRuntimeArguments.Add (rtArg, loc);
						else
							throw new Exception ("shouldnt see me");
					} else {
						// create a new location to hold temp values while activity executing
						var loc = ConstructLocationT (rtArg.Type);
						instance.RuntimeArguments.Add (rtArg, loc);
					}
				} else if (rtArg.Direction == ArgumentDirection.In) {
					var loc = ConstructLocationT (rtArg.Type);
					var aEnv = metadata.Environment as ActivityEnvironment; 
					if (aEnv != null && aEnv.Bindings.ContainsKey (rtArg)) {
						if ( aEnv.Bindings [rtArg] != null && aEnv.Bindings [rtArg].Expression != null) {
							var initialiseTask = new Task (aEnv.Bindings [rtArg].Expression, loc, false);
							AddNextAndInitialise (initialiseTask, instance);
						}
					}
					instance.RuntimeArguments.Add (rtArg, loc);
				} else {
					throw new Exception ("ArgumentDirection unknown");
				}

			}
			Logger.Log ("Initializing {0}\tRuntimeArguments Initialised", task.Activity.DisplayName);
			foreach (var pubVar in metadata.Environment.PublicVariables) {
				var loc = InitialiseVariable (pubVar, instance);
				instance.PublicVariables.Add (pubVar, loc);
			}
			Logger.Log ("Initializing {0}\tPublicVariables Initialised", task.Activity.DisplayName);
			foreach (var impVar in metadata.Environment.ImplementationVariables) {
				var loc = InitialiseVariable (impVar, instance);
				instance.ImplementationVariables.Add (impVar, loc);
			}
			Logger.Log ("Initializing {0}\tImplementationVariables Initialised", task.Activity.DisplayName);

			task.State = TaskState.Initialized;

			return instance;
		}
		Location InitialiseVariable (Variable variable, ActivityInstance instance)
		{
			var loc = ConstructLocationT (variable.Type);
			loc.IsConst = ((variable.Modifiers & VariableModifiers.ReadOnly) == VariableModifiers.ReadOnly);
			if (variable.Default != null)
				AddNextAndInitialise (new Task (variable.Default, loc, true), instance);
			return loc;
		}
		Location ConstructLocationT (Type type)
		{
			Type locationTType = typeof (Location<>);
			Type[] genericParams = { type };
			Type constructed = locationTType.MakeGenericType (genericParams);
			return (Location) Activator.CreateInstance (constructed);
		}
		void Execute (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State == TaskState.Uninitialized)
				throw new InvalidOperationException ("Uninitialized");

			Logger.Log ("Executing {0}", task.Activity.DisplayName);
			task.ActivityInstance.HandleReferences ();
			task.Activity.RuntimeExecute (task.ActivityInstance, this);
			task.State = TaskState.Ran;
		}
		void Teardown (Task task)
		{
			Logger.Log ("Tearing down {0}", task.Activity.DisplayName);

			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State == TaskState.Uninitialized)
				throw new InvalidOperationException ("Uninitialized");

			task.ActivityInstance.State = ActivityInstanceState.Closed;
			task.ActivityInstance.IsCompleted = true;
		}

		Metadata BuildCache (Activity activity, string baseOfId, int no, LocationReferenceEnvironment parentEnv, 
		                     bool isImplementation)
		{
			Logger.Log ("BuildCache called for {0}, IsImplementation: {1}", activity.DisplayName, isImplementation);

			if (activity == null)
				throw new NullReferenceException ("activity");
			if (baseOfId == null)
				throw new NullReferenceException ("baseId");

			activity.Id = (baseOfId == String.Empty) ? no.ToString () : baseOfId + "." + no;

			var metadata = activity.GetMetadata (parentEnv);
			metadata.Environment.IsImplementation = isImplementation;
			AllMetadata.Add (metadata);

			foreach (var item in metadata.Environment.Bindings) {
				//locref is key, arg value
				if (item.Value != null && item.Value.Expression != null) {
					//isImp.. param  doesn't matter
					BuildCache (item.Value.Expression, baseOfId, ++no, metadata.Environment, false); 
				}
			}
			int childNo = 0;
			foreach (var child in metadata.ImplementationChildren)
				BuildCache (child, activity.Id, ++childNo, metadata.Environment, true);

			foreach (var child in metadata.Children)
				BuildCache (child, baseOfId, ++no, metadata.Environment, false);

			foreach (var pVar in metadata.Environment.PublicVariables) {
				if (pVar.Default != null)
					BuildCache (pVar.Default, baseOfId, ++no, metadata.Environment, false); //isImp.. param doesn't matter
			}
			foreach (var impVar in metadata.Environment.ImplementationVariables) {
				if (impVar.Default != null)
					BuildCache (impVar.Default, baseOfId, ++no, metadata.Environment, false); //isImp.. param  doesn't matter
			}
			foreach (var del in metadata.Delegates) {
				if (del.Handler != null) {
					var handlerMd = BuildCache (del.Handler, baseOfId, ++no, metadata.Environment, false);
					handlerMd.InjectRuntimeDelegateArguments (del.GetRuntimeDelegateArguments ());
				}
			}
			foreach (var del in metadata.ImplementationDelegates) {
				if (del.Handler != null) {
					var handlerMd = BuildCache (del.Handler, activity.Id, ++childNo, metadata.Environment, true);
					handlerMd.InjectRuntimeDelegateArguments (del.GetRuntimeDelegateArguments ());
				}
			}
			return metadata;
		}
	}

	internal class Metadata {
		internal ICollection<Activity> Children { get; set; }
		internal ICollection<Activity> ImportedChildren { get; set; }
		internal ICollection<Activity> ImplementationChildren { get; set; }
		internal ICollection<ActivityDelegate> Delegates { get;set; }
		internal ICollection<ActivityDelegate> ImplementationDelegates { get;set; }

		internal ActivityEnvironment Environment { get; set; }

		internal Metadata (Activity activity, LocationReferenceEnvironment parentEnv)
		{
			Children = new Collection<Activity> ();
			ImportedChildren = new Collection<Activity> ();
			ImplementationChildren = new Collection<Activity> ();
			Environment = new ActivityEnvironment (activity, parentEnv);
			Delegates = new Collection<ActivityDelegate> ();
			ImplementationDelegates = new Collection<ActivityDelegate> ();
		}

		public void AddArgument (RuntimeArgument argument)
		{
			// FIXME: add support for automatic initialisation of args, and binding of runtime args, (see tests)

			// .NET doesnt throw error
			if (argument == null)
				return; 
			// FIXME: .net validates against names of other Variables, RuntimeArguments and 
			// DelegateArguments, but not during this method call?
			Environment.RuntimeArguments.Add (argument);
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

		public void AddPublicVariable (Variable variable)
		{
			Environment.PublicVariables.Add (variable);
		}

		public void AddImplementationVariable (Variable implementationVariable)
		{
			Environment.ImplementationVariables.Add (implementationVariable);
		}

		public void AddDelegate (ActivityDelegate activityDelegate)
		{
			// TODO: if dupes passed in error thrown when dupe run on .net
			// .NET doesnt throw error on null
			if (activityDelegate == null)
				return; 
			Delegates.Add (activityDelegate);
		}

		public void AddImplementationDelegate (ActivityDelegate activityDelegate)
		{
			// TODO: if dupes passed in error thrown when dupe run on .net
			// .NET doesnt throw error on null
			if (activityDelegate == null)
				return; 
			ImplementationDelegates.Add (activityDelegate);
		}

		public void Bind (Argument binding, RuntimeArgument argument)
		{
			//FIXME: check if InvalidWorkflowException are actrually raised from calls
			// to Bind arguments or later in workflow processing
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
				} else if (binding.BoundRuntimeArgumentName != null 
				           && binding.BoundRuntimeArgumentName != argument.Name) {
					throw new InvalidWorkflowException (
						String.Format ("The Argument is already bound to RuntimeArgument {0}",
					               binding.BoundRuntimeArgumentName));
				} else {
					binding.BoundRuntimeArgumentName = argument.Name;
				}
			}

			Environment.Bindings [argument] = binding;
		}

		public void SetArgumentsCollection (Collection<RuntimeArgument> arguments)
		{
			if (arguments == null) {
				Environment.RuntimeArguments.Clear ();
				return;
			}
			// FIXME: .net validates against names of other Variables, RuntimeArguments and DelegateArguments
			// but not during this method call afaict
			Environment.RuntimeArguments = arguments;
		}

		public override string ToString ()
		{
			return Environment.Root.ToString ();
		}

		internal void InjectRuntimeDelegateArguments (ICollection<RuntimeDelegateArgument> rdas)
		{
			foreach (var rda in rdas)
				Environment.RuntimeDelegateArguments.Add (rda);
		}
	}

	internal class Task {
		internal TaskState State { get; set; }
		internal Activity Activity { get; private set; }
		internal TaskType Type { get; private set; }
		internal Location ReturnLocation { get; private set; }
		internal ActivityInstance ActivityInstance { get; set; }
		internal bool OverrideConstResult { get; private set; }

		internal Task (Activity activity)
		{
			if (activity == null)
				throw new ArgumentNullException ();

			Activity = activity;
			State = TaskState.Uninitialized;
			Type = TaskType.Normal;
			ReturnLocation = null;
			OverrideConstResult = false;
		}
		// ctor chaining here results in Type / ReturnLocation being set twice
		internal Task (Activity activity, Location returnLocation, bool overrideConst) : this (activity)
		{
			if (returnLocation == null)
				throw new ArgumentNullException ("returnLocation");

			Type = TaskType.Initialization;
			ReturnLocation = returnLocation;
			OverrideConstResult = overrideConst;
		}
		public override string ToString ()
		{
			return Activity.ToString ();
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
