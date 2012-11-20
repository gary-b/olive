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
	public abstract class Argument
	{
		protected Argument ()
		{
			this.EvaluationOrder = Argument.UnspecifiedEvaluationOrder;
		}

		public static Argument Create (Type type, ArgumentDirection direction)
		{
			throw new NotImplementedException ();
		}
		public static Argument CreateReference (Argument argumentToReference, string referencedArgumentName)
		{
			throw new NotImplementedException ();
		}

		public const string ResultValue = "Result";

		public static readonly int UnspecifiedEvaluationOrder = 0;
		
		public Type ArgumentType { get; internal set; }
		public ArgumentDirection Direction { get; internal set; }
		public int EvaluationOrder { get; set; }
		[IgnoreDataMemberAttribute]
		public ActivityWithResult Expression { get; set; }

		internal string BoundRuntimeArgumentName { get; set; }

		public object Get (ActivityContext context)
		{
			//FIXME: test?
			return context.GetValue (this); // FIXME: right to implement in context?
		}
		public T Get<T> (ActivityContext context)
		{
			//FIXME: test?
			return (T) context.GetValue (this);
		}
		public Location GetLocation (ActivityContext context)
		{
			//FIXME: test
			return context.GetLocation (this);
		}
		public void Set (ActivityContext context, object value)
		{
			//FIXME: test?
			context.SetValue (this, value); // FIXME: right to implement in context?
		}
	}
}
