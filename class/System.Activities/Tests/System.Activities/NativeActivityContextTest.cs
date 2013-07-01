using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Activities.Statements;
using System.IO;

namespace Tests.System.Activities {
	[TestFixture]
	class NativeActivityContextTest : WFTest {
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
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddChild (writeLine1);
				metadata.AddImplementationChild (writeLine2);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				var children = context.GetChildren ();
				Assert.IsNotNull (children);
				Assert.AreEqual (0, children.Count);
				context.ScheduleActivity (writeLine1);
				children = context.GetChildren ();
				Assert.AreEqual (1, children.Count);
				Assert.AreSame (writeLine1, children [0].Activity);
			};
			Run (metadataAction, executeAction);
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
		//FIXME: Note the ScheduleAction overloads are also used in ActivityActionTest
		[Test]
		public void ScheduleActionT ()
		{
			// want to allow user to supply activity to which a string will be passed
			var inArg = new DelegateInArgument<string> ();
			ActivityAction<string> action = new ActivityAction<string> {
				Argument = inArg,
				Handler = new WriteLine {
					Text = new InArgument<string> (inArg)
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleAction<string>(action, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
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
		// TODO: test onCompleted and onFaulted for ScheduleDelegate
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
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleActivity_Activity_NullEx ()
		{
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (new WriteLine ());
			}, (context) => {
				context.ScheduleActivity (null);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ScheduleActivity_Activity_CompletionCallback_NullActEx ()
		{
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (new WriteLine ());
			}, (context) => {
				context.ScheduleActivity (null, (ctx, ai)=> {  });
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ScheduleActivity_Activity_CompletionCallback_NullCBOk ()
		{
			var write = new WriteLine ();
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (write);
			}, (context) => {
				context.ScheduleActivity (write, (CompletionCallback) null);
			});
			WorkflowInvoker.Invoke (wf);
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

		// lots of Bookmark related functions

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
