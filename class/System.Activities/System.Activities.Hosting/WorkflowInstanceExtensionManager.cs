using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Threading;
using System.Activities;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Tracking;

namespace System.Activities.Hosting
{
	public class WorkflowInstanceExtensionManager
	{
		internal ICollection<object> ExtensionObjects { get; private set; }
		internal ICollection<Func<object>> ExtensionProviders { get; private set; }
		bool IsReadOnly { get; set; }

		public WorkflowInstanceExtensionManager ()
		{
			ExtensionObjects = new Collection<object> ();
			ExtensionProviders = new Collection<Func<object>> ();
			IsReadOnly = false;
		}
		public virtual void Add<T> (Func<T> extensionCreationFunction) where T : class
		{
			if (extensionCreationFunction == null)
				throw new ArgumentNullException ("extensionCreationFunction");
			if (IsReadOnly)
				throw new InvalidOperationException ();

			ExtensionProviders.Add (extensionCreationFunction);
		}
		public virtual void Add (object singletonExtension)
		{
			if (singletonExtension == null)
				throw new ArgumentNullException ("singletonExtension");
			if (IsReadOnly)
				throw new InvalidOperationException ();

			ExtensionObjects.Add (singletonExtension);
		}
		public void MakeReadOnly ()
		{
			IsReadOnly = true;
		}
	}
}
