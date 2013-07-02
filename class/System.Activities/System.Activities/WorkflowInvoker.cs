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
using System.Reflection;

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
			return InitialiseRunAndGetResults (workflow);
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow)
		{
			return (TResult) InitialiseRunAndGetResults (workflow) ["Result"];
		}
		public IDictionary<string, Object> Invoke (IDictionary<string, Object> inputs)
		{
			return InitialiseRunAndGetResults (WorkflowDefinition, inputs);;
		}
		public IDictionary<string, Object> Invoke (TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,IDictionary<string, Object> inputs)
		{
			if (inputs == null)
				throw new ArgumentNullException ("inputs");
			//if workflow null exception thrown from WorkflowRuntime ctor
			return InitialiseRunAndGetResults (workflow, inputs);
		}
		public static IDictionary<string, Object> Invoke (Activity workflow,TimeSpan timeout)
		{
			throw new NotImplementedException ();
		}
		public static TResult Invoke<TResult> (Activity<TResult> workflow,IDictionary<string, Object> inputs)
		{
			return (TResult) (InitialiseRunAndGetResults (workflow, inputs) ["Result"]);
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
			return InitialiseRunAndGetResults (WorkflowDefinition);
		}
		static IDictionary<string, Object> InitialiseRunAndGetResults (Activity wf)
		{
			var wr = new WorkflowRuntime (wf);
			return RunAndGetResults (wr);
		}
		static IDictionary<string, Object> InitialiseRunAndGetResults (Activity wf, IDictionary<string, object> inputs)
		{
			var wr = new WorkflowRuntime (wf, inputs);
			return RunAndGetResults (wr);
		}
		static IDictionary<string, Object> RunAndGetResults (WorkflowRuntime wr)
		{
			//FIXME: test how the Invoke calls should handle Aborts and Terminations
			wr.Run ();
			IDictionary<string, object> outputs;
			Exception ex;
			wr.GetCompletionState (out outputs, out ex);
			return outputs;
		}
	}
	public static class Logger {
		//use this class to output ad hoc logging info you provide by calling Logger.Log (..)
		static Logger ()
		{
			Log ("\nExecution Started on {0} at {1} \n{2}{2}", DateTime.Now.ToShortDateString (),
			     DateTime.Now.ToShortTimeString (),
			     "----------------------------------------------------------------------------");
		}
		public static void Log (string format, params object[] args)
		{
			//var sw = new StreamWriter (@"WFLog.txt", true);
			//sw.WriteLine (String.Format(format, args));
			//sw.Close();
		}
	}
	internal class WorkflowRuntime {
		Activity WorkflowDefinition { get; set; }
		List<Task> TaskList { get; set; }
		ICollection<Metadata> AllMetadata { get; set; }
		int CurrentInstanceId { get; set; }
		ActivityInstance RootActivityInstance { get; set; }
		// CurrentTask also keeps reference to last task executed after Wf execution and first to be executed before
		Exception TerminateReason { get; set; }
		Exception AbortReason { get; set; }

		internal RuntimeState RuntimeState { get; set;}
		internal Action NotifyPaused { get; set; }
		internal Action<Exception, Activity, string> UnhandledException { get; set; }

		internal WorkflowRuntime (Activity baseActivity) : this (baseActivity, null)
		{
		}
		internal WorkflowRuntime (Activity baseActivity, IDictionary<string, object> inputs)
		{
			if (baseActivity == null)
				throw new ArgumentNullException ("baseActivity");

			WorkflowDefinition = baseActivity;
			AllMetadata = new Collection<Metadata> ();
			TaskList = new List<Task> ();
			RuntimeState = RuntimeState.Ready;
			TerminateReason = null;
			AbortReason = null;

			BuildCache (WorkflowDefinition, String.Empty, 1, null, false);
			RootActivityInstance = AddNextAndInitialise (new Task (WorkflowDefinition), null);

			if (inputs == null)
				return;

			var inArgs = RootActivityInstance.RuntimeArguments
							.Where ((kvp) => kvp.Key.Direction == ArgumentDirection.In 
									|| kvp.Key.Direction == ArgumentDirection.InOut)
							.ToDictionary ((kvp)=> kvp.Key.Name, (kvp)=>kvp.Value);

			foreach (var input in inputs) {
				if (inArgs.ContainsKey (input.Key))
					inArgs [input.Key].Value = input.Value;
				else 
					throw new ArgumentException ("Key " + input.Key + " in input values not found " +
						"on Activity " + baseActivity.ToString (), "inputs"); //FIXME: error msg
			}
		}
		internal ActivityInstanceState GetCompletionState ()
		{	
			return RootActivityInstance.State;//FIXME: presuming its not the last to run that we're after
		}
		internal ActivityInstanceState GetCompletionState (out IDictionary<string, object> outputs, 
		                                                   out Exception terminationException)
		{
			if (RuntimeState == RuntimeState.CompletedSuccessfully) {
				var outArgs = RootActivityInstance.RuntimeArguments
								.Where ((kvp) => kvp.Key.Direction == ArgumentDirection.Out 
					        			|| kvp.Key.Direction == ArgumentDirection.InOut)
								.ToDictionary ((kvp) => kvp.Key.Name, (kvp) => kvp.Value.Value);
		
				if (outArgs.Count > 0)
					outputs = outArgs;
				else 
					outputs = null;
				terminationException = null;
			} else if (RuntimeState == RuntimeState.Terminated) {
				terminationException = TerminateReason;
				outputs = null;
			} else {
				outputs = null;
				terminationException = null;
			}
			return RootActivityInstance.State; //FIXME: presuming its not the last to run that we're after
		}
		internal Exception GetAbortReason ()
		{
			if (RuntimeState == RuntimeState.Aborted)
				return AbortReason;
			else 
				return null;
		}
		internal void Terminate (Exception reason)
		{
			TerminateReason = reason;
			TaskList.Clear ();
			RootActivityInstance.State = ActivityInstanceState.Faulted;
			RuntimeState = RuntimeState.Terminated;
		}
		internal void Abort (Exception reason)
		{
			AbortReason = reason;
			TaskList.Clear ();
			RuntimeState = RuntimeState.Aborted;
		}
		internal void Abort ()
		{
			Abort (null);
		}
		internal ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance)
		{
			return ScheduleActivity (activity, parentInstance, null);
		}
		internal ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance, 
		                                            CompletionCallback onComplete)
		{
			return ScheduleActivity (activity, parentInstance, (Delegate) onComplete);
		}
		internal ActivityInstance ScheduleActivity<TResult> (Activity<TResult> activity, ActivityInstance parentInstance, 
								CompletionCallback<TResult> onComplete, FaultCallback onFaulted)
		{
			return ScheduleActivity (activity, parentInstance, onComplete);
		}
		ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance, 
		                                   Delegate onComplete)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			if (parentInstance == null)
				throw new ArgumentNullException ("parentInstance");

			var task = new Task (activity);
			task.CompletionCallback = onComplete;
			return AddNextAndInitialise (task, parentInstance);
		}
		internal ActivityInstance ScheduleDelegate (ActivityDelegate activityDelegate, 
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
			RuntimeState = RuntimeState.Executing;
			Task task = GetNext ();
			while (task != null) {
				switch (task.State) {
					case TaskState.Uninitialized:
						throw new Exception ("Tasks should be intialised when added to TaskList");
					case TaskState.Initialized:
						try {
							Execute (task);
						} catch (Exception ex) {
							RuntimeState = RuntimeState.UnhandledException;
							if (UnhandledException != null) {
								UnhandledException (ex, task.Activity, task.Activity.Id);
								return; 
							} // else
							throw ex;
						}
					break;
					case TaskState.Ran:
					if (task.CompletionCallback != null) 
						ExecuteCallback (task);
					Teardown (task);
					Remove (task);
					break;
					default:
						throw new Exception ("Invalid TaskState found in TaskList");
				}
				task = GetNext ();
			}
			RuntimeState = RuntimeState.CompletedSuccessfully;
			if (NotifyPaused != null)
				NotifyPaused ();
		}
		void ExecuteCallback (Task task)
		{
			var context = new NativeCallbackActivityContext (task.ActivityInstance.ParentInstance, 
			         					this, task.CompletionCallback);
			var callbackType = task.CompletionCallback.GetType ();
			try {
				if (callbackType == typeof (CompletionCallback)) {
					task.CompletionCallback.DynamicInvoke (context, task.ActivityInstance);
				} else if (callbackType.GetGenericTypeDefinition () == typeof (CompletionCallback<>)) {
					var result = task.ActivityInstance.RuntimeArguments
						.Single ((kvp)=> kvp.Key.Name == Argument.ResultValue &&
						         kvp.Key.Direction == ArgumentDirection.Out).Value.Value;
					task.CompletionCallback.DynamicInvoke (context, task.ActivityInstance, result);
				} else {
					throw new NotSupportedException ("Runtime error, invalid callback delegate");
				}
			} catch (TargetInvocationException ex) {
				throw ex.InnerException;
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
				if (rtArg.Direction == ArgumentDirection.Out || 
				           rtArg.Direction == ArgumentDirection.InOut) {
					var aEnv = metadata.Environment as ActivityEnvironment; 
					if (aEnv != null && aEnv.Bindings.ContainsKey (rtArg) &&
					    aEnv.Bindings [rtArg] != null && aEnv.Bindings [rtArg].Expression != null) {
						// create task to get location to be used as I Value

						//var getLocTask = new Task (aEnv.Bindings [rtArg].Expression
						//AddNextAndInitialise (getLocTask, instance);
						CompletionCallback<object> cb;
						if (rtArg.Direction == ArgumentDirection.Out) {
							cb = (context, completeInstance, value) => {
								var retLoc = ((Location) value);
								retLoc.MakeDefault (); // FIXME: erroneous
								instance.RuntimeArguments.Add (rtArg, 
								                               retLoc);
							};
						} else if (rtArg.Direction == ArgumentDirection.InOut) {
							cb = (context, completeInstance, value) => {
								var retLoc = ((Location) value);
								instance.RuntimeArguments.Add (rtArg, 
								                               retLoc);
							};
						} else
							throw new Exception ("shouldnt see me");
						ScheduleActivity (aEnv.Bindings [rtArg].Expression, instance, cb);
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
							CompletionCallback<object> cb; 
							cb = (context, completeInstance, value) => {
								loc.Value = value;
							};
							ScheduleActivity (aEnv.Bindings [rtArg].Expression, instance, cb);
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
			if (variable.Default != null) {
				CompletionCallback<object> cb = (context, completeInstance, value) => {
					loc.SetConstValue (value);
				};
				ScheduleActivity (variable.Default, instance, cb);
			}
			return loc;
		}
		Location ConstructLocationT (Type type)
		{
			Type locationTType = typeof (Location<>);
			Type [] genericParams = { type };
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
					//isImp.. param affects incidental access to variables from Expression Activities
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
					BuildCache (pVar.Default, baseOfId, ++no, metadata.Environment, false);
			}
			foreach (var impVar in metadata.Environment.ImplementationVariables) {
				if (impVar.Default != null)
					BuildCache (impVar.Default, baseOfId, ++no, metadata.Environment, false);
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
	internal enum RuntimeState {
		Ready,
		Executing,
		CompletedSuccessfully,
		UnhandledException,
		Aborted,
		Terminated
	}
	internal class Metadata {
		readonly Dictionary<ArgumentDirection, Type> argDirMap = new Dictionary<ArgumentDirection, Type> {
									{ArgumentDirection.In, typeof (InArgument<>)},
									{ArgumentDirection.InOut, typeof (InOutArgument<>)},
									{ArgumentDirection.Out, typeof (OutArgument<>)}};

		ICollection<PropertyInfo> argPropsOnRootClass;

		internal ICollection<Activity> Children { get; set; }
		internal ICollection<Activity> ImportedChildren { get; set; }
		internal ICollection<Activity> ImplementationChildren { get; set; }
		internal ICollection<ActivityDelegate> Delegates { get;set; }
		internal ICollection<ActivityDelegate> ImplementationDelegates { get;set; }

		internal ActivityEnvironment Environment { get; set; }

		ICollection<PropertyInfo> ArgumentPropsOnRootClass {
			get {
				if (argPropsOnRootClass == null) {
					argPropsOnRootClass = new Collection<PropertyInfo> ();
					var pubProps = Environment.Root.GetType ().GetProperties (BindingFlags.Public 
					                                                          | BindingFlags.Instance);
					foreach (var prop in pubProps) {
						if (prop.CanWrite && prop.CanRead && IsBindableType (prop))
							argPropsOnRootClass.Add (prop);
					}
				}
				return argPropsOnRootClass;
			}
		}

		internal Metadata (Activity activity, LocationReferenceEnvironment parentEnv)
		{
			Children = new Collection<Activity> ();
			ImportedChildren = new Collection<Activity> ();
			ImplementationChildren = new Collection<Activity> ();
			Environment = new ActivityEnvironment (activity, parentEnv);
			Delegates = new Collection<ActivityDelegate> ();
			ImplementationDelegates = new Collection<ActivityDelegate> ();
			argPropsOnRootClass = null;
		}

		bool IsBindableType (PropertyInfo prop)
		{
			if (!(prop.PropertyType.IsGenericType))
				return false;

			var genType = prop.PropertyType.GetGenericTypeDefinition ();
			return argDirMap.ContainsValue (genType);
		}

		bool IsCorrectDirection (PropertyInfo p, ArgumentDirection direction)
		{
			if (argDirMap [direction] == p.PropertyType.GetGenericTypeDefinition ())
				return true;
			else
				return false;
		}

		Argument ConstructArgument (Type type, ArgumentDirection direction)
		{
			Type argType = argDirMap [direction];
			Type [] genericParams = { type };
			Type constructed = argType.MakeGenericType (genericParams);
			return (Argument) Activator.CreateInstance (constructed);
		}

		public void AddArgument (RuntimeArgument argument)
		{
			// .NET doesnt throw error
			if (argument == null)
				return; 
			// FIXME: .net validates against names of other Variables, RuntimeArguments and 
			// DelegateArguments, but not during this method call?
			Environment.RuntimeArguments.Add (argument);

			var prop = ArgumentPropsOnRootClass.Where (p => p.Name == argument.Name 
			                                       && p.PropertyType.GetGenericArguments () [0] == argument.Type
			                                       && IsCorrectDirection (p, argument.Direction)).SingleOrDefault ();
			if (prop == null)
				return;

			var propArg = (Argument) prop.GetValue (Environment.Root, null);
			if (propArg == null) {
				propArg = ConstructArgument (argument.Type, argument.Direction);
				prop.SetValue (Environment.Root, propArg, null);
			}
			Bind (propArg, argument);
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
		internal ActivityInstance ActivityInstance { get; set; }
		internal Delegate CompletionCallback { get; set; }

		internal Task (Activity activity)
		{
			if (activity == null)
				throw new ArgumentNullException ();

			Activity = activity;
			State = TaskState.Uninitialized;
			Type = TaskType.Normal;
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
