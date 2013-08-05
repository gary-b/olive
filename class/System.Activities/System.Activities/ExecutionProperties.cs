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
	public sealed class ExecutionProperties : IEnumerable<KeyValuePair<string, Object>>, IEnumerable
	{
		public bool IsEmpty { get { return !(GetAllProperties ().Any ()); } }

		IDictionary<string, object> LocalProperties { get; set; }
		IList<string> PublicOnly { get; set; }
		ExecutionProperties ParentProperties { get; set; }
		bool IsImplementation { get; set; }
		Stack<IExecutionProperty> TLSRan { get; set; }

		internal ExecutionProperties (ExecutionProperties parentProperties, bool isImplementation)
		{
			ParentProperties = parentProperties;
			LocalProperties = new Dictionary<string, object> ();
			IsImplementation = isImplementation;
			PublicOnly = new List<string> ();
			TLSRan = new Stack<IExecutionProperty> ();
		}

		public void Add (string name, object property)
		{
			Add (name, property, false);
		}
		public void Add (string name, object property, bool onlyVisibleToPublicChildren)
		{
			if (String.IsNullOrEmpty (name))
				throw new ArgumentException ("Is Null Or Empty", "name");
			if (property == null)
				throw new ArgumentNullException ("property");
			if (LocalProperties.ContainsKey (name))
				throw new ArgumentException (name, "Property with that name already defined at this scope");

			var regP = property as IPropertyRegistrationCallback;
			if (regP != null)
				regP.Register (new RegistrationContext (this));

			LocalProperties.Add (name, property);
			if (onlyVisibleToPublicChildren)
				PublicOnly.Add (name);
		}
		public object Find (string name)
		{
			if (String.IsNullOrEmpty (name))
				throw new ArgumentException ("Is Null Or Empty", "name");
			//default int this case is a kvp struct
			return GetVisibleProperties ().SingleOrDefault (p=> p.Key == name).Value;
		}
		public IEnumerator<KeyValuePair<string, Object>> GetEnumerator ()
		{
			return GetVisibleProperties ().GetEnumerator ();
		}
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetVisibleProperties ().GetEnumerator ();
		}
		public bool Remove (string name)
		{
			if (String.IsNullOrEmpty (name))
				throw new ArgumentException ("Is Null Or Empty", "name");
			if (!LocalProperties.ContainsKey (name))
				return false;
			var property = LocalProperties [name];
			LocalProperties.Remove (name);
			PublicOnly.Remove (name);
			var regP = property as IPropertyRegistrationCallback;
			if (regP != null)
				regP.Unregister (new RegistrationContext (this));
			return true;
		}
		internal IDictionary<string, object> GetVisibleProperties ()
		{
			var dict = new Dictionary<string, object> (
				LocalProperties.Reverse ().ToDictionary (kvp => kvp.Key, kvp => kvp.Value));
			if (ParentProperties != null)
				ParentProperties.GetVisiblePropertiesForChild (dict, IsImplementation);
			return dict;
		}
		internal void GetVisiblePropertiesForChild (IDictionary<string, object> dict, bool childIsImplementation)
		{
			//when props name exists in dictionary it goes invisible (as prop already in dictionary 
			//is closer in scope to calling activity)
			var propsToTake = LocalProperties.Where (kvp => !dict.ContainsKey (kvp.Key)).Reverse ();
			if (childIsImplementation) {
				//ignore public only props
				foreach (var p in propsToTake.Where (
					kvp => !PublicOnly.Contains (kvp.Key)).Reverse ())
					dict.Add (p);
			} else {
				foreach (var p in propsToTake)
					dict.Add (p);
			}
			if (ParentProperties != null)
				ParentProperties.GetVisiblePropertiesForChild (dict, IsImplementation || childIsImplementation);
			//as soon as implementation scope activity met while moving up ancestor tree all further
			//public only props become invisible
		}
		internal IEnumerable<KeyValuePair<string, object>> GetAllProperties ()
		{
			var dict = new List<KeyValuePair<string, object>> (LocalProperties.Reverse ());
			if (ParentProperties != null)
				ParentProperties.GetAllPropertiesForChild (dict);
			return dict;
		}
		internal void GetAllPropertiesForChild (List<KeyValuePair<string, object>> list)
		{
			list.AddRange (LocalProperties.Reverse ());
			if (ParentProperties != null)
				ParentProperties.GetAllPropertiesForChild (list);
		}
		IEnumerable<KeyValuePair<string, IExecutionProperty>> GetAllIExecutionProperties ()
		{
			return GetAllProperties ().Where (kvp => kvp.Value is IExecutionProperty)
				.Select (kvp => new KeyValuePair<string, IExecutionProperty> (
					kvp.Key, ((IExecutionProperty)(kvp.Value)))).ToList ();
		}
		internal void SetupWorkflowThread ()
		{
			if (TLSRan.Any ())
				throw new Exception ("Workflow thread already setup");
			foreach (var kvp in GetAllIExecutionProperties ().Reverse ()) {
				kvp.Value.SetupWorkflowThread ();
				TLSRan.Push (kvp.Value);
			}
		}
		internal void CleanupWorkflowThread ()
		{
			while (TLSRan.Any ()) {
				var p = TLSRan.Pop ();
				p.CleanupWorkflowThread ();
			}
		}
		internal void UnRegister ()
		{
			var context = new RegistrationContext (this);

			while (LocalProperties.Any ()) {
				var kvp = LocalProperties.First ();
				LocalProperties.Remove (kvp);
				var regP = kvp.Value as IPropertyRegistrationCallback;
				if (regP != null)
					regP.Unregister (context);
			}
		}
	}
}
