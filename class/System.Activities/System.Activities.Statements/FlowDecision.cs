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
	public sealed class FlowDecision : FlowNode
	{
		public Activity<bool> Condition { get; set; }
		public FlowNode True { get; set; }
		public FlowNode False { get; set; }
		
		public FlowDecision ()
		{
		}
		public FlowDecision (Activity<bool> condition)
		{
			if (condition == null)
				throw new ArgumentNullException ("condition");
			Condition = condition;
		}
		public FlowDecision (Expression<Func<ActivityContext, bool>> condition)
		{
			throw new NotImplementedException ();
		}
		internal override ICollection<FlowNode> GetChildNodes ()
		{
			var coll = new Collection<FlowNode> ();
			if (True != null)
				coll.Add (True);
			if (False != null)
				coll.Add (False);
			return coll;
		}
		internal override ICollection<Activity> GetActivities ()
		{
			var coll = new Collection<Activity> ();
			if (coll != null)
				coll.Add (Condition);
			return coll;
		}
		internal override void Execute (NativeActivityContext context, Flowchart flowchart)
		{
			context.ScheduleActivity (Condition, flowchart.FlowDecisionCallback);
		}
	}
}
