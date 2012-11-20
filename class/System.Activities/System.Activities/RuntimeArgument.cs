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
	public sealed class RuntimeArgument : LocationReference
	{
		string argName; //FIXME: unsure of purpose of NameCore, TypeCore, Name, NameCore
		Type argType;
		ReadOnlyCollection<string> overloadGroupNames;

		public RuntimeArgument (string name, Type argumentType, 
		                        ArgumentDirection direction) : this (name, argumentType,
                                                                             direction, false,
                                                                             new List<string> ())
		{
		}

		public RuntimeArgument (string name, Type argumentType, 
		                        ArgumentDirection direction, bool isRequired) : this (name, argumentType,
					                                                      direction, isRequired,
					                                                      new List<string> ())
		{
		}

		public RuntimeArgument (string name, Type argumentType, 
		                        ArgumentDirection direction, 
		                        List<string> overloadGroupNames) : this (name, argumentType,
					                                        direction, false,
		                                        			overloadGroupNames) 
		{
		}

		public RuntimeArgument (string name, Type argumentType, ArgumentDirection direction, 
		                        bool isRequired, List<string> overloadGroupNames)
		{
			if (String.IsNullOrEmpty (name))
				throw new ArgumentException ("Cannot be null or empty", "name");
			if (argumentType == null)
				throw new ArgumentNullException ("argumentType");
			if (overloadGroupNames == null)
				overloadGroupNames = new List<string> ();

			this.argName = name;
			this.argType = argumentType;
			this.Direction = direction;
			this.IsRequired = isRequired;
			this.overloadGroupNames = new ReadOnlyCollection<string> (overloadGroupNames);
		}

		public ArgumentDirection Direction { get; private set; }

		public bool IsRequired { get; private set; }

		public ReadOnlyCollection<string> OverloadGroupNames { 
			get { return overloadGroupNames; } 
		}

		protected override string NameCore {
			get { return argName; }
		}

		protected override Type TypeCore {
			get { return argType; }
		}

		public object Get (ActivityContext context)
		{
			//FIXME: test
			return context.GetValue (this);
		}

		public T Get<T> (ActivityContext context)
		{
			//FIXME: test
			return (T) context.GetValue (this);
		}

		public override Location GetLocation (ActivityContext context)
		{
			throw new NotImplementedException ();
		}

		public void Set (ActivityContext context, object value)
		{
			//FIXME: test
			context.SetValue (this, value);
		}
	}
}
