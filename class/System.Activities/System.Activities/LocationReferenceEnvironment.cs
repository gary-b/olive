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
		ICollection<RuntimeArgument> runtimeArguments;
		internal bool IsImplementation { get; set; }
		internal IDictionary<RuntimeArgument, Argument> Bindings { get; private set; }
		internal ICollection<Variable> PublicVariables { get; private set; }
		internal ICollection<Variable> ImplementationVariables { get; private set; }
		internal ICollection<RuntimeArgument> RuntimeArguments { 
			get {
				return runtimeArguments;
			}						 
			set {
				if (value == null)
					throw new ArgumentNullException("RuntimeArguments");
				runtimeArguments = value;
			}
		}
		internal ICollection<RuntimeDelegateArgument> RuntimeDelegateArguments { get; private set; }

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
			RuntimeDelegateArguments = new Collection<RuntimeDelegateArgument> ();
		}

		public override IEnumerable<LocationReference> GetLocationReferences ()
		{
			//FIXME: removed previous implementation, unsure what it should do
			throw new NotImplementedException ();
		}

		public override bool IsVisible (LocationReference locationReference)
		{
			//FIXME: removed previous implementation, unsure what it should do
			throw new NotImplementedException ();
		}

		public override bool TryGetLocationReference (string name, out LocationReference result)
		{
			//FIXME: removed previous implementation, unsure what it should do
			throw new NotImplementedException ();
		}

		void AddScopedRuntimeDelegateArguments (ActivityEnvironment env, ICollection<Activity> argCol)
		{
			var aParent = env.Parent as ActivityEnvironment;
			if (aParent == null) {
				return;
			} else if (aParent.IsImplementation == true) {
				if (aParent.RuntimeDelegateArguments.Count != 0)
					argCol.Add (aParent.Root);
				return;
			} else {
				if (aParent.RuntimeDelegateArguments.Count != 0)
					argCol.Add (aParent.Root);
				AddScopedRuntimeDelegateArguments (aParent, argCol);
			}
		}
	}
}
