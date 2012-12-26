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

	public abstract class Variable : LocationReference
	{
		string varName; //FIXME: unsure of purpose of NameCore, Name

		internal Variable ()
		{
			Modifiers = VariableModifiers.None;
		}

		[IgnoreDataMemberAttribute]
		public ActivityWithResult Default { get; set; }
		public VariableModifiers Modifiers { get; set; }
		public new string Name { 
			get { return varName; } 
			set { varName = value; } 
		}
		protected override string NameCore { get { return varName; } }

		public static Variable Create (string name, Type type, VariableModifiers modifiers)
		{
			throw new NotImplementedException ();
		}

		public void Set (ActivityContext context, object value)
		{
			throw new NotImplementedException ();
		}

		public object Get (ActivityContext context)
		{
			// FIXME: test
			return context.GetLocation ((LocationReference) this).Value;
		}
	}
	
	public sealed class Variable<T> : Variable
	{
		Type varType; //FIXME: unsure of purpose of TypeCore

		public Variable () : base ()
		{
			varType = typeof (T);
		}
		public Variable (Expression<Func<ActivityContext, T>> defaultExpression)
		{
			throw new NotImplementedException ();
		}
		public Variable (string name) : this ()
		{
			Name = name;
		}
		public Variable (string name, Expression<Func<ActivityContext, T>> defaultExpression)
		{
			throw new NotImplementedException ();
		}
		public Variable (string name, T defaultValue) : this (name)
		{
			Default = new Literal<T> (defaultValue);
		}

		public new Activity<T> Default { 
			get {
				// FIXME: see commented out part of test, .NET seems to have handling for base.Default not being correct type
				return (Activity<T>) base.Default; 
			}
			set {
				base.Default = value;
			}
		}

		protected override Type TypeCore {
			get { return varType; }
		}

		public new T Get (ActivityContext context)
		{
			return context.GetValue<T> ((LocationReference) this);
		}

		public void Set (ActivityContext context, T value)
		{
			context.SetValue ((LocationReference) this, (T) value);
		}

		public override Location GetLocation (ActivityContext context)
		{
			return context.GetLocation<T> (this);
		}
	}
}
