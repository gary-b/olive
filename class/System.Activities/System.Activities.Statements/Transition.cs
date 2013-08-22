using System;

namespace System.Activities.Statements {
	public class Transition {
		public Activity Trigger { get; set; }
		public Activity Action { get; set; }
		public Activity<bool> Condition { get; set; }
		public string DisplayName { get; set; }
		public State To { get; set; }
		public Transition ()
		{
		}
	}
}

