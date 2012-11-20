using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Activities;
using System.Activities.Debugger;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Windows.Markup;

namespace System.Activities.Expressions
{
	[ContentProperty ("Value")]
	public sealed class Literal<T> : CodeActivity<T>, IValueSerializableExpression
	{
		public Literal ()
		{
		}
		public Literal (T value)
		{
			Value = value;
		}

		public T Value { get; set; }

		public bool CanConvertToString (IValueSerializerContext context)
		{
			throw new NotImplementedException ();
		}
		public string ConvertToString (IValueSerializerContext context)
		{
			throw new NotImplementedException ();
		}
		public bool ShouldSerializeValue ()
		{
			throw new NotImplementedException ();
		}
		public override string ToString ()
		{
			return (Value == null) ? "null" : Value.ToString ();
		}

		protected override T Execute (CodeActivityContext context)
		{
			return Value;
		}

		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			var rtResult = new RuntimeArgument ("Result", ResultType, ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			if (Result == null)
				Result = new OutArgument<T> ();
			metadata.Bind (Result, rtResult);
		}
	}
}
