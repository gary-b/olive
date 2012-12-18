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
	public abstract class CodeActivity : Activity
	{
		[IgnoreDataMemberAttribute]
		protected override sealed Func<Activity> Implementation {
			get {
				return null;
			} set {
				throw new NotSupportedException ();
			}
		}
		protected override sealed void CacheMetadata (ActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}
		protected virtual void CacheMetadata (CodeActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}
		protected abstract void Execute (CodeActivityContext context);

		internal override Metadata GetMetadata (LocationReferenceEnvironment parentEnv)
		{
			var md = new Metadata (this, parentEnv);
			var cam = new CodeActivityMetadata (md);
			CacheMetadata (cam);
			return md;
		}

		internal override void RuntimeExecute (ActivityInstance instance, WorkflowRuntime runtime)
		{
			var context = new CodeActivityContext (instance, runtime);
			Execute (context);
		}
	}
	
	public abstract class CodeActivity<TResult> : Activity<TResult>
	{
		[IgnoreDataMemberAttribute]
		protected override sealed Func<Activity> Implementation {
			get {
				return null;
			} set {
				throw new NotSupportedException ();
			}
		}

		protected override sealed void CacheMetadata (ActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}
		protected virtual void CacheMetadata (CodeActivityMetadata metadata)
		{
			throw new NotImplementedException ();
		}
		protected abstract TResult Execute (CodeActivityContext context);

		internal override Metadata GetMetadata (LocationReferenceEnvironment parentEnv)
		{
			//duplication of code (CodeActivity.GetEnvironment)
			var md = new Metadata (this, parentEnv);
			var cam = new CodeActivityMetadata (md);
			CacheMetadata (cam);
			return md;
		}

		internal override void RuntimeExecute (ActivityInstance instance, WorkflowRuntime runtime)
		{
			var context = new CodeActivityContext (instance, runtime);
			TResult result = Execute (context);
			context.SetValue ((Argument) Result, result);
		}
	}
}
