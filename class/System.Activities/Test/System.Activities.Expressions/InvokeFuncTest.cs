using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace MonoTests.System.Activities.Expressions {
	[TestFixture]
	public class InvokeFuncTest : ActivityDelegateTestHelper {
		void RunInvokeConcatManyDelegateAndCompare (Variable<string> resultVar, Activity invoke, int noParams)
		{
			var wf = new Sequence {
				Variables =  {
					resultVar,
				},
				Activities =  {
					invoke,
					new WriteLine {
						Text = new InArgument<string> (resultVar),
					}
				}
			};
			var expected = ExpectedConcatManyConsoleOutput (noParams);
			RunAndCompare (wf, expected);
		}
		InvokeFunc<TResult> GetInvokeFunc0<TResult> (ActivityFunc<TResult> func)
		{
			return new InvokeFunc <TResult> {
				Func = func,
			};
		}
		InvokeFunc<T, TResult> GetInvokeFunc1<T, TResult> (ActivityFunc<T, TResult> func, T arg)
		{
			return new InvokeFunc <T, TResult> {
				Func = func,
				Argument = arg,
			};
		}
		InvokeFunc<T1, T2, TResult> GetInvokeFunc2<T1, T2, TResult> (ActivityFunc<T1, T2, TResult> func, T1 arg1, T2 arg2)
		{
			return new InvokeFunc <T1, T2, TResult> {
				Func = func,
				Argument1 = arg1,
				Argument2 = arg2,
			};
		}
		InvokeFunc<T1, T2, T3, TResult> GetInvokeFunc3<T1, T2, T3, TResult> (
			ActivityFunc<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3)
		{
			return new InvokeFunc <T1, T2, T3, TResult> {
				Func = func,
				Argument1 = arg1,
				Argument2 = arg2,
				Argument3 = arg3,
			};
		}
		void RunInvokeAndCompare<T> (Activity<T> invoke, String expected)
		{
			var vResult = new Variable<T> ();
			invoke.Result = vResult;
			var wrapper = new Sequence {
				Variables = { vResult },
				Activities = { 
					invoke,
					new WriteLine { Text = vResult } }
			};
			RunAndCompare (wrapper, expected);
		}
		[Test]
		public void InvokeFunc_TResult ()
		{
			var func = GetActivityFuncConcatMany0 ();
			var invoke = GetInvokeFunc0 (func);
			RunInvokeAndCompare (invoke, ExpectedConcatManyConsoleOutput (0));
		}
		[Test]
		public void InvokeFunc_TResult_FuncNullOK ()
		{
			var invokeStr = GetInvokeFunc0<string> (null);
			Assert.IsNull (WorkflowInvoker.Invoke (invokeStr));
			Assert.IsNull (invokeStr.Func);

			var invokeInt = GetInvokeFunc0<int> (null);
			Assert.AreEqual (0, WorkflowInvoker.Invoke (invokeInt));
		}
		[Test]
		public void InvokeFuncT_TResult ()
		{
			var func = GetActivityFuncConcatMany1 ();
			var invoke = GetInvokeFunc1 (func, "1");
			RunInvokeAndCompare (invoke, ExpectedConcatManyConsoleOutput (1));
		}
		[Test]
		public void InvokeFuncT_TResult_FuncNullOK ()
		{
			var invokeInt = GetInvokeFunc1<int, int> (null, 1);
			Assert.AreEqual (0, WorkflowInvoker.Invoke (invokeInt));
		}
		[Test]
		public void InvokeFuncT1T2_TResult ()
		{
			var func = GetActivityFuncConcatMany2 ();
			var invoke = GetInvokeFunc2 (func, "1", "2");
			RunInvokeAndCompare (invoke, ExpectedConcatManyConsoleOutput (2));
		}
		[Test]
		public void InvokeFuncT1T2_TResult_FuncNullOK ()
		{
			var invokeInt = GetInvokeFunc2<int, int, int> (null, 1, 2);
			Assert.AreEqual (0, WorkflowInvoker.Invoke (invokeInt));
		}
		[Test]
		public void InvokeFuncT1T2T3_TResult ()
		{
			var func = GetActivityFuncConcatMany3 ();
			var invoke = GetInvokeFunc3 (func, "1", "2", "3");
			RunInvokeAndCompare (invoke, ExpectedConcatManyConsoleOutput (3));
		}
		[Test]
		public void InvokeFuncT1T2T3_TResult_FuncNullOK ()
		{
			var invokeInt = GetInvokeFunc3<int, int, int, int> (null, 1, 2, 3);;
			Assert.AreEqual (0, WorkflowInvoker.Invoke (invokeInt));
		}
		// FIXME: test rest of ActivityFunc classes - up to 16 generic params
	}
}

