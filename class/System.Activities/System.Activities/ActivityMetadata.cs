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
	public struct ActivityMetadata
	{
		public static bool operator == (ActivityMetadata left, ActivityMetadata right)
		{
			throw new NotImplementedException ();
		}

		public static bool operator != (ActivityMetadata left, ActivityMetadata right)
		{
			throw new NotImplementedException ();
		}

		public LocationReferenceEnvironment Environment { get { throw new NotImplementedException (); } }
		public bool HasViolations { get { throw new NotImplementedException (); } }

		Metadata Metadata { get; set; }

		internal ActivityMetadata (Metadata metadata) : this ()
		{
			if (metadata == null)
				throw new ArgumentNullException ("metadata");
			Metadata = metadata;
		}

		public void AddArgument (RuntimeArgument argument)
		{
			Metadata.AddArgument (argument);
		}
		public void AddDefaultExtensionProvider<T> (Func<T> extensionProvider) where T : class
		{
			throw new NotImplementedException ();
		}
		public void AddImportedChild (Activity importedChild)
		{
			throw new NotImplementedException ();
		}
		public void AddImportedDelegate (ActivityDelegate importedDelegate)
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
			throw new NotImplementedException ();
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
		public override int GetHashCode ()
		{
			throw new NotImplementedException ();
		}
		public Collection<Activity> GetImportedChildrenWithReflection ()
		{
			throw new NotImplementedException ();
		}
		public Collection<ActivityDelegate> GetImportedDelegatesWithReflection ()
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
