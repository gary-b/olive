using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Transactions;
using System.Windows.Markup;
using System.Xaml;
using System.Xml.Linq;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Activities.Tracking;
using System.Activities.Validation;

namespace System.Activities
{
	public struct NativeActivityMetadata
	{
		public static bool operator == (NativeActivityMetadata left, NativeActivityMetadata right)
		{
			throw new NotImplementedException ();
		}

		public static bool operator != (NativeActivityMetadata left, NativeActivityMetadata right)
		{
			throw new NotImplementedException ();
		}

		public LocationReferenceEnvironment Environment { get { throw new NotImplementedException (); } }
		public bool HasViolations { get { throw new NotImplementedException (); } }

		internal Metadata Metadata { get; set; }
		internal NativeActivityMetadata (Metadata metadata) : this ()
		{
			if (metadata == null)
				throw new ArgumentNullException ("metadata");
			Metadata = metadata;
		}

		public void AddArgument (RuntimeArgument argument)
		{
			Metadata.AddArgument (argument);
		}

		public void AddChild (Activity child)
		{
			Metadata.AddChild (child);
		}
		public void AddDefaultExtensionProvider<T> (Func<T> extensionProvider) where T : class
		{
			throw new NotImplementedException ();
		}
		public void AddDelegate (ActivityDelegate activityDelegate)
		{
			Metadata.AddDelegate (activityDelegate);
		}
		public void AddImplementationChild (Activity child)
		{
			Metadata.AddImplementationChild (child);
		}
		public void AddImplementationDelegate (ActivityDelegate implementationDelegate)
		{
			Metadata.AddImplementationDelegate (implementationDelegate);
		}
		public void AddImplementationVariable (Variable implementationVariable)
		{
			Metadata.AddImplementationVariable (implementationVariable);
		}
		public void AddImportedChild (Activity importedChild)
		{
			throw new NotImplementedException ();
		}
		public void AddImportedDelgate (ActivityDelegate importedDelegate)
		{
			throw new NotImplementedException ();
		}
		public void AddValidationError (string validationErrorMessage)
		{
			throw new NotImplementedException ();
		}
		public void AddValidationError (ValidationError validationError)
		{
			throw new NotImplementedException ();
		}
		public void AddVariable (Variable variable)
		{
			Metadata.AddPublicVariable (variable);
		}
		public void Bind (Argument binding, RuntimeArgument argument)
		{
			Metadata.Bind (binding, argument);
		}
		public override bool Equals (object obj)
		{
			throw new NotImplementedException ();
		}
		public Collection<RuntimeArgument> GetArgumentsWithReflection ()
		{
			throw new NotImplementedException ();
		}
		public Collection<Activity> GetChildrenWithReflection ()
		{
			throw new NotImplementedException ();
		}
		public Collection<ActivityDelegate> GetDelegatesWithReflection ()
		{
			throw new NotImplementedException ();
		}
		public override int GetHashCode ()
		{
			throw new NotImplementedException ();
		}
		public Collection<Variable> GetVariablesWithReflection ()
		{
			throw new NotImplementedException ();
		}
		public void RequireExtension<T> () where T : class
		{
			throw new NotImplementedException ();
		}
		public void RequireExtension (Type extensionType)
		{
			throw new NotImplementedException ();
		}
		public void SetArgumentsCollection (Collection<RuntimeArgument> arguments)
		{
			Metadata.SetArgumentsCollection (arguments);
		}
		public void SetChildrenCollection (Collection<Activity> children)
		{
			throw new NotImplementedException ();
		}
		public void SetDelegatesCollection (Collection<ActivityDelegate> delegates)
		{
			throw new NotImplementedException ();
		}
		public void SetImplementationChildrenCollection (Collection<Activity> implementationChildren)
		{
			throw new NotImplementedException ();
		}
		public void SetImplementationDelegatesCollection (Collection<ActivityDelegate> implementationDelegates)
		{
			throw new NotImplementedException ();
		}
		public void SetImplementationVariablesCollection (Collection<Variable> implementationVariables)
		{
			throw new NotImplementedException ();
		}
		public void SetImportedChildrenCollection (Collection<Activity> importedChildren)
		{
			throw new NotImplementedException ();
		}
		public void SetImportedDelegatesCollection (Collection<ActivityDelegate> importedDelegates)
		{
			throw new NotImplementedException ();
		}
		public void SetValidationErrorsCollection (Collection<ValidationError> validationErrors)
		{
			throw new NotImplementedException ();
		}
		public void SetVariablesCollection (Collection<Variable> variables)
		{
			throw new NotImplementedException ();
		}
	}
}
