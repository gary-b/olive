using System;
using NUnit.Framework;
using System.Activities.Statements;
using System.Activities;
using System.Activities.Expressions;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class StateTest : WFTestHelper {
		static State GetBareInitialAndFinalState (out State finalState)
		{
			finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						To = finalState
					}
				},
			};
			return initialState;
		}
		void RunInSMAndCompare (State initialState, State finalState, string expected)
		{
			var sm = new StateMachine {
				States =  {
					initialState,
					finalState
				},
				InitialState = initialState
			};
			RunAndCompare (sm, expected);
		}
		[Test]
		public void Ctor ()
		{
			var state = new State ();
			Assert.IsNull (state.DisplayName);
			Assert.IsNull (state.Entry);
			Assert.IsNull (state.Exit);
			Assert.IsFalse (state.IsFinal);
			Assert.IsNotNull (state.Transitions);
			Assert.IsNotNull (state.Variables);
		}
		[Test]
		public void DisplayName ()
		{
			var state = new State ();
			Assert.IsNull (state.DisplayName);
			state.DisplayName = "Bob";
			Assert.AreEqual ("Bob", state.DisplayName);
			state.DisplayName = null;
			Assert.IsNull (state.DisplayName);
		}
		[Test]
		public void Execute_EntryAndExitNull ()
		{
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			RunInSMAndCompare (initialState, finalState, "");
		}
		[Test]
		public void Execute_Order_StateAndTransitionActivityExecutionOrder ()
		{
			var condition = new CodeActivityTRunner<bool> (null, (context) => {
				Console.WriteLine ("Condition");
				return true;
			});
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FinalEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Entry = new WriteLine { Text = "InitialEntry" },
				Exit = new WriteLine { Text = "InitialExit" },
				Transitions =  {
					new Transition {
						Trigger = new WriteLine { Text = "Trigger" },
						Action = new WriteLine { Text = "Action" },
						Condition = condition,
						To = finalState
					}
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("InitialEntry{0}Trigger{0}Condition{0}InitialExit{0}" +
			               "Action{0}FinalEntry{0}", Environment.NewLine));
		}
		public Activity GetBookmarkTrigger (string bookmarkName, string executeMessage)
		{
			var act = new NativeActivityRunner (null, (context) => {
				Console.WriteLine (executeMessage);
				context.CreateBookmark (bookmarkName, (ctx, bk, value) => {
					Console.WriteLine (value);
				});
			});
			act.InduceIdle = true;
			return act;
		}
		[Test]
		public void Execute_WaitsForEntryAndExit ()
		{
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Entry = GetBookmarkTrigger ("bk1", "IEntry"),
				Exit = GetBookmarkTrigger ("bk2", "IExit"),
				Transitions =  {
					new Transition {
						Action = new WriteLine { Text = "Action" },
						To = finalState
					}
				},
			};

			var sm = new StateMachine { InitialState = initialState, 
				States = { initialState, finalState } };

			var app = new WFAppWrapper (sm);
			app.Run ();
			Assert.AreEqual (String.Format ("IEntry{0}", Environment.NewLine), app.ConsoleOut);
			app.ResumeBookmark ("bk1", "bk1");
			Assert.AreEqual (String.Format ("IEntry{0}bk1{0}IExit{0}", Environment.NewLine), app.ConsoleOut);
			app.ResumeBookmark ("bk2", "bk2");
			Assert.AreEqual (String.Format ("IEntry{0}bk1{0}IExit{0}bk2{0}Action{0}FEntry{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_IsFinalTrue_ExitHasActivityEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': Final state '' must not have an Exit action.
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			finalState.Exit = new WriteLine { Text = "Hello" };
			RunInSMAndCompare (initialState, finalState, "");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_IsFinalFalse_WhenFinalStateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': State '' must have at least 1 transition.
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			finalState.IsFinal = false;
			RunInSMAndCompare (initialState, finalState, "");
		}
		[Test]
		public void Execute_IsFinalTrue_EntryHasActivityOk ()
		{
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			finalState.Entry = new WriteLine { Text = "Entry" };
			RunInSMAndCompare (initialState, finalState, "Entry" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_Transitions_DupeEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': Transition '' cannot be added to state '' because it has been added to state ''.
			//'': The activity 'Literal<Boolean>' cannot be referenced by activity '' because the latter is not in another 
			//activity's implementation.  An activity can only be referenced by the implementation of an activity which 
			//specifies that activity as a child or import.  Activity 'Literal<Boolean>' is declared by activity ''.
			var finalState = new State ();
			var transition = new Transition {
				Trigger = new WriteLine { Text = "Trigger" },
				Condition = new Literal<bool> (true),
				To = finalState
			};
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					transition,
					transition
				},
			};
			RunInSMAndCompare (initialState, finalState, null);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_Transitions_TransitionUsedIn2StatesEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': Transition '' cannot be added to state '' because it has been added to state ''.
			//'': The activity 'WriteLine' cannot be referenced by activity '' because the latter is not in another activity's implementation.  An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  Activity 'WriteLine' is declared by activity ''.
			//'': The activity 'Literal<Boolean>' cannot be referenced by activity '' because the latter is not in another activity's implementation.  An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  Activity 'Literal<Boolean>' is declared by activity ''.

			var finalState = new State ();
			finalState.IsFinal = true;
			var transitionToFinal = new Transition {
				Trigger = new WriteLine (),
				Condition = new Literal<bool> (true),
				To = finalState
			};
			var state2 = new State {
				Transitions =  {
					transitionToFinal,
				},
			};
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = new WriteLine (),
						Condition = new Literal<bool> (true),
						To = state2
					},
					transitionToFinal
				},
			};

			var sm = new StateMachine { InitialState = initialState, 
				States = { initialState, state2, finalState } };

			RunAndCompare (sm, null);
		}
		[Test]
		[Ignore ("VariableValue's access to variables different when child and arg.expression")]
		public void Execute_Variables ()
		{
			var vBool = new Variable<bool> ("", true);
			var vStr = new Variable<string> ("", "hello");
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FinalEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Variables = { vBool, vStr },
				Entry = new WriteLine { Text = vStr},
				Exit = new WriteLine { Text = vStr },
				Transitions =  {
					new Transition {
						Trigger = new WriteLine { Text = "Trigger" },
						Action = new WriteLine { Text = "Action" },
						Condition = new VariableValue<bool> (vBool),
						To = finalState
					}
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("hello{0}Trigger{0}hello{0}" +
			               "Action{0}FinalEntry{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_Variables_CantAccessFromSiblingStateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': Variable '' must be included in an activity before it is used.
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  
			//There may be another location reference with the same name that is visible at this scope, but it does not reference the same location.

			var vStr = new Variable<string> ("", "hello");
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			initialState.Variables.Add (vStr);
			finalState.Entry = new WriteLine { Text = vStr };
			RunInSMAndCompare (initialState, finalState, "");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_EntryAndExitShareActivityEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'': The activity 'WriteLine' cannot be referenced by activity '' because the latter is not in another activity's implementation.  
			//An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  
			//Activity 'WriteLine' is declared by activity ''.
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			var writer = new WriteLine { Text = "writer" };
			initialState.Entry = writer;
			initialState.Exit = writer;
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("writer{0}writer{0}", Environment.NewLine));
		}
		[Test]
		public void ToStringTest ()
		{
			var state = new State ();
			Assert.AreEqual (state.GetType ().FullName, state.ToString ());
			state.DisplayName = "Bob";
			Assert.AreEqual (state.GetType ().FullName, state.ToString ());
		}
	}
}

