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

	}

	//added
	internal class ActivityEnvironment : LocationReferenceEnvironment
	{
		Activity root;
		internal bool IsImplementation { get; set; }
		internal IDictionary<RuntimeArgument, Argument> Bindings { get; private set; }
		internal ICollection<Variable> PublicVariables { get; private set; }
		internal ICollection<Variable> ImplementationVariables { get; private set; }
		internal ICollection<RuntimeArgument> RuntimeArguments { get; private set; }

		// variables declared on other activities but in scope
		internal IDictionary<Variable, Activity> ScopedVariables {
			get {
				var varDict = new Dictionary<Variable, Activity> ();
				AddScopedVariables (this, varDict);
				return varDict;
			}
		}

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
			PublicVariables = new Collection<Variable> ();
			ImplementationVariables = new Collection<Variable> ();
			RuntimeArguments = new Collection<RuntimeArgument> ();
		}

		public override IEnumerable<LocationReference> GetLocationReferences ()
		{
			//FIXME: test
			// returns all LocationReferences in scope of activity
			var varList = new List<LocationReference> ();
			varList.AddRange (GetVariables ());
			varList.AddRange (RuntimeArguments);
			return varList;
		}

		public override bool IsVisible (LocationReference locationReference)
		{
			//FIXME: test
			return GetLocationReferences ().Any (lr => lr == locationReference);
		}

		public override bool TryGetLocationReference (string name, out LocationReference result)
		{
			//FIXME: test
			// only checks variables in scope of activity
			result = GetLocationReferences ().SingleOrDefault (lr => lr.Name == name);
			return (result != null);
		}

		internal IEnumerable<Variable> GetVariables ()
		{
			// return in all variables in scope of activity
			return ImplementationVariables.Concat (ScopedVariables.Select (kvp => kvp.Key));
		}

		void AddScopedVariables (ActivityEnvironment env, IDictionary<Variable, Activity> varDict)
		{
			var aParent = env.Parent as ActivityEnvironment;
			if (aParent == null) {
				return;
			} else if (env.IsImplementation == true) {
				foreach (var v in aParent.ImplementationVariables)
					varDict.Add (v, aParent.Root);
				return;
			} else {
				foreach (var v in aParent.PublicVariables)
					varDict.Add (v, aParent.Root);
				AddScopedVariables (aParent, varDict);
			}
		}
	}
}
