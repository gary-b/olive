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
	public sealed class ArgumentValue<T> : CodeActivity<T>
	{
		public ArgumentValue ()
		{
			ArgumentName = null;
		}
		public ArgumentValue (string argumentName)
		{
			ArgumentName = argumentName;
		}

		public string ArgumentName { get; set; }

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
			// RuntimeArgument validates its name never allowed to be null / empty
			return (T) context.GetLocationOfArgInScope (ArgumentName).Value;
		}

		public override string ToString ()
		{
			if (String.IsNullOrEmpty(ArgumentName))
				return base.ToString ();
			else
				return ArgumentName;
		}
	}
}
