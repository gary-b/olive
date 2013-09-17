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

		private Activity rootActivity = null;

		private Activity RootActivity {
			get {
				if (rootActivity == null)
					rootActivity = Implementation ();
				return rootActivity;
			}
		}

		protected internal int CacheId { get; private set; }

		protected Collection<Constraint> Constraints { get; private set; }

		public string DisplayName { get; set; }

		public string Id { get; internal set; }

		[XamlDeferLoad (typeof (FuncDeferringLoader), typeof (Activity))]
		[Browsable (false)]
		[Ambient]
		protected virtual Func<Activity> Implementation { get; set; }

		internal virtual bool InternalCanInduceIdle {
			get { return false; }
		}

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
				md.ImplementationChildren.Add (RootActivity);
			}
			var am = new ActivityMetadata (md);
			CacheMetadata (am);
			return md;
		}

		internal virtual void RuntimeExecute (ActivityInstance instance, WorkflowRuntime runtime)
		{
			if (Implementation != null) {
				var context = new ActivityContext (instance, runtime);
				context.InternalScheduleActivity (RootActivity);
			}
		}

		internal virtual void RuntimeCancel (ActivityInstance instance, WorkflowRuntime runtime)
		{
			runtime.ScheduleCancelChildren (instance);
			instance.MarkCanceledBasedOnChildren (runtime.GetChildren (instance));
		}
	}

	[TypeConverter (typeof (ActivityWithResultConverter))]
	public abstract class Activity<TResult> : ActivityWithResult
	{
		private OutArgument<TResult> resultToUse; 

		protected override OutArgument ResultToUse {
			get {
				return Result;
			}
			set {
				Result = (OutArgument<TResult>) value;
			}
		}

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

		public new OutArgument<TResult> Result { 
			get { return resultToUse; }
			set { resultToUse = value; }
		}

		internal override Metadata GetMetadata (LocationReferenceEnvironment parentEnv)
		{
			var md = base.GetMetadata (parentEnv);
			DeclareResultArg (md);
			return md;
		}
	}
}
