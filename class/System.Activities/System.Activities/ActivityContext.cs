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
			return (Location<T>) Instance.GetLocationReferences () [locationReference];
		}
		
		public object GetValue (Argument argument)
		{
			var dataKvp = Instance.RuntimeArguments.Single (kvp => kvp.Key.Name == argument.BoundRuntimeArgumentName);
			return dataKvp.Value.Value;
		}
		
		public T GetValue<T> (InArgument<T> argument)
		{
			return (T) GetValue ((Argument) argument);
		}
		
		public T GetValue<T> (InOutArgument<T> argument)
		{
			return (T) GetValue ((Argument) argument);
		}
		
		public T GetValue<T> (LocationReference locationReference)
		{
			return (T) Instance.GetLocationReferences () [locationReference].Value;
		}

		public T GetValue<T> (OutArgument<T> argument)
		{
			return (T) GetValue ((Argument) argument);
		}
		
		public object GetValue (RuntimeArgument runtimeArgument)
		{
			return Instance.RuntimeArguments [runtimeArgument].Value;
		}
		
		public void SetValue (Argument argument, object value)
		{
			var dataKvp = Instance.RuntimeArguments.Single (kvp => kvp.Key.Name == argument.BoundRuntimeArgumentName);
			dataKvp.Value.Value = value;
		}
		
		public void SetValue<T> (InArgument<T> argument, T value)
		{
			SetValue ((Argument) argument, value);
		}
		
		public void SetValue<T> (InOutArgument<T> argument, T value)
		{
			SetValue ((Argument) argument, value);
		}
		
		public void SetValue<T> (LocationReference locationReference, T value)
		{
			var locRefs = Instance.GetLocationReferences ();
			if (locRefs.ContainsKey (locationReference))
				locRefs [locationReference].Value = value;
		}
		
		public void SetValue<T> (OutArgument<T> argument, T value)
		{
			SetValue ((Argument) argument, value);
		}

		internal Location GetLocation (Argument argument)
		{
			var dataKvp = Instance.RuntimeArguments.Single (kvp => kvp.Key.Name == argument.BoundRuntimeArgumentName);
			return dataKvp.Value;
		}

		internal Location GetScopedLocation (Variable variable)
		{
			return Instance.ScopedVariables [variable];
		}

		internal void InternalScheduleActivity (Activity activity)
		{
			Runtime.ScheduleActivity (activity, Instance);
		}
	}
}
