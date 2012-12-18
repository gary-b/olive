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
	[ContentProperty ("Activities")]
	public sealed class Sequence : NativeActivity
	{
		Collection<Activity> activities = new Collection<Activity> ();
		Collection<Variable> variables = new Collection<Variable> ();
		public Collection<Activity> Activities { get { return activities; } }
		public Collection<Variable> Variables { get { return variables; } }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (Variables != null) {
				foreach (var v in Variables)
					metadata.AddVariable (v);
			}
			if (Activities != null) {
				foreach (var a in Activities)
					metadata.AddChild (a);
			}
		}

		protected override void Execute (NativeActivityContext context)
		{
			// FIXME: TEMPORARY IMPLEMENTATION: SHOULD SCHEDULE NEXT ACTIVITY ONLY WHEN LAST ONE COMPLETE
			// USING ScheduleActivity (Activity, OnCompletionCallback)

			if (Activities != null) {
				// reverse order as ScheduleActivity has LIFO execution order
				foreach (var a in Activities.Reverse ())
					context.ScheduleActivity (a);
			}
		}
	}
}
