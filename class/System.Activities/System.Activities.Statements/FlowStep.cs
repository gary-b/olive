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
	[ContentProperty ("Action")]
	public sealed class FlowStep : FlowNode
	{
		public Activity Action { get; set; }
		public FlowNode Next { get; set; }

		internal override ICollection<FlowNode> GetChildNodes ()
		{
			var coll = new Collection<FlowNode> ();
			if (Next != null)
				coll.Add (Next);
			return coll;
		}
		internal override ICollection<Activity> GetActivities ()
		{
			var coll = new Collection<Activity> ();
			coll.Add (Action);
			return coll;
		}
		internal override void Execute (NativeActivityContext context, Flowchart flowchart)
		{
			if (Action != null)
				context.ScheduleActivity (Action, flowchart.FlowStepCallback);
		}
	}
}
