using System;
using NUnit.Framework;
using System.Activities.Statements;
using System.Activities;
using System.Activities.Expressions;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class TransitionTest : WFTestHelper {
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
			var t = new Transition ();
			Assert.IsNull (t.Action);
			Assert.IsNull (t.Condition);
			Assert.IsNull (t.DisplayName);
			Assert.IsNull (t.To);
			Assert.IsNull (t.Trigger);
		}
		[Test]
		public void DisplayName ()
		{
			var t = new Transition ();
			Assert.IsNull (t.DisplayName);
			t.DisplayName = "Bob";
			Assert.AreEqual ("Bob", t.DisplayName);
			t.DisplayName = null;
			Assert.IsNull (t.DisplayName);
		}
		[Test]
		public void Execute_ActionAndConditionAndTriggerNull ()
		{
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			initialState.Entry = new WriteLine { Text = "InitialEntry" };
			initialState.Exit = new WriteLine { Text = "InitialExit" };
			finalState.Entry = new WriteLine { Text = "FinalEntry" };
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("InitialEntry{0}InitialExit{0}FinalEntry{0}",Environment.NewLine));
			Assert.IsNull (initialState.Transitions[0].Trigger);
		}
		[Test]
		public void Execute_WaitsForAction ()
		{
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Exit = new WriteLine { Text = "IExit" },
				Transitions =  {
					new Transition {
						Action = GetBookmarkTrigger ("bk1", "IAction"),
						To = finalState
					}
				},
			};

			var sm = new StateMachine { InitialState = initialState, 
				States = { initialState, finalState } };

			var app = new WFAppWrapper (sm);
			app.Run ();
			Assert.AreEqual (String.Format ("IExit{0}IAction{0}", Environment.NewLine), app.ConsoleOut);
			app.ResumeBookmark ("bk1", "bk1");
			Assert.AreEqual (String.Format ("IExit{0}IAction{0}bk1{0}FEntry{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test]
		public void Execute_Condition ()
		{
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			int count = 0;
			var condition = new CodeActivityTRunner<bool> (null, (context) => {
				if (count++ < 2) {
					Console.WriteLine ("C_False");
					return false;
				}
				Console.WriteLine ("C_True");
				return true;
			});
			initialState.Exit = new WriteLine { Text = "IExit" };
			initialState.Transitions [0].Trigger = new WriteLine { Text = "T" };
			initialState.Transitions [0].Condition = condition;
			initialState.Transitions [0].Action = new WriteLine { Text = "A" };
			finalState.Entry = new WriteLine { Text = "FEntry" };
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T{0}C_False{0}T{0}C_False{0}T" +
			               "{0}C_True{0}IExit{0}A{0}FEntry{0}",Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void To_NullEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': 'To' property of transition '' of state '' must not be null.
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			initialState.Transitions [0].To = null;
			RunInSMAndCompare (initialState, finalState, "");
		}
		[Test]
		public void To_SameState ()
		{
			int i = 0;
			var condition = new CodeActivityTRunner<bool> (null, (context) => {
				if (i++ < 1) {
					Console.WriteLine ("T1C_True");
					return true;
				}
				Console.WriteLine ("T1C_False");
				return false;
			});
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Entry = new WriteLine { Text = "IEntry"},
				Exit = new WriteLine { Text = "IExit" },
				Transitions =  {
					new Transition {
						Trigger = new WriteLine { Text = "T2Trigger" },
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = new WriteLine { Text = "T1Trigger" },
						Action = new WriteLine { Text = "T1Action" },
						Condition = condition,
					}
				},
			};
			initialState.Transitions [1].To = initialState;
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("IEntry{0}T1Trigger{0}T2Trigger{0}T1C_True{0}IExit{0}T1Action{0}" +
			               "IEntry{0}T1Trigger{0}T2Trigger{0}T1C_False{0}T1Trigger{0}IExit{0}T2Action{0}FEntry{0}",Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_TriggerAndConditionShareActivityEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'': The activity 'Literal<Boolean>' cannot be referenced by activity '' because the latter is not in another activity's 
			//implementation.  An activity can only be referenced by the implementation of an activity which specifies that activity 
			//as a child or import.  Activity 'Literal<Boolean>' is declared by activity ''.

			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			var litBool = new Literal<bool> (true);
			initialState.Transitions [0].Trigger = litBool;
			initialState.Transitions [0].Condition = litBool;
			RunInSMAndCompare (initialState, finalState, "");
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_TriggerAndActionShareActivityEx ()
		{
			State finalState;
			var initialState = GetBareInitialAndFinalState (out finalState);
			var litBool = new Literal<bool> (true);
			initialState.Transitions [0].Trigger = litBool;
			initialState.Transitions [0].Action = litBool;
			RunInSMAndCompare (initialState, finalState, "");
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
		public void Execute_MultipleTransitions_Trigger_AllAllowedToCompleteOrBlock ()
		{
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = GetBookmarkTrigger ("bk2", "T4"),
						To = finalState
					},
					new Transition {
						Trigger = new WriteLine { Text = "T3" },
						To = finalState
					},
					new Transition {
						Trigger = new WriteLine { Text = "T2" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk1", "T1"),
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T1{0}T2{0}T3{0}T4{0}FEntry{0}", Environment.NewLine));
		}
		Activity<bool> GetConditionWriter (string message, bool result) 
		{
			return new CodeActivityTRunner<bool> (null, (context) => {
				Console.WriteLine (message);
				return result;
			});
		}
		[Test]
		public void Execute_MultipleTransitions_1stToExecuteCompletes ()
		{
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = new WriteLine { Text = "T2" },
						Condition = GetConditionWriter ("T2Condition", true),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = new WriteLine { Text = "T1" },
						Condition = GetConditionWriter ("T1Condition", true),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T1{0}T2{0}T1Condition{0}T1Action{0}FEntry{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_MultipleTransitions_Trigger_Null_NoConditionEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'StateMachine': Trigger-less transition '' of state '' must contain a condition.  A state can only have one unconditional transition that has no trigger.
			//'StateMachine': Trigger-less transition '' of state '' must contain a condition.  A state can only have one unconditional transition that has no trigger.
			var finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition { To = finalState },
					new Transition { To = finalState },
				},
			};
			RunInSMAndCompare (initialState, finalState, null);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_MultipleTransitions_Trigger_Null_OnlyOneHasConditionEx ()
		{
			var finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition { To = finalState, Condition = new Literal<bool> (true) },
					new Transition { To = finalState },
				},
			};
			RunInSMAndCompare (initialState, finalState, null);
		}
		[Test]
		public void Execute_MultipleTransitions_Trigger_Null_HasConditions_OrderChanges ()
		{
			var finalState = new State ();
			finalState.IsFinal = true;
			finalState.Entry = new WriteLine { Text = "FinalEntry" };
			var initialState = new State {
				Transitions =  {
					new Transition {
						Condition = GetConditionWriter ("T2Condition", true),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Condition = GetConditionWriter ("T1Condition", true),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				}
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T2Condition{0}T2Action{0}FinalEntry{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_MultipleTransitions_Trigger_WaitsForBookmarkResumptionsOnOtherTriggers ()
		{
			var resumer = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("T3");
				context.ResumeBookmark (new Bookmark ("bk2"), "bk2");
				context.ResumeBookmark (new Bookmark ("bk1"), "bk1");
			});
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = resumer,
						Action = new WriteLine { Text = "T3Action" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk2", "T2"),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk1", "T1"),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T1{0}T2{0}T3{0}bk2{0}bk1{0}T3Action{0}FEntry{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_MultipleTransitions_Trigger_WaitsForBookmarkResumptionsOnOtherTriggers2 ()
		{
			bool block = false;
			var resumer = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("T3");
				context.ResumeBookmark (new Bookmark ("bk2"), "bk2");
				context.ResumeBookmark (new Bookmark ("bk1"), "bk1");
				if (block)
					context.CreateBookmark();
				block = true;
			});
			resumer.InduceIdle = true;
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = resumer,
						Condition = GetConditionWriter ("T3Con", false),
						Action = new WriteLine { Text = "T3Action" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk2", "T2"),
						Condition = GetConditionWriter ("T2Con", true),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk1", "T1"),
						Condition = GetConditionWriter ("T1Con", true),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T1{0}T2{0}T3{0}bk2{0}bk1{0}T3Con{0}T3{0}T2Con{0}T2Action{0}FEntry{0}", Environment.NewLine));
		}
		class CancelTracker : NativeActivityRunner {
			string cancelMessage;
			public CancelTracker (Action<NativeActivityMetadata> cacheMetadata, 
			                      Action<NativeActivityContext> execute,
			                     string cancelMessage) : base (cacheMetadata, execute)
			{
				this.cancelMessage = cancelMessage;
			}
			protected override void Cancel (NativeActivityContext context)
			{
				Console.WriteLine (cancelMessage);
				context.CancelChildren ();
				context.RemoveAllBookmarks ();
			}
		}
		[Test]
		[Ignore ("CancelChildren support")]
		public void Execute_MultipleTransitions_Trigger_BlockedAndScheduledTriggersCANCELLEDWhen1Completes ()
		{
			var resumer = new CancelTracker (null, (context) => {
				Console.WriteLine ("T3");
				context.ResumeBookmark (new Bookmark ("bk1"), "bk1");
			}, "T3Cancelled");
			var t2Trigger = new CancelTracker (null, (context) => {
				Console.WriteLine ("T2");
				context.CreateBookmark ();
			}, "T2Cancelled");
			t2Trigger.InduceIdle = true;
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = resumer,
						Condition = GetConditionWriter ("T3Con", false),
						Action = new WriteLine { Text = "T3Action" },
						To = finalState
					},
					new Transition {
						Trigger = t2Trigger,
						Condition = GetConditionWriter ("T2Con", true),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk1", "T1"),
						Condition = GetConditionWriter ("T1Con", true),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T1{0}T2{0}T3{0}bk1{0}T3Con{0}T3{0}T1Con{0}T2Cancelled{0}T1Action{0}FEntry{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_MultipleTransitions_WithBookmarks_OneResumed ()
		{
			var resumer = new NativeActivityRunner (null, (context) => {
				Console.WriteLine ("T3");
				context.ResumeBookmark (new Bookmark ("bk2"), "bk2");
			});
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = resumer,
						Condition = new Literal<bool> (false),
						Action = new WriteLine { Text = "T3Action" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk2", "T2"),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = GetBookmarkTrigger ("bk1", "T1"),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("T1{0}T2{0}T3{0}bk2{0}T3{0}T2Action{0}FEntry{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_MultipleTransitions_SharedTrigger_ConditionsEvalInOrderUserAdded1 ()
		{
			var trigger = new WriteLine { Text = "trigger" };
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T2Con", true),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T1Con", true),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("trigger{0}T2Con{0}T2Action{0}FEntry{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_MultipleTransitions_SharedTrigger_ConditionsEvalInOrderUserAdded2 ()
		{
			var trigger = new WriteLine { Text = "trigger" };
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T2Con", false),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T1Con", true),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("trigger{0}T2Con{0}T1Con{0}T1Action{0}FEntry{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_MultipleTransitions_SharedTrigger_ConditionFalseOnBoth_3rdTriggerPresent ()
		{
			var trigger = new WriteLine { Text = "SharedT" };
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = new WriteLine { Text = "T3" },
						Condition = GetConditionWriter ("T3Con", true),
						Action = new WriteLine { Text = "T3Action" },
						To = finalState
					},
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T2Con", false),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T1Con", false),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					}
				},
			};
			RunInSMAndCompare (initialState, finalState, 
			                   String.Format ("SharedT{0}T3{0}T2Con{0}T1Con{0}SharedT{0}T3Con{0}T3Action{0}FEntry{0}", Environment.NewLine));
		}
		class ConditionBookmarker : NativeActivity<bool> {
			string bookmarkName;
			string executeMessage;
			protected override bool CanInduceIdle {
				get { return true; }
			}
			public ConditionBookmarker (string bookmarkName, string executeMessage)
			{
				this.executeMessage = executeMessage;
				this.bookmarkName = bookmarkName;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
			}
			protected override void Execute (NativeActivityContext context)
			{
				Console.WriteLine (executeMessage);
				context.CreateBookmark (bookmarkName, BookmarkCallback);
			}
			void BookmarkCallback (NativeActivityContext context, Bookmark bookmark, object value)
			{
				Console.WriteLine (value);
				Result.Set (context, true);
			}
		}
		[Test]
		public void Execute_WaitsForCondition ()
		{
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = new WriteLine { Text = "T2" },
						Condition = new ConditionBookmarker ("bk2", "Con2"),
						Action = new WriteLine { Text = "T2Action" },
						To = finalState
					},
					new Transition {
						Trigger = new WriteLine { Text = "T1" },
						Condition = new ConditionBookmarker ("bk1", "Con1"),
						Action = new WriteLine { Text = "T1Action" },
						To = finalState
					},
				},
			};
			var sm = new StateMachine { States = { initialState, finalState }, InitialState = initialState };
			var app = new WFAppWrapper (sm);
			app.Run ();
			Assert.AreEqual (String.Format ("T1{0}T2{0}Con1{0}", Environment.NewLine), app.ConsoleOut);
			app.ResumeBookmark ("bk1", "bk1");
			Assert.AreEqual (String.Format ("T1{0}T2{0}Con1{0}bk1{0}T1Action{0}FEntry{0}", Environment.NewLine), app.ConsoleOut);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_MultipleTransitions_SharedTrigger_OnlyOneHasConditionEx ()
		{
			var trigger = new WriteLine ();
			var finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition { 
						Trigger = trigger,
						To = finalState, 
						Condition = new Literal<bool> (true) },
					new Transition { 
						Trigger = trigger,
						To = finalState },
				},
			};
			RunInSMAndCompare (initialState, finalState, null);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_MultipleTransitions_SharedTrigger_NoConditionsEx ()
		{
			var trigger = new WriteLine ();
			var finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition { 
						Trigger = trigger,
						To = finalState, },
					new Transition { 
						Trigger = trigger,
							To = finalState,
						}
				},
			};
			RunInSMAndCompare (initialState, finalState, null);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_MultipleTransitions_SharedActionEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'': The activity 'WriteLine' cannot be referenced by activity '' because the latter is not in another activity's implementation.  
			//An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  Activity 'WriteLine' is declared by activity ''.
			var action = new WriteLine ();
			var finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition { 
						Trigger = new WriteLine (),
						Action = action,
						To = finalState, },
					new Transition { 
						Trigger = new WriteLine (),
						Action = action,
						To = finalState,
					}
				},
			};
			RunInSMAndCompare (initialState, finalState, null);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_MultipleTransitions_SharedConditionEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'': The activity 'Literal<Boolean>' cannot be referenced by activity '' because the latter is not in another activity's implementation. 
			//An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  
			//Activity 'Literal<Boolean>' is declared by activity ''.
			var condition = new Literal<bool> (true);
			var finalState = new State ();
			finalState.IsFinal = true;
			var initialState = new State {
				Transitions =  {
					new Transition { 
						Trigger = new WriteLine (),
						Condition = condition,
						To = finalState, },
					new Transition { 
						Trigger = new WriteLine (),
						Condition = condition,
						To = finalState,
					}
				},
			};
			RunInSMAndCompare (initialState, finalState, null);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_MultipleTransitions_SharedTrigger_AcrossStatesEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'': The activity 'WriteLine' cannot be referenced by activity '' because the latter is not in another activity's implementation.  
			//An activity can only be referenced by the implementation of an activity which specifies that activity as a child or import.  
			//Activity 'WriteLine' is declared by activity ''.

			var trigger = new WriteLine { Text = "trigger" };
			var finalState = new State ();
			finalState.Entry = new WriteLine { Text = "FEntry" };
			finalState.IsFinal = true;
			var state2 = new State {
				Transitions =  {
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T2Con", false),
						To = finalState
					},
				},
			};
			var initialState = new State {
				Transitions =  {
					new Transition {
						Trigger = trigger,
						Condition = GetConditionWriter ("T2Con", false),
						To = state2
					},
				},
			};

			var sm = new StateMachine { InitialState = initialState, 
				States = { initialState, state2, finalState } };

			RunAndCompare (sm, String.Format ("trigger{0}T2Con{0}T1Con{0}T1Action{0}FEntry{0}", Environment.NewLine));
		}

		[Test]
		public void ToStringTest ()
		{
			var t = new Transition ();
			Assert.AreEqual (t.GetType().FullName, t.ToString ());
			t.DisplayName = "Bob";
			Assert.AreEqual (t.GetType().FullName, t.ToString ());
		}
	}
}

