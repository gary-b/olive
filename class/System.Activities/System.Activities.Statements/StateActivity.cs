using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Activities.Statements {
	internal class StateActivity : NativeActivity<State> {
		internal State State { get; private set; }

		Variable<Bookmark> TriggersExecutedNotifierBK { get; set; }
		Variable<bool> TriggersHaveExecuted  { get; set; }		
		Variable<Queue<string>> CompleteTriggerQueue  { get; set; }		//Activity.Id
		Variable<Queue<int>> SharedTransitionsBeingProcessed  { get; set; } 	//index pos in State.Transitions
		Variable<int> ExitTransition  { get; set; }

		TriggersExecutedNotifier TriggersExecutedNotifier { get; set; }
		EmptyActivity PlaceHolderTrigger { get; set; }

		protected override bool CanInduceIdle {
			get { return true; }
		}

		internal StateActivity (State state)
		{
			if (state == null)
				throw new ArgumentNullException ("state");
			State = state;
			TriggersExecutedNotifierBK = new Variable<Bookmark> ();
			TriggersHaveExecuted = new Variable<bool> ();
			CompleteTriggerQueue = new Variable<Queue<string>> ();
			SharedTransitionsBeingProcessed = new Variable<Queue<int>> ();
			ExitTransition = new Variable<int> ("", -1);

			TriggersExecutedNotifier = new TriggersExecutedNotifier { 
				BookmarkTrigger =  TriggersExecutedNotifierBK
			};
			PlaceHolderTrigger = new EmptyActivity ();
		}

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			metadata.AddImplementationVariable (TriggersExecutedNotifierBK);
			metadata.AddImplementationVariable (TriggersHaveExecuted);
			metadata.AddImplementationVariable (CompleteTriggerQueue);
			metadata.AddImplementationVariable (SharedTransitionsBeingProcessed);
			metadata.AddImplementationVariable (ExitTransition);
			metadata.AddImplementationChild (TriggersExecutedNotifier);

			foreach (var var in State.Variables)
				metadata.AddVariable (var);
			if (State.Entry != null)
				metadata.AddChild (State.Entry);
			if (State.Exit != null)
				metadata.AddChild (State.Exit);
			var triggers = new List<Activity> ();
			foreach (var t in State.Transitions) {
				if (t.Trigger != null) //FIMXE: support null triggers
					triggers.Add (t.Trigger);
				if (t.Condition != null)
					metadata.AddChild (t.Condition);
				if (t.Action != null)
					metadata.AddChild (t.Action);
			}
			foreach (var trigger in triggers.Distinct ())
				metadata.AddChild (trigger);
			if (State.Transitions.Any (t => t.Trigger == null))
				metadata.AddChild (PlaceHolderTrigger);


		}
		protected override void Execute (NativeActivityContext context)
		{
			CompleteTriggerQueue.Set (context, new Queue<string> ());
			SharedTransitionsBeingProcessed.Set (context, new Queue<int> ());
			//the Final state should just schedule Entry if present and stop, which will allow
			//the parent StateMachine activity to complete. (Result arg left at null)
			if (State.Entry == null) {
				if (!State.IsFinal)
					StartTriggersCB (context, null);
			} else {
				if (State.IsFinal)
					context.ScheduleActivity (State.Entry);
				else
					context.ScheduleActivity (State.Entry, StartTriggersCB);
			}
		}
		IList<int>  GetTransitionsIndexesWithTrigger (string activityId)
		{	
			//finds the transitions whose trigger's id matches activityId
			//result returned as a queue of these transitions index position
			var transitions = State.Transitions.Where (
				t => t.Trigger != null && t.Trigger.Id == activityId).ToList();
			if (!transitions.Any ()) {
				if (PlaceHolderTrigger.Id == activityId) {
					transitions = State.Transitions.Where (
						t => t.Trigger == null).ToList();
				} else {
					throw new Exception ("no triggers found with this Activity.Id");
				}
			}
			return transitions.Select (t => State.Transitions.IndexOf (t)).ToList ();
		}
		Transition GetTransitionFromCondition (Activity activity)
		{
			return State.Transitions.Single (t => t.Condition == activity);
		}
		void StartTriggersCB (NativeActivityContext context, ActivityInstance compAI)
		{
			var bk = context.CreateBookmark (TriggersExecutedNotifierCB, 
			                                 BookmarkOptions.MultipleResume | BookmarkOptions.NonBlocking);
			TriggersExecutedNotifierBK.Set (context, bk);
			var triggers = new List<Activity> ();
			//schedule EmptyActivity for null trigger 
			if (State.Transitions.Any (t => t.Trigger == null))
				triggers.Add (PlaceHolderTrigger);
			//only schedule shared triggers once
			triggers.AddRange (State.Transitions.Where (
				t => t.Trigger != null).Select (
				t => t.Trigger).Distinct ());
			ScheduleTriggers (context, triggers);
		}
		void TriggersExecutedNotifierCB (NativeActivityContext context, Bookmark bookmark, object value)
		{
			TriggersHaveExecuted.Set (context, true);
			ProcessCompleteTriggers (context);
		}
		void TriggerCompleteCB (NativeActivityContext context, ActivityInstance compAI)
		{
			CompleteTriggerQueue.Get (context).Enqueue (compAI.Activity.Id);
			ProcessCompleteTriggers (context);
		}
		void ProcessCompleteTriggers (NativeActivityContext context)
		{
			if (!TriggersHaveExecuted.Get (context) || !CompleteTriggerQueue.Get (context).Any())
				return;
			string triggerId = CompleteTriggerQueue.Get (context).Dequeue ();
			var transIdxs = GetTransitionsIndexesWithTrigger (triggerId);
			if (transIdxs.Count == 1) { 
				var transition = State.Transitions [transIdxs [0]];
				if (transition.Condition == null)
					ExitState (context, transition);
				else
					context.ScheduleActivity (transition.Condition, ConditionCB);
			} else { 
				//shared trigger
				foreach (var idx in transIdxs)
					SharedTransitionsBeingProcessed.Get (context).Enqueue (idx);
				ProcessSharedTrigger (context, null);
			}
		}
		void ProcessSharedTrigger (NativeActivityContext context, Transition lastTransition)
		{
			//lastTransition only used when rescheduling trigger. The transitions that need 
			//processed are stored in the SharedTransitionsBeingProcessed Queue variable, 
			//(which would be empty in the case of the trigger needing rescheduled)
			if (!SharedTransitionsBeingProcessed.Get (context).Any ()) {
				ScheduleTrigger (context, lastTransition.Trigger ?? PlaceHolderTrigger);
				return;
			}
			var nextTransIdx = SharedTransitionsBeingProcessed.Get (context).Dequeue ();
			context.ScheduleActivity (State.Transitions [nextTransIdx].Condition, 
			                          SharedTransitionConditionCB);
		}
		void ExitState (NativeActivityContext context, Transition transition)
		{
			ExitTransition.Set (context, State.Transitions.IndexOf (transition));
			context.CancelChildren (); 
			if (State.Exit == null)
				RunExitTransactionActionCB (context, null);
			else
				context.ScheduleActivity (State.Exit, RunExitTransactionActionCB);
		}
		void RunExitTransactionActionCB (NativeActivityContext context, ActivityInstance compAI)
		{
			var transition = State.Transitions [ExitTransition.Get (context)];
			if (transition.Action == null)
				MoveToNextStateCB (context, null);
			else
				context.ScheduleActivity (transition.Action, MoveToNextStateCB);
		}
		void MoveToNextStateCB (NativeActivityContext context, ActivityInstance compAI)
		{
			var transition = State.Transitions [ExitTransition.Get (context)];
			Result.Set (context, transition.To); //FIXME: potential persistence issue?
		}
		void ConditionCB (NativeActivityContext context, ActivityInstance compAI, bool result)
		{
			var transition = GetTransitionFromCondition (compAI.Activity);
			if (result)
				ExitState (context, transition);
			else
				ScheduleTrigger (context, transition.Trigger ?? PlaceHolderTrigger);
		}
		void SharedTransitionConditionCB (NativeActivityContext context, ActivityInstance compAI, bool result)
		{
			var transition = GetTransitionFromCondition (compAI.Activity);
			if (result)
				ExitState (context, transition);
			else 
				ProcessSharedTrigger (context, transition);
		}
		void ScheduleTriggers (NativeActivityContext context, ICollection<Activity> triggers)
		{	
			/* Triggers are scheduled for 2 reasons,
			 *  1) when the state is entered the triggers on all transitions are scheduled
			 *  2) when a condition activity on a transition returns false its trigger is rescheduled
			 * Need to track when newly scheduled triggers have all either completed or blocked because
			 *  a) we dont want to start processing transitions whose triggers have completed until
			 *    all scheduled triggers have either completed or blocked like .NET does.
			 *  b) when a trigger is rescheduled after its Condition activity returned false, if that trigger 
			 *    proceeds to block we need to continue processing any other transitions whose triggers have
			 *    also completed.
			 * We also do not want to start processing transitions until any resumed bookmarks have complete.
			 * The TriggerCompleteQueue, TriggersHaveExecuted flag, TriggersExecutedNotifier activity
			 * and TriggersExecutedNotifierBK bookmark make it so.
			*/
			TriggersHaveExecuted.Set (context, false);
			context.ScheduleActivity (TriggersExecutedNotifier);
			foreach (var trigger in triggers)
				context.ScheduleActivity (trigger, TriggerCompleteCB);
		}
		void ScheduleTrigger (NativeActivityContext context, Activity trigger)
		{
			ScheduleTriggers (context, new Collection<Activity> { trigger });
		}
	}
	internal class TriggersExecutedNotifier : NativeActivity {
		internal InArgument<Bookmark> BookmarkTrigger { get; set; }
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			var rtBookmarkTrigger = new RuntimeArgument ("BookmarkTrigger", typeof (Bookmark), ArgumentDirection.In);
			metadata.AddArgument (rtBookmarkTrigger);
			metadata.Bind (BookmarkTrigger, rtBookmarkTrigger);
		}
		protected override void Execute (NativeActivityContext context)
		{
			context.ResumeBookmark (BookmarkTrigger.Get (context), null);
		}
	}
	internal class EmptyActivity : Activity {
		internal EmptyActivity ()
		{
		}
	}
}

