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
					   ActivityLocation location, WorkflowRuntime runtime)
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
			activityLocation = location;

			RuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			PublicVariables = new Dictionary<Variable, Location> ();
			ImplementationVariables = new Dictionary<Variable, Location> ();
			RuntimeDelegateArguments = new Dictionary<RuntimeDelegateArgument, Location> ();
			IsImplementation = isImplementation;
			variablesInScope = null;
			runtimeDelegateArgsInScope = null;
			ancestorArgsInScope = null;
			var parentProperties = (ParentInstance == null) ? null : ParentInstance.Properties;
			Properties = new ExecutionProperties (parentProperties, IsImplementation, this, runtime);
		}

		IDictionary<Variable, Location> variablesInScope;
		IDictionary<RuntimeDelegateArgument, Location> runtimeDelegateArgsInScope;
		IDictionary<RuntimeArgument, Location> ancestorArgsInScope;
		ActivityLocation activityLocation;

		public Activity Activity { get; internal set; }
		public string Id { get; internal set; }
		public bool IsCompleted { 
			get { return State != ActivityInstanceState.Executing; } 
		}
		public ActivityInstanceState State { get; internal set; }
		internal bool IsImplementation { get; set; }

		bool IsVariableDefault { 
			get { return activityLocation == ActivityLocation.VariableDefault; } 
		}
		bool IsArgumentExpression { 
			get { return activityLocation == ActivityLocation.ArgumentExpression; } 
		}

		internal ExecutionProperties Properties { get; private set; }

		internal ActivityInstance ParentInstance { get; private set; }
		internal IDictionary<RuntimeArgument, Location> RuntimeArguments { get; private set; }
		internal IDictionary<Variable, Location> PublicVariables { get; private set; }
		internal IDictionary<Variable, Location> ImplementationVariables { get; private set; }

		internal IDictionary<RuntimeDelegateArgument, Location> RuntimeDelegateArguments { get; private set; }
		internal RuntimeDelegateArgument ResultRuntimeDelegateArgument { get; set; }

		internal IDictionary<RuntimeDelegateArgument, Location> RuntimeDelegateArgsInScope { 
			get {
				if (runtimeDelegateArgsInScope == null) {
					runtimeDelegateArgsInScope = new Dictionary<RuntimeDelegateArgument, 
											Location> ();
					AddScopedRuntimeDelegateArgs (this, runtimeDelegateArgsInScope);
				}
				return runtimeDelegateArgsInScope;
			}
		}
		internal IDictionary<Variable, Location> VariablesInScope { 
			get {
				if (variablesInScope == null) {
					variablesInScope = new Dictionary<Variable, Location> ();
					AddScopedVariables (this, variablesInScope);
				}
				return variablesInScope;
			}
		}
		internal IDictionary<RuntimeArgument, Location> AncestorArgsInScope {
			get {
				if (ancestorArgsInScope == null) {
					ancestorArgsInScope = new Dictionary<RuntimeArgument, Location> ();
					AddScopedArguments (this, ancestorArgsInScope);
				}
				return ancestorArgsInScope;
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
			} else if (ai.IsArgumentExpression) {
				AddScopedArguments (ai.ParentInstance, argDict);
			} else if (ai.IsImplementation) {
				foreach (var kvp in ai.ParentInstance.RuntimeArguments)
					argDict.Add (kvp);
				return;
			} else { //ai must be public so
				AddScopedArguments (ai.ParentInstance, argDict);
			}
		}
		void AddScopedRuntimeDelegateArgs (ActivityInstance ai, IDictionary<RuntimeDelegateArgument, Location> argDict)
		{
			if (ai.IsArgumentExpression) { // ai.ParentInstance will never be null
				AddScopedRuntimeDelegateArgs (ai.ParentInstance, argDict);
				return;
			}

			foreach (var kvp in ai.RuntimeDelegateArguments)
				argDict.Add (kvp);

			if (ai.ParentInstance == null)
				return;
			else if (ai.IsImplementation && !ai.IsVariableDefault)
				return;
			else // ai is public
				AddScopedRuntimeDelegateArgs (ai.ParentInstance, argDict);
		}
		void AddScopedVariables (ActivityInstance ai, IDictionary<Variable, Location> varDict)
		{
			if (ai.ParentInstance == null) {
				return;
			} else if (ai.IsArgumentExpression) {
				AddScopedVariables (ai.ParentInstance, varDict);
			} else if (ai.IsImplementation && !ai.IsVariableDefault) {
				foreach (var kvp in ai.ParentInstance.ImplementationVariables)
					varDict.Add (kvp);
			} else { // ai is public (or imp and it IsVariableDefault)
				foreach (var kvp in ai.ParentInstance.PublicVariables)
					varDict.Add (kvp);
				AddScopedVariables (ai.ParentInstance, varDict);
			}
		}
	}
	internal enum ActivityLocation {
		Normal,
		ArgumentExpression,
		VariableDefault
	}
}
