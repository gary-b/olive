using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

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
			RuntimeArguments = new Dictionary<RuntimeArgument, Location> ();
			PublicVariables = new Dictionary<Variable, Location> ();
			ImplementationVariables = new Dictionary<Variable, Location> ();
			// variables declared and initialised by other activities but in scope
			ScopedVariables = new Dictionary<Variable, Location> ();
			ParentInstance = parentInstance;
		}

		public Activity Activity { get; internal set; }
		public string Id { get; internal set; }
		public bool IsCompleted { get; internal set; }
		public ActivityInstanceState State { get; internal set; }

		internal ActivityInstance ParentInstance { get; private set; }
		internal IDictionary<RuntimeArgument, Location> RuntimeArguments { get; private set; }
		internal IDictionary<Variable, Location> PublicVariables { get; private set; }
		internal IDictionary<Variable, Location> ImplementationVariables { get; private set; }
		internal IDictionary<Variable, Location> ScopedVariables { get; private set; }

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
	}
}
