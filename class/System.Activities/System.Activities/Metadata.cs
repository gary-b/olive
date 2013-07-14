using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Activities {
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
}

