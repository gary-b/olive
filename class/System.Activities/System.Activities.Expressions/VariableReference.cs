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
			throw new NotImplementedException ();
		}
		public VariableReference (Variable variable)
		{
			throw new NotImplementedException ();
		}
		
		protected override Location<T> Execute (CodeActivityContext context)
		{
			throw new NotImplementedException ();
		}
		
		public override string ToString ()
		{
			throw new NotImplementedException ();
		}
	}
}
