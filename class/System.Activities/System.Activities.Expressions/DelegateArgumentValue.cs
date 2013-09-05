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
	[ContentProperty ("DelegateArgument")]
	public sealed class DelegateArgumentValue<T> : CodeActivity<T>
	{
		public DelegateArgument DelegateArgument { get; set; }

		public DelegateArgumentValue ()
		{
		}
		public DelegateArgumentValue (DelegateArgument delegateArgument)
		{
			DelegateArgument = delegateArgument;
		}

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
			return (T) context.GetLocationInScope (DelegateArgument).Value;
		}
	}
}
