using System;
using System.Collections.ObjectModel;

namespace System.Activities.Statements {
	public class State {
		public bool IsFinal { get; set; }
		public string DisplayName { get; set; }
		public Activity Entry { get; set; }
		public Activity Exit { get; set; }
		public Collection<Transition> Transitions { get; private set; }
		public Collection<Variable> Variables { get; private set; }

		public State ()
		{
			Transitions = new Collection<Transition> ();
			Variables = new Collection<Variable> ();
		}
	}
}

