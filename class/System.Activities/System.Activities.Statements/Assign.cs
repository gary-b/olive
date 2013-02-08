using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Transactions;
using System.Activities;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Hosting;
using System.Windows.Markup;

namespace System.Activities.Statements
{
	public sealed class Assign : CodeActivity
	{
		[RequiredArgumentAttribute]
		public OutArgument To { get; set; }
		[RequiredArgumentAttribute]
		public InArgument Value { get; set; }

		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			var rtTo = new RuntimeArgument ("To", To.ArgumentType, ArgumentDirection.Out);
			metadata.AddArgument (rtTo);
			if (To == null)
				To = new OutArgument<object> ();
			metadata.Bind (To, rtTo);
			var rtValue = new RuntimeArgument ("Value", Value.ArgumentType, ArgumentDirection.In);
			metadata.AddArgument (rtValue);
			if (Value == null)
				Value = new InArgument<object> ();
			metadata.Bind (Value, rtValue);
		}

		protected override void Execute (CodeActivityContext context)
		{
			To.Set (context, Value.Get (context));
		}
	}
	
	public sealed class Assign<T> : CodeActivity
	{
		[RequiredArgumentAttribute]
		public OutArgument<T> To { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T> Value { get; set; }

		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			var rtTo = new RuntimeArgument ("To", typeof (T), ArgumentDirection.Out);
			metadata.AddArgument (rtTo);
			if (To == null)
				To = new OutArgument<T> ();
			metadata.Bind (To, rtTo);
			var rtValue = new RuntimeArgument ("Value", typeof (T), ArgumentDirection.In);
			metadata.AddArgument (rtValue);
			if (Value == null)
				Value = new InArgument<T> ();
			metadata.Bind (Value, rtValue);
		}

		protected override void Execute (CodeActivityContext context)
		{
			To.Set (context, Value.Get (context));
		}
	}
}
