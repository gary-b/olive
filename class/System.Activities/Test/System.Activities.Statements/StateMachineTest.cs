using System;
using NUnit.Framework;
using System.Activities.Statements;
using System.Activities;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class StateMachineTest : WFTestHelper {
		static State GetInitialAndFinalState (out State finalState)
		{
			finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Entry = new WriteLine { Text = "entry" },
				Transitions =  {
					new Transition {
						To = finalState
					}
				},
			};
			return initialState;
		}

		[Test]
		public void Ctor ()
		{
			var sm = new StateMachine ();
			Assert.IsNotNull (sm.Variables);
			Assert.IsNull (sm.InitialState);
			Assert.IsNotNull (sm.States);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void InitialState_NullEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': StateMachine 'StateMachine' must have an initial state.
			var sm = new StateMachine { 
				InitialState = null
			};
			WorkflowInvoker.Invoke (sm);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void InitialState_CantBeFinalStateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': Initial state '' must not also be a final state.
			var finalState = new State ();
			finalState.IsFinal = true;

			var sm = new StateMachine { 
				States = { finalState },
				InitialState = finalState
			};
			WorkflowInvoker.Invoke (sm);
		}
		[Test]
		public void Execute_InitialStateAndStates ()
		{
			State finalState;
			var initialState = GetInitialAndFinalState (out finalState);

			var sm = new StateMachine { 
				States = { initialState, finalState },
				InitialState = initialState
			};
			RunAndCompare (sm, String.Format ("entry{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void States_InitialStateMissingEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': Initial state '' must be added to 'States' collection of a state machine.
			State finalState;
			var initialState = GetInitialAndFinalState (out finalState);

			var sm = new StateMachine { 
				States = { finalState },
				InitialState = initialState
			};
			WorkflowInvoker.Invoke (sm);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void States_DeeperStateMissingEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': Target state '' of transition '' must belong to a state machine.
			State finalState;
			var initialState = GetInitialAndFinalState (out finalState);

			var sm = new StateMachine { 
				States = { initialState },
				InitialState = initialState
			};
			WorkflowInvoker.Invoke (sm);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void States_SharedStateWith2StateMachinesEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'': The activity 'WriteLine' cannot be referenced by activity '' because the latter is not in another activity's implementation.  An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  Activity 'WriteLine' is declared by activity ''.
			//'': The activity 'Null Trigger' cannot be referenced by activity '' because the latter is not in another activity's implementation.  An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  Activity 'Null Trigger' is declared by activity ''.
			State finalState;
			var initialState = GetInitialAndFinalState (out finalState);

			var sm = new StateMachine { 
				States = { initialState, finalState },
				InitialState = initialState
			};
			var sm2 = new StateMachine { 
				States = { initialState, finalState },
				InitialState = initialState
			};
			WorkflowInvoker.Invoke (new Sequence { Activities = { sm, sm2 } });
		}
		[Test]
		public void Execute_Variables ()
		{
			var v1 = new Variable<string> ("", "hello");
			var finalState = new State { Entry = new WriteLine { Text = v1 } };
			finalState.IsFinal = true;
			var intialState = new State { 
				Entry = new WriteLine { Text = v1 },
				Transitions = { 
					new Transition { 
						To = finalState, 
						Action = new WriteLine { Text = v1 } 
					}
				},
			};

			var sm = new StateMachine { 
				Variables = { v1 },
				States = { intialState, finalState },
				InitialState = intialState
			};
			RunAndCompare (sm, String.Format ("hello{0}hello{0}hello{0}", Environment.NewLine));
		}
	}
}

