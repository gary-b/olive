using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Activities.Statements {
	public class StateMachine : NativeActivity {

		public State InitialState { get; set; }
		public Collection<State> States { get; private set; }
		public Collection<Variable> Variables { get; private set; }
		Collection<StateActivity> StateActivities { get; set; }

		public StateMachine ()
		{
			States = new Collection<State> ();
			Variables = new Collection<Variable> ();
			StateActivities = new Collection<StateActivity> ();
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			foreach (var var in Variables)
				metadata.AddVariable (var);
			foreach (var state in States) {
				var stateAct = new StateActivity (state);
				StateActivities.Add (stateAct);
				metadata.AddChild (stateAct);
			}
		}
		protected override void Execute (NativeActivityContext context)
		{
			context.ScheduleActivity (GetStateActivity (InitialState), StateActivityCB);
		}
		void StateActivityCB (NativeActivityContext context, ActivityInstance compAI, State state)
		{
			if (state != null)
				context.ScheduleActivity (GetStateActivity (state), StateActivityCB);
		}
		StateActivity GetStateActivity (State state)
		{
			return StateActivities.Single (a => a.State == state);
		}
	}
}

