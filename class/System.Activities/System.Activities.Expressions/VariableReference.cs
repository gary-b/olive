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
	public sealed class VariableReference<T> : CodeActivity<Location<T>>
	{
		public Variable Variable { get; set; }
		
		public VariableReference ()
		{
		}

		public VariableReference (Variable variable)
		{
			Variable = variable;
		}

		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			var rtResult = new RuntimeArgument ("Result", ResultType, ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			if (Result == null)
				Result = new OutArgument<Location<T>> ();
			metadata.Bind (Result, rtResult);
		}

		protected override Location<T> Execute (CodeActivityContext context)
		{
			return (Location<T>) context.GetScopedLocation (Variable);
		}
		
		public override string ToString ()
		{
			if (Variable == null)
				return base.ToString ();
			else
				return Variable.Name;
		}
	}
}
