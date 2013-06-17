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
	public abstract class NativeActivity : Activity
	{
		protected virtual bool CanInduceIdle { 
			get { return false; } 
		}
		[IgnoreDataMemberAttribute]
		protected override sealed Func<Activity> Implementation { 
			get { return null; } 
			set { throw new NotSupportedException (); }
		}
		
		protected virtual void Abort (NativeActivityAbortContext context)
		{
			throw new NotImplementedException ();
		}

		protected override sealed void CacheMetadata (ActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}

		protected virtual void CacheMetadata (NativeActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}

		protected virtual void Cancel (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}

		protected abstract void Execute (NativeActivityContext context);

		internal override Metadata GetMetadata (LocationReferenceEnvironment parentEnv)
		{
			var md = new Metadata (this, parentEnv);
			var nam = new NativeActivityMetadata (md);
			CacheMetadata (nam);
			return md;
		}

		internal override void RuntimeExecute (ActivityInstance instance, WorkflowRuntime runtime)
		{
			var context = new NativeActivityContext (instance, runtime);
			Execute (context);
		}
	}
	
	public abstract class NativeActivity<TResult> : Activity<TResult>
	{
		protected virtual bool CanInduceIdle { get { throw new NotImplementedException (); } }
		[IgnoreDataMemberAttribute]
		protected override sealed Func<Activity> Implementation { get; set; }
		
		protected virtual void Abort (NativeActivityAbortContext context)
		{
			throw new NotImplementedException ();
		}

		protected override sealed void CacheMetadata (ActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}

		protected virtual void CacheMetadata (NativeActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}

		protected virtual void Cancel (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}

		protected abstract void Execute (NativeActivityContext context);
	}
}
