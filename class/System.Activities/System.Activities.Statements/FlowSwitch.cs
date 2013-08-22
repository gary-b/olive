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
{	[ContentProperty ("Cases")]
	public sealed class FlowSwitch<T> : FlowNode
	{
		public Activity<T> Expression { get; set; }
		public FlowNode Default { get; set; }
		public IDictionary<T, FlowNode> Cases { get; private set;}
		
		public FlowSwitch ()
		{
			Cases = new NullDictionary<T, FlowNode> ();
		}
		internal override ICollection<FlowNode> GetChildNodes ()
		{
			var coll = new List<FlowNode> (Cases.Values.Where (n => n != null));
			if (Default != null)
				coll.Add (Default);
			return coll;
		}
		internal override ICollection<Activity> GetActivities ()
		{
			var coll = new Collection<Activity> ();
			coll.Add (Expression);
			return coll;
		}
		internal override void Execute (NativeActivityContext context, Flowchart flowchart)
		{
			context.ScheduleActivity (Expression, flowchart.FlowSwitchCallback<T>);
		}
	}
}
