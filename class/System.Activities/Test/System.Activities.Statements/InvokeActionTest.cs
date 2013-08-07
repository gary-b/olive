using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class InvokeActionTest : ActivityDelegateTestHelper {
		InvokeAction GetInvokeAction0 (ActivityAction action)
		{
			return new InvokeAction {
				Action = action,
			};
		}
		InvokeAction<T> GetInvokeAction1<T> (ActivityAction<T> action, Variable<T> arg)
		{
			return new InvokeAction<T> {
				Action = action,
				Argument = arg,
			};
		}
		//use of variable proves delegate run publicly
		InvokeAction<T1, T2> GetInvokeAction2<T1, T2> (ActivityAction<T1, T2> action, Variable<T1> arg1, T2 arg2)
		{
			return new InvokeAction<T1, T2> {
				Action = action,
				Argument1 = arg1,
				Argument2 = arg2,
			};
		}
		InvokeAction<T1, T2, T3> GetInvokeAction3<T1, T2, T3> (
			ActivityAction<T1, T2, T3> action, Variable<T1> arg1, T2 arg2, T3 arg3)
		{
			return new InvokeAction<T1, T2, T3> {
				Action = action,
				Argument1 = arg1,
				Argument2 = arg2,
				Argument3 = arg3,
			};
		}
		Variable<string> vStr1 = new Variable<string> ("", "1");
		Variable<int> vInt1 = new Variable<int> ("", 1);
		void RunInvokeAndCompare (Activity invoke, Variable v1, String expected)
		{
			//use of variable proves delegate run publicly
			var wrapper = new Sequence {
				Variables = { v1 },
				Activities = { invoke }
			};
			RunAndCompare (wrapper, expected);
		}
		[Test]
		public void InvokeAction_RunsActionAsPublicDelegate ()
		{
			var v1 = new Variable<string> ("", "value");
			var wf = new Sequence {
				Variables = {
					v1
				},
				Activities = {
					new InvokeAction {
						Action = new ActivityAction {
							Handler = new WriteLine {
								Text = v1
							}
						}
					}
				}
			};
			RunAndCompare (wf, "value" + Environment.NewLine);
		}
		[Test]
		public void InvokeAction_ActionNullOK ()
		{
			var invoke = GetInvokeAction0 (null);
			RunAndCompare (invoke, String.Empty);
		}
		[Test]
		public void InvokeAction ()
		{
			var action = GetActivityActionConcatMany0 ();
			var invoke = GetInvokeAction0 (action);
			RunAndCompare (invoke, ExpectedConcatManyConsoleOutput (0));
		}
		[Test]
		public void InvokeActionT ()
		{
			var action = GetActivityActionConcatMany1 ();
			var invoke = GetInvokeAction1 (action, vStr1);
			RunInvokeAndCompare (invoke, vStr1, ExpectedConcatManyConsoleOutput (1));
		}
		[Test]
		public void InvokeActionT_ActionNullOK ()
		{
			var invoke = GetInvokeAction1 (null, vInt1);
			RunInvokeAndCompare (invoke, vInt1, String.Empty);
		}
		[Test]
		public void InvokeActionT1T2 ()
		{
			var action = GetActivityActionConcatMany2 ();
			var invoke = GetInvokeAction2 (action, vStr1, "2");
			RunInvokeAndCompare (invoke, vStr1, ExpectedConcatManyConsoleOutput (2));
		}
		[Test]
		public void InvokeActionT1T2_ActionNullOK ()
		{
			var invoke = GetInvokeAction2 (null, vInt1, 2);
			RunInvokeAndCompare (invoke, vInt1, String.Empty);
		}
		[Test]
		public void InvokeActionT1T2T3 ()
		{
			var action = GetActivityActionConcatMany3 ();
			var invoke = GetInvokeAction3 (action, vStr1, "2", "3");
			RunInvokeAndCompare (invoke, vStr1, ExpectedConcatManyConsoleOutput (3));
		}
		[Test]
		public void InvokeActionT1T2T3_ActionNullOK ()
		{
			var invoke = GetInvokeAction3 (null, vInt1, 2, 3);
			RunInvokeAndCompare (invoke, vInt1, String.Empty);
		}
	}
}

