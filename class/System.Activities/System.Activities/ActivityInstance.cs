using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace System.Activities
{
	[DataContract (Name = "ActivityInstance", Namespace = "http://schemas.datacontract.org/2010/02/System.Activities")]
	public sealed class ActivityInstance
	{
		internal ActivityInstance (Activity activity, string id, bool isCompleted, ActivityInstanceState state)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			if (String.IsNullOrEmpty (id))
				throw new ArgumentException ("Cannot be null or empty", "id");

			Id = id;
			IsCompleted = isCompleted;
			State = state;
			Activity = activity;
			Data = new Dictionary<LocationReference, Location> ();
		}

		public Activity Activity { get; internal set; }
		public string Id { get; internal set; }
		public bool IsCompleted { get; internal set; }
		public ActivityInstanceState State { get; internal set; }

		internal Dictionary<LocationReference, Location> Data;
	}
}
