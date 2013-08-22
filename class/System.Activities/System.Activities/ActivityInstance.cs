using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Activities
{
	[DataContract (Name = "ActivityInstance", Namespace = "http://schemas.datacontract.org/2010/02/System.Activities")]
	public sealed class ActivityInstance
	{
		internal ActivityInstance (Activity activity, string id, ActivityInstanceState state,
					   ActivityInstance parentInstance, bool isImplementation,
					   WorkflowRuntime runtime)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			if (String.IsNullOrEmpty (id))
				throw new ArgumentException ("Cannot be null or empty", "id");
			if (runtime == null)
				throw new ArgumentNullException ("runtime");

			Id = id;
			State = state;
			Activity = activity;
			ParentInstance = parentInstance;

			RuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			PublicVariables = new Dictionary<Variable, Location> ();
			ImplementationVariables = new Dictionary<Variable, Location> ();
			RuntimeDelegateArguments = new Dictionary<RuntimeDelegateArgument, Location> ();
			IsImplementation = isImplementation;
			variablesInScopeOfArgs = null;
			runtimeDelegateArgsInScopeOfArgs = null;
			ancestorArgsInScopeOfArgs = null;
			var parentProperties = (ParentInstance == null) ? null : ParentInstance.Properties;
			Properties = new ExecutionProperties (parentProperties, IsImplementation, this, runtime);
		}

		IDictionary<Variable, Location> variablesInScopeOfArgs;
		IDictionary<RuntimeDelegateArgument, Location> runtimeDelegateArgsInScopeOfArgs;
		IDictionary<RuntimeArgument, Location> ancestorArgsInScopeOfArgs;

		public Activity Activity { get; internal set; }
		public string Id { get; internal set; }
		public bool IsCompleted { 
			get { return State != ActivityInstanceState.Executing; } }
		public ActivityInstanceState State { get; internal set; }
		internal bool IsImplementation { get; set; }

		internal ExecutionProperties Properties { get; private set; }

		internal ActivityInstance ParentInstance { get; private set; }
		internal IDictionary<RuntimeArgument, Location> RuntimeArguments { get; private set; }
		internal IDictionary<Variable, Location> PublicVariables { get; private set; }
		internal IDictionary<Variable, Location> ImplementationVariables { get; private set; }

		internal IDictionary<RuntimeDelegateArgument, Location> RuntimeDelegateArguments { get; private set; }
		internal RuntimeDelegateArgument ResultRuntimeDelegateArgument { get; set; }

		internal IDictionary<RuntimeDelegateArgument, Location> RuntimeDelegateArgsInScopeOfArgs { 
			get {
				if (runtimeDelegateArgsInScopeOfArgs == null) {
					runtimeDelegateArgsInScopeOfArgs = new Dictionary<RuntimeDelegateArgument, 
											Location> ();
					AddScopedRuntimeDelegateArgs (this, runtimeDelegateArgsInScopeOfArgs);
				}
				return runtimeDelegateArgsInScopeOfArgs;
			}
		}
		internal IDictionary<Variable, Location> VariablesInScopeOfArgs { 
			get {
				if (variablesInScopeOfArgs == null) {
					variablesInScopeOfArgs = new Dictionary<Variable, Location> ();
					AddScopedVariables (this, variablesInScopeOfArgs);
				}
				return variablesInScopeOfArgs;
			}
		}
		internal IDictionary<RuntimeArgument, Location> AncestorArgsInScopeOfArgs {
			get {
				if (ancestorArgsInScopeOfArgs == null) {
					ancestorArgsInScopeOfArgs = new Dictionary<RuntimeArgument, Location> ();
					AddScopedArguments (this, ancestorArgsInScopeOfArgs);
				}
				return ancestorArgsInScopeOfArgs;
			}
		}
		internal IDictionary<LocationReference, Location> GetLocationReferences ()
		{
			// returns LocationReferences in scope inside execute function
			var lrefs = new Dictionary<LocationReference, Location> ();
			foreach (var v in RuntimeArguments)
				lrefs.Add ((LocationReference) v.Key, v.Value);
			foreach (var v in ImplementationVariables)
				lrefs.Add ((LocationReference) v.Key, v.Value);
			return lrefs;
		}
		void AddScopedArguments (ActivityInstance ai, IDictionary<RuntimeArgument, Location> argDict)
		{
			if (ai.ParentInstance == null) {
				return;
			} else if (ai.IsImplementation == true) {
				foreach (var kvp in ai.ParentInstance.RuntimeArguments)
					argDict.Add (kvp);
				return;
			} else { //ai must be public so
				AddScopedArguments (ai.ParentInstance, argDict);
			}
		}
		void AddScopedRuntimeDelegateArgs (ActivityInstance ai,  IDictionary<RuntimeDelegateArgument, Location> argDict)
		{
			foreach (var kvp in ai.RuntimeDelegateArguments)
				argDict.Add (kvp);

			if (ai.ParentInstance == null)
				return;
			else if (ai.IsImplementation == true)
				return;
			else // ai is public
				AddScopedRuntimeDelegateArgs (ai.ParentInstance, argDict);
		}
		void AddScopedVariables (ActivityInstance ai, IDictionary<Variable, Location> varDict)
		{
			if (ai.ParentInstance == null) {
				return;
			} else if (ai.IsImplementation == true) {
				foreach (var kvp in ai.ParentInstance.ImplementationVariables)
					varDict.Add (kvp);
				return;
			} else { // ai is public
				foreach (var kvp in ai.ParentInstance.PublicVariables)
					varDict.Add (kvp);
				AddScopedVariables (ai.ParentInstance, varDict);
			}
		}
	}
}
