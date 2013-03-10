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
	[MonoTODO]
	public class ActivityContext
	{
		// FIXME: when attempting to get or set a value for a variable which cannot be accessed .NET returns 
		// an InvalidOperationException ex. If the variable is declared in the workflow (but out of scope), 
		// it advises on which activity it has been declared.
		internal ActivityContext ()
		{
		}
		internal ActivityContext (ActivityInstance instance, WorkflowRuntime runtime)
		{
			if (instance == null)
				throw new ArgumentNullException ("instance");
			if (runtime == null)
				throw new ArgumentNullException ("runtime");

			Instance = instance;
			Runtime = runtime;
		}

		protected ActivityInstance Instance { get; set; }
		internal WorkflowRuntime Runtime { get; set; }

		public string ActivityInstanceId { 
			get {
				return Instance.Id; // guess this is right?
			}
			//was a private set;
		}
		public WorkflowDataContext DataContext { get; private set; }
		public Guid WorkflowInstanceId { get; private set; }

		public T GetExtension<T> () where T : class
		{
			throw new NotImplementedException ();
		}

		public Location<T> GetLocation<T> (LocationReference locationReference)
		{
			if (locationReference == null)
				throw new ArgumentNullException ("locationReference");

			try {
				return (Location<T>) Instance.GetLocationReferences () [locationReference];
			} catch (KeyNotFoundException ex) {
				throw new InvalidOperationException ("The locationReference cannot be used");
			}
		}
		
		public object GetValue (Argument argument)
		{
			if (argument == null)
				throw new ArgumentNullException ("argument");

			Location loc;
			try {
				loc = Instance.RuntimeArguments.Single (kvp => 
				      kvp.Key.Name == argument.BoundRuntimeArgumentName).Value;
			} catch (InvalidOperationException ex) {
				throw new InvalidOperationException ("The argument cannot be used.");
			}
			return loc.Value;
		}
		
		public T GetValue<T> (InArgument<T> argument)
		{
			if (argument == null)
				throw new ArgumentNullException ("argument");

			return (T) GetValue ((Argument) argument);
		}
		
		public T GetValue<T> (InOutArgument<T> argument)
		{
			if (argument == null)
				throw new ArgumentNullException ("argument");

			return (T) GetValue ((Argument) argument);
		}
		
		public T GetValue<T> (LocationReference locationReference)
		{
			if (locationReference == null)
				throw new ArgumentNullException ("locationReference");

			try {
				return (T) Instance.GetLocationReferences () [locationReference].Value;
			} catch (KeyNotFoundException ex) {
				throw new InvalidOperationException ("The locationReference cannot be used");
			}
		}

		public T GetValue<T> (OutArgument<T> argument)
		{
			if (argument == null)
				throw new ArgumentNullException ("argument");

			return (T) GetValue ((Argument) argument);
		}
		
		public object GetValue (RuntimeArgument runtimeArgument)
		{
			if (runtimeArgument == null)
				throw new ArgumentNullException ("runtimeArgument");

			try {
				return Instance.RuntimeArguments [runtimeArgument].Value;
			} catch (KeyNotFoundException ex) {
				throw new InvalidOperationException ("The argument cannot be used");
			}
		}
		
		public void SetValue (Argument argument, object value)
		{
			if (argument == null)
				throw new ArgumentNullException ("argument");
			//FIXME: exception
			var dataKvp = Instance.RuntimeArguments.Single (kvp => kvp.Key.Name == argument.BoundRuntimeArgumentName);
			dataKvp.Value.Value = value;
		}
		
		public void SetValue<T> (InArgument<T> argument, T value)
		{
			if (argument == null)
				return;

			SetValue ((Argument) argument, value);
		}
		
		public void SetValue<T> (InOutArgument<T> argument, T value)
		{
			if (argument == null)
				return;

			SetValue ((Argument) argument, value);
		}
		
		public void SetValue<T> (LocationReference locationReference, T value)
		{
			if (locationReference == null)
				throw new ArgumentNullException ("locationReference");

			try {
				Instance.GetLocationReferences () [locationReference].Value = value;
			} catch (KeyNotFoundException ex) {
				throw new InvalidOperationException ("The argument cannot be used.  Make sure that it " +
									"is declared on an activity.");
			}
		}
		
		public void SetValue<T> (OutArgument<T> argument, T value)
		{
			if (argument == null)
				return;

			SetValue ((Argument) argument, value);
		}

		internal Location GetLocation (Argument argument)
		{
			var dataKvp = Instance.RuntimeArguments.Single (kvp => kvp.Key.Name == argument.BoundRuntimeArgumentName);
			return dataKvp.Value;
		}

		internal Location GetLocation (LocationReference locationReference)
		{
			return Instance.GetLocationReferences () [locationReference];
		}

		internal Location GetScopedLocation (Variable variable)
		{
			return Instance.ScopedVariables [variable];
		}

		internal Location GetScopedLocation (DelegateArgument delegateArgument)
		{
			return Instance.ScopedRuntimeDelegateArguments.Where (kvp => kvp.Key.BoundArgument == delegateArgument).Single ().Value;
		}

		internal void InternalScheduleActivity (Activity activity)
		{
			Runtime.ScheduleActivity (activity, Instance);
		}
	}
}
