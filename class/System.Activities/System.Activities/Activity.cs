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
using System.Activities.XamlIntegration;

namespace System.Activities
{
	[ContentProperty ("Implementation")]
	public abstract class Activity
	{
		protected Activity ()
		{
			CacheId = 0;
			Id = null;
			Constraints = new Collection<Constraint> ();
			DisplayName = this.GetType ().Name;
		}

		protected internal int CacheId { get; private set; }

		protected Collection<Constraint> Constraints { get; private set; }

		public string DisplayName { get; set; }

		public string Id { get; internal set; }

		[XamlDeferLoad (typeof (FuncDeferringLoader), typeof (Activity))]
		[Browsable (false)]
		[Ambient]
		protected virtual Func<Activity> Implementation { get; set; }

		protected virtual void CacheMetadata (ActivityMetadata metadata)
		{
			// FIXME: should use reflection to setup metadata
		}

		public bool ShouldSerializeDisplayName ()
		{
			throw new NotImplementedException ();
		}

		public override string ToString ()
		{
			return String.Concat (String.Format ("{0}: {1}", Id, DisplayName));
		}

		internal virtual Metadata GetMetadata (LocationReferenceEnvironment parentEnv)
		{
			var md = new Metadata (this, parentEnv);
			if (Implementation != null) {
				var activity = Implementation ();
				md.ImplementationChildren.Add (activity);
			}
			var am = new ActivityMetadata (md);
			CacheMetadata (am);
			return md;
		}

		internal virtual void RuntimeExecute (ActivityInstance instance, WorkflowRuntime runtime)
		{
			if (Implementation != null) {
				var context = new ActivityContext (instance, runtime);
				context.InternalScheduleActivity (Implementation ());
			}
		}
	}

	[TypeConverter (typeof (ActivityWithResultConverter))]
	public abstract class Activity<TResult> : ActivityWithResult
	{
		public static Activity<TResult> FromValue (TResult constValue)
		{
			throw new NotImplementedException ();
		}
		
		public static Activity<TResult> FromVariable (Variable variable)
		{
			throw new NotImplementedException ();
		}

		public static Activity<TResult> FromVariable (Variable<TResult> variable)
		{
			throw new NotImplementedException ();
		}

		public static implicit operator Activity<TResult> (TResult constValue)
		{
			throw new NotImplementedException ();
		}

		public static implicit operator Activity<TResult> (Variable variable)
		{
			throw new NotImplementedException ();
		}

		public static implicit operator Activity<TResult> (Variable<TResult> variable)
		{
			throw new NotImplementedException ();
		}

		// instance members

		protected Activity ()
			: base (typeof (TResult))
		{

		}
		
		public new OutArgument<TResult> Result { get; set; }
	}
}
