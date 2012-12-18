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
	public sealed class VariableValue<T> : CodeActivity<T>
	{
		public VariableValue ()
		{
		}
		public VariableValue (Variable variable)
		{
			Variable = variable;
		}

		public Variable Variable { get; set; }

		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			var rtResult = new RuntimeArgument ("Result", ResultType, ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			if (Result == null)
				Result = new OutArgument<T> ();
			metadata.Bind (Result, rtResult);
		}

		protected override T Execute (CodeActivityContext context)
		{
			return (T) context.GetScopedLocation (Variable).Value;
		}

		public override string ToString ()
		{
			if (Variable == null || String.IsNullOrEmpty (Variable.Name))
				return base.ToString (); // FIXME: returns VariableValue'1 instead of eg VariableValue<string>
			else
				return Variable.Name;
		}
	}
}
