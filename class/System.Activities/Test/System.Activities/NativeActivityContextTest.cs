using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Activities.Statements;
using System.IO;
using System.ComponentModel;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class NativeActivityContextTest : ActivityDelegateTestHelper { //FIXME: could be doing with multiple inheritance
		// cant instantiate NativeActivityContext, has internal ctor	
		void Run (Action<NativeActivityMetadata> metadata, Action<NativeActivityContext> execute)
		{
			var testBed = new NativeActivityRunner (metadata, execute);
			WorkflowInvoker.Invoke (testBed);
		}
		[Test]
		[Ignore ("GetChildren")]
		public void GetChildren ()
		{
			var writeLine1 = new WriteLine ();
			var writeLine2 = new WriteLine ();

			var wf = new NativeActWithCBRunner ((metadata) => {
				metadata.AddChild (writeLine1);
				metadata.AddImplementationChild (writeLine2);
			}, (context, callback) => {
				var children = context.GetChildren ();
				Assert.IsNotNull (children);
				Assert.AreEqual (0, children.Count);
				context.ScheduleActivity (writeLine2);
				context.ScheduleActivity (writeLine1, callback);
				children = context.GetChildren ();
				Assert.AreEqual (2, children.Count);
				Assert.AreSame (writeLine2, children [0].Activity);
				Assert.AreSame (writeLine1, children [1].Activity);
			}, (context, completedInstance, callback) => {
				var children = context.GetChildren ();
				Assert.AreEqual (1, children.Count);
				Assert.AreSame (writeLine2, children [0].Activity);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void SetValue_Variable_GetValue_Variable ()
		{
			var vStr = new Variable<string> ();

			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				Assert.IsNull (context.GetValue ((Variable) vStr));
				context.SetValue ((Variable) vStr, "newVal");
				Assert.AreEqual ("newVal", context.GetValue ((Variable) vStr));
			}));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void SetValue_Variable_NullEx ()
		{
			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.SetValue ((Variable) null, "newVal");
			}));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValue_Variable_NullEx ()
		{
			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.GetValue ((Variable) null);
			}));
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValue_Variable_NotDeclaredEx ()
		{
			//Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();

			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.SetValue ((Variable)vStr, "newVal");
			}));
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValue_Variable_NotDeclaredEx ()
		{
			// Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();

			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.GetValue ((Variable) vStr);
			}));
		}
		[Test]
		public void SetValueT_VariableT_GetValueT_VariableT ()
		{
			var vStr = new Variable<string> ();

			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				Assert.AreEqual (null, context.GetValue<string> (vStr));
				context.SetValue<string> (vStr, "newVal");
				Assert.AreEqual ("newVal", context.GetValue<string> (vStr));
			}));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void SetValueT_VariableT_NullEx ()
		{
			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.SetValue<string> ((Variable<string>) null, "newVal");
			}));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValueT_VariableT_NullEx ()
		{
			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.GetValue<string> ((Variable<string>) null);
			}));
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValueT_VariableT_NotDeclaredEx ()
		{
			//Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();

			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.SetValue<string> (vStr, "newVal");
			}));
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValueT_VariableT_NotDeclaredEx ()
		{
			//Variable '' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
			var vStr = new Variable<string> ();

			WorkflowInvoker.Invoke (new NativeActivityRunner (null, (context) => {
				context.GetValue<string> (vStr);
			}));
		}
		[Test,ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleDelegate_DelegateNullEx ()
		{
			var param = new Dictionary<string, object> ();
			var wf = new NativeActivityRunner (null, (context) => {
				context.ScheduleDelegate (null, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("Exception")]
		public void ScheduleDelegate_NotDeclaredEx ()
		{
			//System.ArgumentException : The provided activity was not part of this workflow definition when its metadata was being processed.  
			//The problematic activity named 'WriteLine' was provided by the activity named 'NativeRunnerMock'.
			var param = new Dictionary<string, object> ();
			var action = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> ("Hello\nWorld") }
			};
			var wf = new NativeActivityRunner (null, (context) => {
				context.ScheduleDelegate (action, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ScheduleDelegate_TooManyParamsEx ()
		{
			//System.ArgumentException : The supplied input parameter count 1 does not match the expected count of 0.
			var param = new Dictionary<string, object> {{"name", "value"}};
			var action = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> ("Hello\nWorld") }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ScheduleDelegate_SupplyParamForOutArgEx ()
		{
			//System.ArgumentException : The supplied input parameter count 1 does not match the expected count of 0.
			var param = new Dictionary<string, object> {{"Result", "value"}};
			var func = new ActivityFunc<String> {
				Handler = new Concat { String1 = "Hello\nWorld" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleDelegate (func, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ScheduleDelegate_TooFewParamsEx ()
		{
			//System.ArgumentException : The supplied input parameter count 1 does not match the expected count of 2.
			var param = new Dictionary<string, object> {{"Argument1", "value"}};
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var action = new ActivityAction<string,string> {
				Argument1 = delArg1,
				Argument2 = delArg2,
				Handler = new WriteLine { Text = new InArgument<string> (delArg1) }
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ScheduleDelegate_NullParamsWhenNeededEx ()
		{
			//System.ArgumentException : The supplied input parameter count 0 does not match the expected count of 2.
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var action = new ActivityAction<string,string> {
				Argument1 = delArg1,
				Argument2 = delArg2,
				Handler = new WriteLine { Text = new InArgument<string> (delArg1) }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, null);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ScheduleDelegate_NullParamsWhenNotNeeded ()
		{
			var action = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> ("Hello\nWorld") }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, null);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ScheduleDelegate_WrongTypeParamEx ()
		{
			//System.ArgumentException : Expected an input parameter value of type 'System.String' for parameter named 'Argument'.
			//Parameter name: inputParameters
			var param = new Dictionary<string, object> {{"Argument", 10}};
			var delArg1 = new DelegateInArgument<string> ();
			var action = new ActivityAction<string> {
				Argument = delArg1,
				Handler = new WriteLine { Text = new InArgument<string> (delArg1) }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ScheduleDelegate_DifferentlyNamedParamsEx ()
		{
			//System.ArgumentException : Expected input parameter named 'Argument2' was not found.
			var param = new Dictionary<string, object> {{"Argument1", "value"},
									{"wrongName","value"}};
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var action = new ActivityAction<string,string> {
				Argument1 = delArg1,
				Argument2 = delArg2,
				Handler = new WriteLine { Text = new InArgument<string> (delArg1) }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void ScheduleDelegate_DifferentlyCasedParamsEx ()
		{
			//System.ArgumentException : Expected input parameter named 'Argument2' was not found.
			var param = new Dictionary<string, object> {{"Argument1", "value"},
									{"argument2","value"}};
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var action = new ActivityAction<string,string> {
				Argument1 = delArg1,
				Argument2 = delArg2,
				Handler = new WriteLine { Text = new InArgument<string> (delArg1) }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, param);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ScheduleDelegate_0Params ()
		{
			var param = new Dictionary<string, object> ();
			var action = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> ("Hello\nWorld") }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, param);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void ScheduleDelegate_3Params ()
		{
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var delArg3 = new DelegateInArgument<string> ();
			var action = new ActivityAction<string, string, string> {
				Argument1 = delArg1,
				Argument2 = delArg2,
				Argument3 = delArg3,
				Handler = new Sequence {
					Activities = {
						new WriteLine {
							Text = new InArgument<string> (delArg1)
						},
						new WriteLine {
							Text = new InArgument<string> (delArg2)
						},
						new WriteLine {
							Text = new InArgument<string> (delArg3)
						},
					}
				}
			};
			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				var param = new Dictionary<string, object> { {"Argument1", "Arg1"},
					{"Argument2", "Arg2"},
					{"Argument3", "Arg3"}};
				context.ScheduleDelegate (action, param);
			});
			RunAndCompare (wf, String.Format ("Arg1{0}Arg2{0}Arg3{0}", Environment.NewLine));
		}
		[Test]
		public void ScheduleDelegate_CompletionCallback_FaultCallback ()
		{
			//setting Result to DelOutArg explicitly
			var outArg = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = outArg,
				Handler = new HelloWorldEx { Result = outArg }
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleDelegate (func, null, (ctx, compAI, outArgs) => {
					Console.WriteLine ("CompCB State:" + compAI.State);
					Console.WriteLine (outArgs ["Result"]);
				},(ctx, ex, propAI) => {
					Console.WriteLine ("FaultCB State:" + propAI.State);
					ctx.HandleFault ();
				});
			});
			//note helloWorldEx sets Result arg before it exceptions
			RunAndCompare (wf, String.Format ("FaultCB State:Faulted{0}CompCB State:Faulted{0}Hello\nWorld{0}", Environment.NewLine));
		}
		[Test]
		[Ignore ("Closed variable callback delegate validation")]
		public void ScheduleDelegate_AnonymousMethods_ClosedVarsNotOK_QuickTest ()
		{
			var func = new ActivityFunc<string> {
				Handler = new Concat ()
			};
			Exception ex1 = null, ex2 = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				int i = 0;
				try {
					context.ScheduleDelegate (func, null, (ctx, compAI, outArgs) => {
						var p = i;
					});
				} catch (Exception exception) {
					ex1 = exception;
				}
				try {
					context.ScheduleDelegate (func, null, null, (ctx, exception, compAI) => {
						var q = i;
					});
				} catch (Exception exception) {
					ex2 = exception;
				}
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (ArgumentException), ex1);
			Assert.IsInstanceOfType (typeof (ArgumentException), ex2);
		}
		[Test]
		[Ignore ("Closed variable callback delegate validation")]
		public void ScheduleFunc_AnonymousMethods_ClosedVarsNotOK_QuickTest ()
		{
			var func = new ActivityFunc<string> {
				Handler = new Concat ()
			};
			Exception ex1 = null, ex2 = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				int i = 0;
				try {
					context.ScheduleFunc (func, (ctx, compAI, value) => {
						var p = i;
					});
				} catch (Exception exception) {
					ex1 = exception;
				}
				try {
					context.ScheduleFunc (func, null, (ctx, exception, compAI) => {
						var q = i;
					});
				} catch (Exception exception) {
					ex2 = exception;
				}
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (ArgumentException), ex1);
			Assert.IsInstanceOfType (typeof (ArgumentException), ex2);
		}
		[Test]
		[Ignore ("Closed variable callback delegate validation")]
		public void ScheduleAction_AnonymousMethods_ClosedVarsNotOK_QuickTest ()
		{
			var action = new ActivityAction {
				Handler = new WriteLine ()
			};
			Exception ex1 = null, ex2 = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				int i = 0;
				try {
					context.ScheduleAction (action, (ctx, compAI) => {
						var p = i;
					});
				} catch (Exception exception) {
					ex1 = exception;
				}
				try {
					context.ScheduleAction (action, null, (ctx, exception, compAI) => {
						var q = i;
					});
				} catch (Exception exception) {
					ex2 = exception;
				}
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsInstanceOfType (typeof (ArgumentException), ex1);
			Assert.IsInstanceOfType (typeof (ArgumentException), ex2);
		}
		[Test]
		public void ScheduleFunc_CompletionCallbackT_FaultCallback ()
		{
			//not setting Result to DelOutArg explicitly
			var func = new ActivityFunc<string> {
				Handler = new HelloWorldEx ()
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, (ctx, compAI, result) => {
					Console.WriteLine ("CompCB State:" + compAI.State);
					Console.WriteLine (result);
				},(ctx, ex, propAI) => {
					Console.WriteLine ("FaultCB State:" + propAI.State);
					ctx.HandleFault ();
				});
			});
			//note helloWorldEx sets Result arg before it exceptions
			RunAndCompare (wf, String.Format ("FaultCB State:Faulted{0}CompCB State:Faulted{0}Hello\nWorld{0}", Environment.NewLine));
			Assert.IsNull (func.Result);
			Assert.IsNotNull (((CodeActivity<string>)func.Handler).Result);
		}
		// FIXME: test CompletionCallback and FaultCallback validation checks for ScheduleActivity overloads?
		[Test]
		public void ScheduleDelegate_NonStringClassTypeForParam ()
		{
			var tw = new StringWriter ();
			var param = new Dictionary<string, object> {{"Argument", tw}};
			var delArg = new DelegateInArgument<TextWriter> ();
			var action = new ActivityAction<TextWriter> {
				Argument = delArg,
				Handler = new WriteLine { 
					Text = new InArgument<string> ("Hello\nWorld"),
					TextWriter = new InArgument<TextWriter> (delArg)
				}
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleDelegate (action, param);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, tw.ToString ());
		}
		CompletionCallback<string> compCBTStringConsoleWriter = (context, compAI, value) => {
			Console.WriteLine (value);
		};
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleFunc_TResult_FuncNullEx ()
		{
			var func = GetActivityFuncConcatMany0 ();
			ExecuteActionInWFWithDelegateAndCompare (func, (context) => {
				context.ScheduleFunc (null, compCBTStringConsoleWriter);
			}, 0);
		}
		[Test]
		public void ScheduleFunc_TResult ()
		{
			var func = GetActivityFuncConcatMany0 ();
			ExecuteActionInWFWithDelegateAndCompare (func, (context) => {
				context.ScheduleFunc (func, compCBTStringConsoleWriter);
			}, 0);
		}
		[Test]
		public void ScheduleFuncT_TResult ()
		{
			var func = GetActivityFuncConcatMany1 ();
			ExecuteActionInWFWithDelegateAndCompare (func, (context) => {
				context.ScheduleFunc (func, "1", compCBTStringConsoleWriter);
			}, 1);
		}
		[Test]
		public void ScheduleFuncT1T2_TResult ()
		{
			var func = GetActivityFuncConcatMany2 ();
			ExecuteActionInWFWithDelegateAndCompare (func, (context) => {
				context.ScheduleFunc (func, "1", "2", compCBTStringConsoleWriter);
			}, 2);
		}
		[Test]
		public void ScheduleFuncT1T2T3_TResult ()
		{
			var func = GetActivityFuncConcatMany3 ();
			ExecuteActionInWFWithDelegateAndCompare (func, (context) => {
				context.ScheduleFunc (func, "1", "2", "3", compCBTStringConsoleWriter);
			}, 3);
		}
		//FIXME: 16 overloads of ScheduleFunc
		//FIXME: havnt tested fault callback of ScheduleFunc overloads
		void ExecuteActionInWFWithDelegateAndCompare (ActivityDelegate del, Action<NativeActivityContext> contextAction, int noParams)
		{
			var wf = new NativeActivityRunner (metadata =>  {
				metadata.AddDelegate (del);
			}, context =>  {
				contextAction (context);
			});
			var expected = ExpectedConcatManyConsoleOutput (noParams);
			RunAndCompare (wf, expected);
		}
		[Test]
		public void ScheduleAction_CompletionCallback_FaultCallback ()
		{
			var action = new ActivityAction {
				Handler = new HelloWorldEx ()
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleAction (action, (ctx, compAI) => {
					Console.WriteLine ("CompCB State:" + compAI.State);
				}, (ctx, ex, propAI) => {
					Console.WriteLine ("FaultCB State:" + propAI.State);
					ctx.HandleFault ();
				});
			});
			RunAndCompare (wf, String.Format ("FaultCB State:Faulted{0}CompCB State:Faulted{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleAction_NullActionEx ()
		{
			var action = GetActivityActionConcatMany0 ();
			ExecuteActionInWFWithDelegateAndCompare (action, (context) => {
				context.ScheduleAction (null);
			}, 0);
		}
		[Test]
		public void ScheduleAction ()
		{
			var action = GetActivityActionConcatMany0 ();
			ExecuteActionInWFWithDelegateAndCompare (action, (context) => {
				context.ScheduleAction (action);
			}, 0);
		}
		[Test]
		public void ScheduleActionT ()
		{
			var action = GetActivityActionConcatMany1 ();
			ExecuteActionInWFWithDelegateAndCompare (action, (context) => {
				context.ScheduleAction (action, "1");
			}, 1);
		}
		[Test]
		public void ScheduleActionT1T2 ()
		{
			var action = GetActivityActionConcatMany2 ();
			ExecuteActionInWFWithDelegateAndCompare (action, (context) => {
				context.ScheduleAction (action, "1", "2");
			}, 2);
		}
		[Test]
		public void ScheduleActionT1T2T3 ()
		{
			var action = GetActivityActionConcatMany3 ();
			ExecuteActionInWFWithDelegateAndCompare (action, (context) => {
				context.ScheduleAction (action, "1", "2", "3");
			}, 3);
		}
		//FIXME: 16 overloads of ScheduleAction
		//FIXME: havnt tested completion callback or fault callback of ScheduleAction overloads
		[Test]
		public void ScheduleActivity_Activity ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleActivity_Activity_NullEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.ScheduleActivity (null);
			});
		}
		[Test]
		public void ScheduleActivity_Activity_CompletionCallback ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine, (ctx, ai)=> { Console.WriteLine ("compCbRan"); });
			});
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}compCbRan{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleActivity_Activity_CompletionCallback_NullActEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.ScheduleActivity (null, (ctx, ai)=> {  });
			});
		}
		[Test]
		public void ScheduleActivity_Activity_CompletionCallback_NullCBOk ()
		{
			var write = new WriteLine { Text = "Hello\nWorld" };
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (write);
			}, (context) => {
				context.ScheduleActivity (write, (CompletionCallback) null);
			});
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleActivityT_ActivityT_CompletionCallback_FaultCallback_NullActivityEx ()
		{
			//CompletionCallback and FaultCallback are optional params anyway
			ExecuteStatementAndThrow ((context) => {
				context.ScheduleActivity ((Activity<string>) null);
			});
		}
		[Test]
		public void ScheduleActivityT_ActivityT_CompletionCallback_FaultCallback ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (
					helloWorldEx, (ctx, ai, value)=> { 
						Console.WriteLine ("compCbRan"); 
						Console.WriteLine (value); },
					(ctx, ex, ai)=> { 
						Console.WriteLine ("faultCbRan"); 
						ctx.HandleFault ();
					});
			});
			//note helloWorldEx sets Result arg before it exceptions
			RunAndCompare (wf, String.Format ("faultCbRan{0}compCbRan{0}Hello\nWorld{0}", Environment.NewLine));
		}
		[Test]
		public void ScheduleActivity_Activity_FaultCallback ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (
					helloWorldEx, (ctx, ex, ai)=> { 
					Console.WriteLine ("faultCbRan"); 
					ctx.HandleFault ();
				});
			});
			RunAndCompare (wf, String.Format ("faultCbRan{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleActivity_Activity_FaultCallback_NullActivityEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.ScheduleActivity (null, (ctx, ex, ai)=> { });
			});
		}
		[Test]
		public void ScheduleActivity_Activity_FaultCallback_NullFaultCallbackOK ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (
					helloWorldEx, (FaultCallback) null);
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.UnhandledException);
			Assert.AreSame (helloWorldEx.IThrow, app.UnhandledException);
		}
		[Test]
		public void ScheduleActivity_Activity_CompletionCallback_FaultCallback ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity (
					helloWorldEx, (ctx, ai)=> { Console.WriteLine ("compCbRan"); },
				(ctx, ex, ai)=> { 
					Console.WriteLine ("faultCbRan"); 
					ctx.HandleFault ();
				});
			});
			RunAndCompare (wf, String.Format ("faultCbRan{0}compCbRan{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleActivity_Activity_CompletionCallback_FaultCallback_NullActivityEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.ScheduleActivity (null, (ctx, ai)=> { 
					Console.WriteLine ("compCbRan"); 
				}, (ctx, ex, ai)=> { 
					Console.WriteLine ("faultCbRan"); 
					ctx.HandleFault ();
				});});
		}
		[Test]
		public void ScheduleActivity_Activity_CompletionCallback_FaultCallback_BothCallbacksNullOk ()
		{
			var helloWorldEx = new HelloWorldEx ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (helloWorldEx);
			}, (context) => {
				context.ScheduleActivity ((Activity)helloWorldEx, null, null);
			});
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.UnhandledException);
			Assert.AreSame (helloWorldEx.IThrow, app.UnhandledException);
		}
		static BookmarkCallback writeValueBKCB = (context, bookmark, value) => {
			Console.WriteLine ((string) value);
		};
		[Test]
		public void CreateBookmark ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ();
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (String.Empty, bookmark.Name);
			Assert.AreEqual (0, app.GetBookmarks ().Count); // cant get BookmarkInfo as no name
			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus not BookmarkOptions.NonBlocking
			app.ResumeBookmark (bookmark, null); // wouldnt resume if scope set
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status); // thus not BookmarkOptions.MultiResume
		}
		[Test]
		public void CreateBookmark_Name ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ("b1");
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual ("b1", bookmark.Name);

			var bmi = app.GetBookmarks ().Single (); //can get BookmarkInfo as has name
			Assert.AreEqual (wf.DisplayName, bmi.OwnerDisplayName);

			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus not BookmarkOptions.NonBlocking
			app.ResumeBookmark ("b1", null); // wouldnt resume if scope set
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status); 
			// thus not BookmarkOptions.MultiResume + name correct
		}
		[Test]
		public void CreateBookmark_Callback ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark (writeValueBKCB);
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (0, app.GetBookmarks ().Count); // still cant get BookmarkInfo as no name
			Assert.AreEqual (String.Empty, bookmark.Name);
			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus not BookmarkOptions.NonBlocking
			app.ResumeBookmark (bookmark, "resumed"); // wouldnt resume if scope set
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status); // thus not BookmarkOptions.MultiResume
			Assert.AreEqual ("resumed" + Environment.NewLine, app.ConsoleOut); // thus callback ran
		}
		[Test]
		public void CreateBookmark_Callback_Options ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark (writeValueBKCB, BookmarkOptions.MultipleResume);
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (0, app.GetBookmarks ().Count); // still cant get BookmarkInfo as no name
			Assert.AreEqual (String.Empty, bookmark.Name);
			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus not BookmarkOptions.NonBlocking
			app.ResumeBookmark (bookmark, "resumed"); // wouldnt resume if scope set
			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus BookmarkOptions.MultiResume
			Assert.AreEqual ("resumed" + Environment.NewLine, app.ConsoleOut); // thus callback ran
		}
		[Test]
		public void CreateBookmark_Name_Callback ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBKCB);
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual ("b1", bookmark.Name);

			var bmi = app.GetBookmarks ().Single (); //can get BookmarkInfo as has name
			Assert.AreEqual (wf.DisplayName, bmi.OwnerDisplayName);

			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus not BookmarkOptions.NonBlocking
			app.ResumeBookmark ("b1", "resumed"); // wouldnt resume if scope set
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status); // thus not BookmarkOptions.MultiResume
			Assert.AreEqual ("resumed" + Environment.NewLine, app.ConsoleOut); // thus callback ran
		}
		[Test]
		public void CreateBookmark_Name_Callback_Options ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ("b1", writeValueBKCB, BookmarkOptions.MultipleResume);
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual ("b1", bookmark.Name);

			var bmi = app.GetBookmarks ().Single (); //can get BookmarkInfo as has name
			Assert.AreSame (wf.DisplayName, bmi.OwnerDisplayName);

			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus not BookmarkOptions.NonBlocking
			app.ResumeBookmark ("b1", "resumed"); // wouldnt resume if scope set
			Assert.AreEqual (WFAppStatus.Idle, app.Status); // thus BookmarkOptions.MultiResume
			Assert.AreEqual ("resumed" + Environment.NewLine, app.ConsoleOut); // thus callback ran
		}
		[Test]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_Name_Callback_Scope ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_Name_Callback_Scope_Options ()
		{
			throw new NotImplementedException ();
		}
		static void ExecuteStatementAndThrow (Action<NativeActivityContext> action)
		{
			Exception ex = null;
			var wf = new NativeActivityRunner (null, (context) => {
				try {
					action (context);
				} catch (Exception ex2) {
					ex = ex2;
				}
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			if (ex != null) {
				throw ex;
			} else {
				if (app.Status != WFAppStatus.CompletedSuccessfully)
					throw new Exception ("something unexpected went wrong in the workflow");
			}
		}
		static void ExecuteCreateBookmarkAndThrow (String name, BookmarkCallback bookmarkCallback)
		{
			ExecuteStatementAndThrow ((context) => {
				context.CreateBookmark (name, bookmarkCallback);
			});
		}
		static void ExecuteCreateBookmarkAndThrow (String name, BookmarkCallback bookmarkCallback, 
						 BookmarkOptions bookmarkOptions)
		{
			ExecuteStatementAndThrow ((context) => {
				context.CreateBookmark (name, bookmarkCallback, bookmarkOptions);
			});
		}
		static void ExecuteCreateBookmarkAndThrow (string name, BookmarkCallback bookmarkCallback, 
						 BookmarkScope bookmarkscope)
		{
			ExecuteStatementAndThrow ((context) => {
				context.CreateBookmark (name, bookmarkCallback, bookmarkscope);
			});
		}
		static void ExecuteCreateBookmarkAndThrow (string name, BookmarkCallback bookmarkCallback, 
						 BookmarkScope bookmarkScope, BookmarkOptions bookmarkOptions)
		{
			ExecuteStatementAndThrow ((context) => {
				context.CreateBookmark (name, bookmarkCallback, bookmarkScope, bookmarkOptions);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void CreateBookmark_Name_NameEmptyEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.CreateBookmark (String.Empty);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void CreateBookmark_Name_NameNullEx ()
		{
			ExecuteStatementAndThrow ((context) => {
				context.CreateBookmark ((string) null);
			});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void CreateBookmark_Name_Callback_NameEmptyEx ()
		{
			ExecuteCreateBookmarkAndThrow (String.Empty, writeValueBKCB);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void CreateBookmark_Name_Callback_NameNullEx ()
		{
			ExecuteCreateBookmarkAndThrow ((string) null, writeValueBKCB);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void CreateBookmark_Name_Callback_CallbackNullEx ()
		{
			ExecuteCreateBookmarkAndThrow ("name", null);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void CreateBookmark_Name_Callback_Options_NameEmptyEx ()
		{
			ExecuteCreateBookmarkAndThrow (String.Empty, writeValueBKCB, BookmarkOptions.None);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void CreateBookmark_Name_Callback_Options_NameNullEx ()
		{
			ExecuteCreateBookmarkAndThrow (null, writeValueBKCB, BookmarkOptions.None);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void CreateBookmark_Name_Callback_Options_CallbackNullEx ()
		{
			ExecuteCreateBookmarkAndThrow ("name", null, BookmarkOptions.None);
		}
		[Test, ExpectedException (typeof (InvalidEnumArgumentException))]
		public void CreateBookmark_Name_Callback_Options_OptionsInvalidEx ()
		{
			ExecuteCreateBookmarkAndThrow ("name", writeValueBKCB, (BookmarkOptions)(-1));
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_Name_Callback_Scope_NameEmptyEx ()
		{
			ExecuteCreateBookmarkAndThrow (String.Empty, writeValueBKCB, BookmarkScope.Default);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_Name_Callback_Scope_NameNullEx ()
		{
			ExecuteCreateBookmarkAndThrow (null, writeValueBKCB, BookmarkScope.Default);
		}
		[Test, ExpectedException (typeof (NullReferenceException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_Name_Callback_Scope_CallbackNullEx ()
		{
			ExecuteCreateBookmarkAndThrow ("name", null, BookmarkScope.Default);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_Name_Callback_Scope_ScopeNullEx ()
		{
			ExecuteCreateBookmarkAndThrow ("name", writeValueBKCB, null);
		}
		[Test]
		public void CreateBookmark_Callback_CallbackNullOK ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ((BookmarkCallback) null);
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark (bookmark, null);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
		}
		[Test]
		public void CreateBookmark_Callback_Options_CallbackNullOK ()
		{
			Bookmark bookmark = null;
			var wf = new NativeActivityRunner (null, (context) => {
				bookmark = context.CreateBookmark ((BookmarkCallback) null, BookmarkOptions.None);
			});
			wf.InduceIdle = true;
			var app = new WFAppWrapper (wf);
			app.Run ();
			Assert.AreEqual (WFAppStatus.Idle, app.Status);
			app.ResumeBookmark (bookmark, null);
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
		}
		[Test, ExpectedException (typeof (InvalidEnumArgumentException))]
		public void CreateBookmark_Callback_Options_OptionsInvalidEx ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark (writeValueBKCB, (BookmarkOptions)(-1));
			});
			wf.InduceIdle = true;
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_name_Callback_Scope_Options_NameNullEx ()
		{
			//System.ArgumentException : The argument name is null or empty.
			//Parameter name: name
			ExecuteCreateBookmarkAndThrow (null, writeValueBKCB, BookmarkScope.Default, BookmarkOptions.None);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_name_Callback_Scope_Options_NameEmptyEx ()
		{
			//System.ArgumentException : The argument name is null or empty.
			//Parameter name: name
			ExecuteCreateBookmarkAndThrow (String.Empty, writeValueBKCB, BookmarkScope.Default, BookmarkOptions.None);
		}
		[Test, ExpectedException (typeof (NullReferenceException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_name_Callback_Scope_Options_CallbackNullEx ()
		{
			ExecuteCreateBookmarkAndThrow ("name", null, BookmarkScope.Default, BookmarkOptions.None);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_name_Callback_Scope_Options_ScopeNullEx ()
		{
			ExecuteCreateBookmarkAndThrow ("name", writeValueBKCB, null, BookmarkOptions.None);
		}
		[Test, ExpectedException (typeof (InvalidEnumArgumentException))]
		[Ignore ("BookmarkScope")]
		public void CreateBookmark_name_Callback_Scope_Options_OptionsInvalidEx ()
		{
			//System.ComponentModel.InvalidEnumArgumentException : The value of argument 'options' (-1) is invalid for Enum type 'BookmarkOptions'.
			//Parameter name: options
			ExecuteCreateBookmarkAndThrow ("name", writeValueBKCB, BookmarkScope.Default, (BookmarkOptions)(-1));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ResumeBookmark_Bookmark_Value_NullBookmarkEx ()
		{
			ExecuteStatementAndThrow ((context) => context.ResumeBookmark (null, ""));
		}
		[Test]
		public void ResumeBookmark_Bookmark_Value ()
		{
			var wf = new NativeActivityRunner (null, (context) => {
				var bm = context.CreateBookmark (writeValueBKCB);
				context.ResumeBookmark (bm, "resumed");
			});
			wf.InduceIdle = true;
			RunAndCompare (wf, "resumed" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void RemoveBookmark_Bookmark_NullEx ()
		{
			ExecuteStatementAndThrow  ((context) => context.RemoveBookmark ((Bookmark) null));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void RemoveBookmark_Name_NullEx ()
		{
			ExecuteStatementAndThrow  ((context) => context.RemoveBookmark ((String) null));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void RemoveBookmark_Name_EmptyEx ()
		{
			//oddly this throws an ArgumentNullException exception
			ExecuteStatementAndThrow  ((context) => context.RemoveBookmark (String.Empty));
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		[Ignore ("BookmarkScope")]
		public void RemoveBookmark_Name_Scope_ScopeNullEx ()
		{
			ExecuteStatementAndThrow  ((context) => context.RemoveBookmark ("name", null));
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("BookmarkScope")]
		public void RemoveBookmark_Name_Scope_NameNullEx ()
		{
			//System.ArgumentException : The argument name is null or empty.
			ExecuteStatementAndThrow  ((context) => context.RemoveBookmark (null, BookmarkScope.Default));
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("BookmarkScope")]
		public void RemoveBookmark_Name_Scope_NameEmptyEx ()
		{
			//System.ArgumentException : The argument name is null or empty.
			ExecuteStatementAndThrow  ((context) => context.RemoveBookmark (String.Empty, BookmarkScope.Default));
		}
		[Test]
		public void RemoveBookmark_Bookmark ()
		{
			bool result = false, dummy = false;
			var wf = new NativeActivityRunner (null, (context) => {
				var bm = context.CreateBookmark ("b1");
				result = context.RemoveBookmark (bm);
				dummy = context.RemoveBookmark (new Bookmark ("sdas"));
			});
			wf.InduceIdle = true;
			WorkflowInvoker.Invoke (wf);
			Assert.IsTrue (result);
			Assert.IsFalse (dummy);
		}
		[Test]
		public void RemoveBookmark_Name ()
		{
			bool result = false, dummy = false;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1");
				result = context.RemoveBookmark ("b1");
				dummy = context.RemoveBookmark ("sdas");
			});
			wf.InduceIdle = true;
			WorkflowInvoker.Invoke (wf);
			Assert.IsTrue (result);
			Assert.IsFalse (dummy);
		}
		[Test]
		[Ignore ("BookmarkScope")]
		public void RemoveBookmark_Name_Scope ()
		{
			bool result = false, dummy = false;
			var wf = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ("b1", writeValueBKCB, BookmarkScope.Default);
				result = context.RemoveBookmark ("b1", BookmarkScope.Default);
				dummy = context.RemoveBookmark ("sdas", BookmarkScope.Default);
			});
			wf.InduceIdle = true;
			WorkflowInvoker.Invoke (wf);
			Assert.IsTrue (result);
			Assert.IsFalse (dummy);
		}
		[Test]
		public void RemoveAllBookmarks ()
		{
			var child = new NativeActivityRunner (null, (context) => {
				context.CreateBookmark ();
				context.CreateBookmark ();
				context.CreateBookmark ("childBM");
				context.RemoveAllBookmarks ();
			});
			child.InduceIdle = true;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.CreateBookmark ("b1", writeValueBKCB);
				context.ScheduleActivity (child);
			});
			wf.InduceIdle = true;
			var app = GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			app.ResumeBookmark ("b1", "resumed");
			Assert.AreEqual (WFAppStatus.CompletedSuccessfully, app.Status);
			Assert.AreEqual ("resumed" + Environment.NewLine, app.ConsoleOut);
		}
	}
	class NativeActivityContextTestSuite {
		public BookmarkScope DefaultBookmarkScope { get { throw new NotImplementedException (); } }
		public bool IsCancellationRequested { get { throw new NotImplementedException (); } }
		public ExecutionProperties Properties { get { throw new NotImplementedException (); } }

		public void Abort ()
		{
			throw new NotImplementedException ();
		}
		public void AbortChildInstance ()
		{
			throw new NotImplementedException ();
		}
		public void CancelChild ()
		{
			throw new NotImplementedException ();
		}
		public void CancelChildren ()
		{
			throw new NotImplementedException ();
		}

		public void GetChildren ()
		{
			throw new NotImplementedException ();
		}
		public void MarkCanceled ()
		{
			throw new NotImplementedException ();
		}
		
		// lots more ScheduleAction overrides

		public void ScheduleActivity ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivity_CompletionCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivity_FaultCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivity_CompletionCallback_FaultCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleActivityT_CompletionCallbackT_FaultCallback ()
		{
			throw new NotImplementedException ();
		}
		public void ScheduleFuncT_CompletionCallback_FaultCallback ()
		{
			throw new NotImplementedException ();
		}

		// lots more Schedule overrides

		public void Track ()
		{
			throw new NotImplementedException ();
		}
	}

}
