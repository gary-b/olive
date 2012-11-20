using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace System.Activities
{
	public abstract class LocationReferenceEnvironment
	{
		public LocationReferenceEnvironment Parent { get; protected set; }
		public abstract Activity Root { get; }
		
		public abstract IEnumerable<LocationReference> GetLocationReferences ();
		public abstract bool IsVisible (LocationReference locationReference);
		public abstract bool TryGetLocationReference (string name, out LocationReference result);

		internal ICollection<LocationReference> LocationReferences { get; set; } // FIXME: right to be here?
	}

	//added
	internal class ActivityEnvironment : LocationReferenceEnvironment
	{
		Activity root;
		internal IDictionary<RuntimeArgument, Argument> Bindings { get; set; }

		public override Activity Root {
			get {
				return root;
			}
		}

		internal ActivityEnvironment (Activity activity, LocationReferenceEnvironment parent)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			
			root = activity;
			Parent = parent;
			Bindings = new Dictionary<RuntimeArgument, Argument> ();
			LocationReferences = new Collection<LocationReference> ();
		}

		public override IEnumerable<LocationReference> GetLocationReferences ()
		{
			//FIXME: return actual collection?
			return LocationReferences;
		}

		public override bool IsVisible (LocationReference locationReference)
		{
			throw new NotImplementedException ();
		}

		public override bool TryGetLocationReference (string name, out LocationReference result)
		{
			//FIXME: test
			result = LocationReferences.SingleOrDefault (lr => lr.Name == name);
			return (result != null);
		}
	}
}
