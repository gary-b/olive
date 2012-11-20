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
	public abstract class InArgument : Argument
	{
		protected InArgument () : base ()
		{
			this.Direction = ArgumentDirection.In;
		}
		public static InArgument CreateReference (InArgument argumentToReference, string referencedArgumentName)
		{
			throw new NotImplementedException ();
		}
		public static InArgument CreateReference (InOutArgument argumentToReference, string referencedArgumentName)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Expression")]
	// FIXME: enable with valid type
	//[TypeConverter (typeof (InArgumentConverter))]
	[MonoTODO]
	public sealed class InArgument<T> : InArgument
	{
		public InArgument () : base ()
		{
			this.ArgumentType = typeof (T);

		}
		public InArgument (T constValue) : this ()
		{
			this.Expression = new Literal<T> (constValue);
		}

		public InArgument (Activity<T> expression) : this ()
		{
			this.Expression = expression;
		}

		public InArgument (DelegateArgument delegateArgument)
		{
			throw new NotImplementedException ();
		}

		public InArgument (Variable variable)
		{
			throw new NotImplementedException ();
		}

		public InArgument (Expression<Func<ActivityContext, T>> expression)
		{
			throw new NotImplementedException ();
		}

		public static implicit operator InArgument<T> (T constValue)
		{
			return new InArgument<T> (constValue);
		}
		public static implicit operator InArgument<T> (Activity<T> expression)
		{
			return new InArgument<T> (expression);
		}
		public static implicit operator InArgument<T> (DelegateArgument delegateArgument)
		{
			throw new NotImplementedException ();
		}
		public static implicit operator InArgument<T> (Variable variable)
		{
			throw new NotImplementedException ();
		}

		public static InArgument<T> FromDelegateArgument (DelegateArgument delegateArgument)
		{
			throw new NotImplementedException ();
		}

		public static InArgument<T> FromExpression (Activity<T> expression)
		{
			throw new NotImplementedException ();
		}

		public static InArgument<T> FromValue (T constValue)
		{
			throw new NotImplementedException ();
		}

		public static InArgument<T> FromVariable (Variable variable)
		{
			throw new NotImplementedException ();
		}
	}
}
