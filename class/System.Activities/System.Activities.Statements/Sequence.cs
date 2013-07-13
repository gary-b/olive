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
		Variable<int> vIndex = new Variable<int> ();
		Collection<Activity> activities = new Collection<Activity> ();
		Collection<Variable> variables = new Collection<Variable> ();
		public Collection<Activity> Activities { get { return activities; } }
		public Collection<Variable> Variables { get { return variables; } }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			foreach (var v in Variables)
				metadata.AddVariable (v);
			foreach (var a in Activities)
				metadata.AddChild (a);
			metadata.AddImplementationVariable (vIndex);
		}

		protected override void Execute (NativeActivityContext context)
		{
			vIndex.Set (context, 0);
			if (Activities.Count == 0)
				return;
			context.ScheduleActivity (Activities [0], Callback);
		}

		void Callback (NativeActivityContext context, object value)
		{
			int nextIndex = vIndex.Get (context) + 1;
			if (nextIndex < Activities.Count)
				context.ScheduleActivity (Activities [nextIndex], Callback);
			vIndex.Set (context, nextIndex);
		}
	}
}
