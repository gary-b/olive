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
		internal ActivityInstance (Activity activity, string id, bool isCompleted, ActivityInstanceState state,
		                           ActivityInstance parentInstance, bool isImplementation)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			if (String.IsNullOrEmpty (id))
				throw new ArgumentException ("Cannot be null or empty", "id");

			Id = id;
			IsCompleted = isCompleted;
			State = state;
			Activity = activity;
			ParentInstance = parentInstance;

			RuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			PublicVariables = new Dictionary<Variable, Location> ();
			ImplementationVariables = new Dictionary<Variable, Location> ();
			RefInOutRuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			RefOutRuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			RuntimeDelegateArguments = new Dictionary<RuntimeDelegateArgument, Location> ();
			ScopedRuntimeDelegateArguments = new Dictionary<RuntimeDelegateArgument, Location> ();
			IsImplementation = isImplementation;
			variablesInScopeOfArgs = null;
		}
		Dictionary<Variable, Location> variablesInScopeOfArgs;

		public Activity Activity { get; internal set; }
		public string Id { get; internal set; }
		public bool IsCompleted { get; internal set; }
		public ActivityInstanceState State { get; internal set; }
		internal bool IsImplementation { get; set; }

		internal ActivityInstance ParentInstance { get; private set; }
		internal IDictionary<RuntimeArgument, Location> RuntimeArguments { get; private set; }
		internal IDictionary<Variable, Location> PublicVariables { get; private set; }
		internal IDictionary<Variable, Location> ImplementationVariables { get; private set; }

		// holds reference to the locations that should be used as I Value (Location<Location<T>>)
		internal IDictionary<RuntimeArgument, Location> RefInOutRuntimeArguments { get; private set; }
		internal IDictionary<RuntimeArgument, Location> RefOutRuntimeArguments { get; private set; }
		internal Dictionary<RuntimeDelegateArgument, Location> RuntimeDelegateArguments { get; private set; }
		internal Dictionary<RuntimeDelegateArgument, Location> ScopedRuntimeDelegateArguments { get; private set; }

		internal Dictionary<Variable, Location> VariablesInScopeOfArgs { 
			get {
				if (variablesInScopeOfArgs != null)
					return variablesInScopeOfArgs;
				variablesInScopeOfArgs = new Dictionary<Variable, Location> ();
				AddScopedVariables (this, variablesInScopeOfArgs);
				return variablesInScopeOfArgs;
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

		internal ActivityInstance FindInstance (Activity activity)
		{
			return FindInParents (this, activity);
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

		ActivityInstance FindInParents (ActivityInstance instance, Activity activity)
		{
			if (instance.ParentInstance == null)
				throw new InvalidOperationException ("ActivityInstance not found");
			else if (instance.ParentInstance.Activity == activity)
				return instance.ParentInstance;
			else
				return FindInParents (instance.ParentInstance, activity);
		}

		internal void HandleReferences ()
		{
			foreach (var v in RefInOutRuntimeArguments)
				RuntimeArguments.Add (v.Key, (Location) v.Value.Value);

			foreach (var v in RefOutRuntimeArguments) {
				// existing value of variable not available to activity for Out Arguments
				// FIXME: BUG: If the value is not set by Activity, its original value should be unaffected
				((Location) v.Value.Value).MakeDefault ();
				RuntimeArguments.Add (v.Key, (Location) v.Value.Value);
			}
		}
	}
}
