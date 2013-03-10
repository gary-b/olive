using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Activities
{
	[DataContract (Name = "ActivityInstance", Namespace = "http://schemas.datacontract.org/2010/02/System.Activities")]
	public sealed class ActivityInstance
	{
		internal ActivityInstance (Activity activity, string id, bool isCompleted, ActivityInstanceState state,
		                           ActivityInstance parentInstance)
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
			ScopedVariables = new Dictionary<Variable, Location> ();
			RefInOutRuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			RefOutRuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			RuntimeDelegateArguments = new Dictionary<RuntimeDelegateArgument, Location> ();
			ScopedRuntimeDelegateArguments = new Dictionary<RuntimeDelegateArgument, Location> ();
		}

		public Activity Activity { get; internal set; }
		public string Id { get; internal set; }
		public bool IsCompleted { get; internal set; }
		public ActivityInstanceState State { get; internal set; }

		internal ActivityInstance ParentInstance { get; private set; }
		internal IDictionary<RuntimeArgument, Location> RuntimeArguments { get; private set; }
		internal IDictionary<Variable, Location> PublicVariables { get; private set; }
		internal IDictionary<Variable, Location> ImplementationVariables { get; private set; }
		// variables declared and initialised by other activities but in scope
		internal IDictionary<Variable, Location> ScopedVariables { get; private set; }
		// holds reference to the locations that should be used as I Value (Location<Location<T>>)
		internal IDictionary<RuntimeArgument, Location> RefInOutRuntimeArguments { get; private set; }
		internal IDictionary<RuntimeArgument, Location> RefOutRuntimeArguments { get; private set; }
		internal Dictionary<RuntimeDelegateArgument, Location> RuntimeDelegateArguments { get; private set; }
		internal Dictionary<RuntimeDelegateArgument, Location> ScopedRuntimeDelegateArguments { get; private set; }

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
				// FIXME: check .NET changes value of referenced variable
				((Location) v.Value.Value).MakeDefault ();
				RuntimeArguments.Add (v.Key, (Location) v.Value.Value);
			}
		}
	}
}
