using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Collections.Generic;
using System.Activities.Expressions;

namespace MonoTests.System.Activities {
	//Mostly from Increment 5
	[TestFixture]
	public class ActivityDelegateHandlingRuntimeTest : WFTestHelper {
		ActivityAction GetActionVariableWriter (out Variable<string> varStr, string varValue)
		{
			varStr = new Variable<string> ("", varValue);
			return new ActivityAction {
				Handler = new WriteLine {
					Text = new InArgument<string> (varStr)
				}
			};
		}
		ActivityAction GetMetadataWriter (string name)
		{
			return new ActivityAction {
				Handler = new NativeActivityRunner ((metadata)=> { 
					Console.WriteLine (name); 
				}, null),
			};
		}
		[Test]
		public void CacheMetadata_OrderCalled_ActivityIdGeneration ()
		{
			//children executed in lifo manner
			//but implementation children executed first
			int orderCounter = 0;
			//FIXME: see notes in CacheMetadata_OrderCalled test
			var pubChild1PubChild1 = GetMetadataWriter ("pubChild1PubChild1");
			var pubChild1PubChild2 = GetMetadataWriter ("pubChild1PubChild2");
			var pubChild1ImpChild1 = GetMetadataWriter ("pubChild1ImpChild1");
			var pubChild1ImpChild2 = GetMetadataWriter ("pubChild1ImpChild2");
			var pubChild2PubChild1 = GetMetadataWriter ("pubChild2PubChild1");
			var pubChild2PubChild2 = GetMetadataWriter ("pubChild2PubChild2");
			var pubChild2ImpChild1 = GetMetadataWriter ("pubChild2ImpChild1");
			var pubChild2ImpChild2 = GetMetadataWriter ("pubChild2ImpChild2");
			var impChild1PubChild1 = GetMetadataWriter ("impChild1PubChild1");
			var impChild1PubChild2 = GetMetadataWriter ("impChild1PubChild2");
			var impChild1ImpChild1 = GetMetadataWriter ("impChild1ImpChild1");
			var impChild1ImpChild2 = GetMetadataWriter ("impChild1ImpChild2");
			var impChild2PubChild1 = GetMetadataWriter ("impChild2PubChild1");
			var impChild2PubChild2 = GetMetadataWriter ("impChild2PubChild2");
			var impChild2ImpChild1 = GetMetadataWriter ("impChild2ImpChild1");
			var impChild2ImpChild2 = GetMetadataWriter ("impChild2ImpChild2");

			var pubChild2 = new NativeActivityRunner ((metadata) => {
				Console.WriteLine ("pubChild2");
				metadata.AddImplementationDelegate (pubChild2ImpChild2);
				metadata.AddDelegate (pubChild2PubChild2);
				metadata.AddImplementationDelegate (pubChild2ImpChild1);
				metadata.AddDelegate (pubChild2PubChild1);
			}, (context) => {
				context.ScheduleAction (pubChild2ImpChild2);
				context.ScheduleAction (pubChild2PubChild2);
				context.ScheduleAction (pubChild2ImpChild1);
				context.ScheduleAction (pubChild2PubChild1);
			});

			var impChild2 = new NativeActivityRunner (metadata => {
				Console.WriteLine ("impChild2");
				metadata.AddImplementationDelegate (impChild2ImpChild2);
				metadata.AddDelegate (impChild2PubChild2);
				metadata.AddImplementationDelegate (impChild2ImpChild1);
				metadata.AddDelegate (impChild2PubChild1);
			}, (context) => {
				context.ScheduleAction (impChild2ImpChild2);
				context.ScheduleAction (impChild2PubChild2);
				context.ScheduleAction (impChild2ImpChild1);
				context.ScheduleAction (impChild2PubChild1);
			});

			var pubChild1 = new NativeActivityRunner ((metadata) => {
				Console.WriteLine ("pubChild1");
				metadata.AddImplementationDelegate (pubChild1ImpChild2);
				metadata.AddDelegate (pubChild1PubChild2);
				metadata.AddImplementationDelegate (pubChild1ImpChild1);
				metadata.AddDelegate (pubChild1PubChild1);
			}, (context) => {
				context.ScheduleAction (pubChild1ImpChild2);
				context.ScheduleAction (pubChild1PubChild2);
				context.ScheduleAction (pubChild1ImpChild1);
				context.ScheduleAction (pubChild1PubChild1);
			});

			var impChild1 = new NativeActivityRunner (metadata => {
				Console.WriteLine ("impChild1");
				metadata.AddImplementationDelegate (impChild1ImpChild2);
				metadata.AddDelegate (impChild1PubChild2);
				metadata.AddImplementationDelegate (impChild1ImpChild1);
				metadata.AddDelegate (impChild1PubChild1);
			}, (context) => {
				context.ScheduleAction (impChild1ImpChild2);
				context.ScheduleAction (impChild1PubChild2);
				context.ScheduleAction (impChild1ImpChild1);
				context.ScheduleAction (impChild1PubChild1);
			});

			var wf = new NativeActivityRunner (metadata => {
				Console.WriteLine ("wf");
				metadata.AddImplementationChild (impChild2);
				metadata.AddChild (pubChild2);
				metadata.AddImplementationChild (impChild1);
				metadata.AddChild (pubChild1);
			}, (context) => {
				context.ScheduleActivity (impChild2);
				context.ScheduleActivity (pubChild2);
				context.ScheduleActivity (impChild1);
				context.ScheduleActivity (pubChild1);
			});

			var app = new WFAppWrapper (wf);
			app.Run ();
			//Test Order Called
			var split = app.ConsoleOut.Split (new string [] { Environment.NewLine }, StringSplitOptions.None);
			//remove trailing empty string
			var actualOrder = new string [split.Length - 1];
			for (int i = 0; i < split.Length - 1; i++)
				actualOrder [i] = split [i];
			var expected = new string [] {
				"wf",
				"impChild1", "impChild1ImpChild1", "impChild1ImpChild2", "impChild1PubChild1", "impChild1PubChild2",
				"impChild2", "impChild2ImpChild1", "impChild2ImpChild2", "impChild2PubChild1", "impChild2PubChild2",
				"pubChild1", "pubChild1ImpChild1", "pubChild1ImpChild2", "pubChild1PubChild1", "pubChild1PubChild2",
				"pubChild2", "pubChild2ImpChild1", "pubChild2ImpChild2", "pubChild2PubChild1", "pubChild2PubChild2",
			};
			Assert.AreEqual (expected, actualOrder);

			// Test Activity Ids Generated
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("2", pubChild1.Id);
			Assert.AreEqual ("3", pubChild1PubChild1.Handler.Id);
			Assert.AreEqual ("4", pubChild1PubChild2.Handler.Id);
			Assert.AreEqual ("2.1", pubChild1ImpChild1.Handler.Id);
			Assert.AreEqual ("2.2", pubChild1ImpChild2.Handler.Id);
			Assert.AreEqual ("1.1", impChild1.Id);
			Assert.AreEqual ("1.2", impChild1PubChild1.Handler.Id);
			Assert.AreEqual ("1.3", impChild1PubChild2.Handler.Id);
			Assert.AreEqual ("1.1.1", impChild1ImpChild1.Handler.Id);
			Assert.AreEqual ("1.1.2", impChild1ImpChild2.Handler.Id);
			Assert.AreEqual ("5", pubChild2.Id);
			Assert.AreEqual ("6", pubChild2PubChild1.Handler.Id);
			Assert.AreEqual ("7", pubChild2PubChild2.Handler.Id);
			Assert.AreEqual ("5.1", pubChild2ImpChild1.Handler.Id);
			Assert.AreEqual ("5.2", pubChild2ImpChild2.Handler.Id);
			Assert.AreEqual ("1.4", impChild2.Id);
			Assert.AreEqual ("1.5", impChild2PubChild1.Handler.Id);
			Assert.AreEqual ("1.6", impChild2PubChild2.Handler.Id);
			Assert.AreEqual ("1.4.1", impChild2ImpChild1.Handler.Id);
			Assert.AreEqual ("1.4.2", impChild2ImpChild2.Handler.Id);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Exceptions")]
		public void ReuseDelegateArgsEx ()
		{
			// FIXME: is this the best place for this test?

			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'Sequence': DelegateArgument '' can not be used on Activity 'Sequence' because it is already in use by Activity 'Sequence'.

			var argStr1 = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string, string> {
				Argument1 = argStr1,
				Argument2 = argStr1,
				Handler = new Sequence {
					Activities = {
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						},
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						},
					}
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1", "2");
			});
			RunAndCompare (wf, "1" + Environment.NewLine + 
				       "2" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Exceptions")]
		public void NotDeclaredDelegateArgsEx ()
		{
			// FIXME: is this the best place for this test?
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'DelegateArgumentValue<String>': DelegateArgument '' must be included in an activity's ActivityDelegate before it is used.
			// 'DelegateArgumentValue<String>': The referenced DelegateArgument object ('') is not visible at this scope.
			var argStr1 = new DelegateInArgument<string> ();
			var writeAction = new ActivityAction<string> {
				Handler = new Sequence {
					Activities = {
						new WriteLine { 
							Text = new InArgument<string> (argStr1)
						}
					}
				}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction, "1");
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void PubVarAccessFromPubDelegate ()
		{
			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleAction (action);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void PubVarAccessFromPubChildPubDelegate ()
		{
			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleAction (action);
			});
			var wf = new Sequence {
				Variables = { varStr },
				Activities = { child }
			};
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void PubVarAccessFromPubChildImpDelegateEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '3: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.

			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (action);
			}, (context) => {
				context.ScheduleAction (action);
			});
			var wf = new Sequence {
				Variables = { varStr },
				Activities = { child }
			};
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void PubVarAccessFromImpChildPubDelegateEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.

			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleAction(action);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleActivity (child);
			});

			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void PubVarAccessFromImpChildImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.
			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ( (metadata) => {
				metadata.AddImplementationDelegate (action);
			}, (context) => {
				context.ScheduleAction(action);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleActivity (child);
			});

			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void ImpVarAccessFromImpChildPubDelegate ()
		{
			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleAction (action);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity (child);
			});

			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ImpVarAccessFromPubChildPubDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be 
			// another location reference with the same name that is visible at this scope, but it does not reference the same location.

			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleAction(action);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity (child);
			});

			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ImpVarAccessFromPubChildImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '2: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location.
			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (action);
			}, (context) => {
				context.ScheduleAction(action);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity (child);
			});

			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ImpVarAccessFromImpChildImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:  
			//The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			//with the same name that is visible at this scope, but it does not reference the same location.

			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (action);
			}, (context) => {
				context.ScheduleAction(action);
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (child);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleActivity (child);
			});

			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void PubVarAccessFromImpDelegateEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:
			// The referenced Variable object (Name = '') is not visible at this scope.  There may be another location reference 
			// with the same name that is visible at this scope, but it does not reference the same location

			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (action);
				metadata.AddVariable (varStr);
			}, (context) => {
				context.ScheduleAction (action);
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ImpVarAccessFromImpDelegate ()
		{
			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (action);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleAction (action);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ImpVarAccessFromPubDelegateEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'VariableValue<String>': The referenced Variable object (Name = '') is not visible at this scope.  There may be 
			// another location reference with the same name that is visible at this scope, but it does not reference the same location.

			Variable<string> varStr;
			var action = GetActionVariableWriter (out varStr, "Hello\nWorld");

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
				metadata.AddImplementationVariable (varStr);
			}, (context) => {
				context.ScheduleAction (action);
			});
			WorkflowInvoker.Invoke (wf);
		}
		class PublicDelegateRunner<T> : NativeActivity {	
			ActivityAction<T> aAction;
			T value;
			public PublicDelegateRunner (ActivityAction<T> action, T value)
			{
				aAction = action;
				this.value = value;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddDelegate (aAction);
			}
			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleAction<T> (aAction, value);
			}			
		}
		class ImplementationDelegateRunner<T> : NativeActivity {	
			ActivityAction<T> aAction;
			T value;
			public ImplementationDelegateRunner (ActivityAction<T> action, T value)
			{
				aAction = action;
				this.value = value;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddImplementationDelegate (aAction);
			}
			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleAction<T> (aAction, value);
			}			
		}
		class ImplementationHolder<T> : NativeActivity where T:Activity {
			public T Activity { get; set; }

			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				metadata.AddImplementationChild (Activity);
			}

			protected override void Execute (NativeActivityContext context)
			{
				context.ScheduleActivity (Activity);
			}
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessDelArgFromHndlrImpChildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			   'WriteLineHolder': The private implementation of activity '2: WriteLineHolder' has the following validation error
			   The referenced DelegateArgument object ('') is not visible at this scope.
			*/
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new WriteLineHolder {
					ImplementationWriteLine = new WriteLine {
						Text = new InArgument<string> (delArg)
					}
				}
			};

			var wf = new PublicDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessDelArgFromHndlrImpChildsImpChildEx ()
		{
			/* System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			   'ImplementationHolder<ImplementationHolder<WriteLine>>': The private implementation of activity 
			   '2: ImplementationHolder<ImplementationHolder<WriteLine>>' has the following validation error:
			   The referenced DelegateArgument object ('') is not visible at this scope.
			 */ 
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<ImplementationHolder<WriteLine>> {
					Activity = new ImplementationHolder<WriteLine> {
						Activity = new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};

			var wf = new PublicDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessDelArgFromHndlrImpChildsPubChildEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'ImplementationHolder<Sequence>': The private implementation of activity '2: ImplementationHolder<Sequence>' has the following validation error:
			  The referenced DelegateArgument object ('') is not visible at this scope.
			 */ 

			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<Sequence> {
					Activity = new Sequence {
						Activities = {
							new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new PublicDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AccessDelArgFromHndlrPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};

			var wf = new PublicDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AccessDelArgFromHndlrPubChildsPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new Sequence {
							Activities = {
								new WriteLine {
									Text = new InArgument<string> (delArg)
								}
							}
						}
					}
				}
			};

			var wf = new PublicDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessDelArgFromHndlrPubChildsImpChildEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'ImplementationHolder<WriteLine>': The private implementation of activity '3: ImplementationHolder<WriteLine>' has the 
			// following validation error:   The referenced DelegateArgument object ('') is not visible at this scope.
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new ImplementationHolder<WriteLine> {
							Activity = new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new PublicDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		[Ignore ("DelegateArgument Get / Set Methods")]
		public void AccessDelArgFromExecuteEx ()
		{
			//System.InvalidOperationException : DelegateArgument 'Argument' does not exist in this environment.
			var delArg = new DelegateInArgument<string> ();
			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new NativeActivityRunner (null, (context) => {
					Console.WriteLine ((string) delArg.Get (context));
				})
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (action);
			}, (context) => {
				context.ScheduleAction (action, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void ScheduleMultipleActions ()
		{
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var action1 = new ActivityAction<string> {
				Argument = delArg1,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg1)
				}
			};
			var action2 = new ActivityAction<string> {
				Argument = delArg2,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg2)
				}
			};
			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action1);
				metadata.AddDelegate (action2);
			}, (context) => {
				context.ScheduleAction (action2, "Arg2");
				context.ScheduleAction (action1, "Arg1");
			});
			RunAndCompare (wf, String.Format ("Arg1{0}Arg2{0}", Environment.NewLine));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void ScheduleMultipleActionsCrossedArgsEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'DelegateArgumentValue<String>': DelegateArgument '' must be included in an activity's ActivityDelegate before it is used.
			  'DelegateArgumentValue<String>': The referenced DelegateArgument object ('') is not visible at this scope.
			 */ 
			var delArg1 = new DelegateInArgument<string> ();
			var delArg2 = new DelegateInArgument<string> ();
			var action1 = new ActivityAction<string> {
				Argument = delArg1,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg1)
				}
			};
			var action2 = new ActivityAction<string> {
				Argument = delArg2,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg1) // should cause error
				}
			};
			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action1);
				metadata.AddDelegate (action2);
			}, (context) => {
				context.ScheduleAction (action2, "Arg2");
				context.ScheduleAction (action1, "Arg1");
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void ScheduleActionsMultipleTimesDifArgs ()
		{

			var delArg = new DelegateInArgument<string> ();
			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new WriteLine {
					Text = new InArgument<string> (delArg)
				}
			};

			//FIXME: do i need to test ImplementationDelegates too?
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				context.ScheduleAction (action, "Run2");
				context.ScheduleAction (action, "Run1");
			});
			RunAndCompare (wf, String.Format ("Run1{0}Run2{0}", Environment.NewLine));
		}

		#region ------------MAYBE DONT KEEP THESE TESTS ---------------------
		/*just show setting delegate public or implemenetation doesnt affect scoping rules for 
		 * arguments passed in*/

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Implementation_AccessDelArgFromHndlrImpChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new WriteLineHolder {
					ImplementationWriteLine = new WriteLine {
						Text = new InArgument<string> (delArg)
					}
				}
			};

			var wf = new ImplementationDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Implementation_AccessDelArgFromHndlrImpChildsImpChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<ImplementationHolder<WriteLine>> {
					Activity = new ImplementationHolder<WriteLine> {
						Activity = new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Implementation_AccessDelArgFromHndlrImpChildsPubChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new ImplementationHolder<Sequence> {
					Activity = new Sequence {
						Activities = {
							new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void Implementation_AccessDelArgFromHndlrPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new WriteLine {
							Text = new InArgument<string> (delArg)
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void Implementation_AccessDelArgFromHndlrPubChildsPubChild ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new Sequence {
							Activities = {
								new WriteLine {
									Text = new InArgument<string> (delArg)
								}
							}
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Implementation_AccessDelArgFromHndlrPubChildsImpChildEx ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new ImplementationHolder<WriteLine> {
							Activity = new WriteLine {
								Text = new InArgument<string> (delArg)
							}
						}
					}
				}
			};

			var wf = new ImplementationDelegateRunner<string> (action, "Hello\nWorld");
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		#endregion
		[Test]
		public void DelegateIds ()
		{
			// can't use RunAndCompare as need access to workflow before comparing
			var sw = new StringWriter ();
			Console.SetOut (sw);

			var action1 = new ActivityAction {
				Handler = new TrackIdWrite ()
			};
			var action2 = new ActivityAction {
				Handler = new TrackIdWrite ()
			};
			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action1);
				metadata.AddImplementationDelegate (action2);
			}, (context) => {
				context.ScheduleAction(action1);
				context.ScheduleAction(action2);});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (child);
			}, (context) => {
				context.ScheduleActivity(child);
			});

			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual (String.Format("CacheId: {1} ActivityInstanceId: 4 Id: 2.1{0}" +
						       "CacheId: {1} ActivityInstanceId: 3 Id: 3{0}", 
					 Environment.NewLine, wf.CacheId), sw.ToString ());
		}
		[Test]
		[Ignore ("DelegateArgumentReference")]
		public void DelegateInArgValueChanged ()
		{
			var delArg = new DelegateInArgument<string> ();

			var action = new ActivityAction<string> {
				Argument = delArg,
				Handler = new Sequence {
					Activities = {
						new WriteLine { Text = new InArgument<string> (delArg) },
						new Assign { 
							To = new OutArgument<string> (delArg),
							Value = new InArgument<string> ("Changed")
						},
						new WriteLine { Text = new InArgument<string> (delArg) },
						new Sequence {
							Activities = {
								new WriteLine { Text = new InArgument<string> (delArg) },
							}
						},
					}
				}
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);}, (context) => {
				context.ScheduleAction (action, "Hello\nWorld");});

			RunAndCompare (wf, String.Format ("Hello\nWorld{0}Changed{0}Changed{0}", Environment.NewLine));
		}
		[Test]
		public void AccessDelArgFromHndlrsPubDelegateHndlr ()
		{
			var delArg = new DelegateInArgument<string> ();
			var childAction = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> (delArg) }
			};
			var parentAction = new ActivityAction<string> {
				Argument = delArg,
				Handler = new NativeActivityRunner ((metadata) => {
					metadata.AddDelegate (childAction);
				}, (context) => {
					context.ScheduleAction (childAction);
				})
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (parentAction);
			}, (context) => {
				context.ScheduleAction (parentAction, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AccessDelArgFromHndlrsImpDelegateHndlrEx ()
		{
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '2: NativeRunnerMock' has the following validation error:  
			// The referenced DelegateArgument object ('') is not visible at this scope.
			var delArg = new DelegateInArgument<string> ();
			var childAction = new ActivityAction {
				Handler = new WriteLine { Text = new InArgument<string> (delArg) }
			};
			var parentAction = new ActivityAction<string> {
				Argument = delArg,
				Handler = new NativeActivityRunner ((metadata) => {
					metadata.AddImplementationDelegate (childAction);
				}, (context) => {
					context.ScheduleAction (childAction);
				})
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (parentAction);
			}, (context) => {
				context.ScheduleAction (parentAction, "Hello\nWorld");
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		[Ignore ("ActivityDelegate with null handler given Activity")]
		public void ScheduleActivityAction_WithNullHandler_GivenHandler ()
		{
			var action = new ActivityAction ();
			ActivityInstance ai = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (action);
			}, (context) => {
				ai = context.ScheduleAction (action);
				// .NET still returns an Activity when Handler empty
				Assert.IsNotNull (ai);
				Assert.IsNotNull (ai.Activity);
				Assert.AreEqual (ActivityInstanceState.Closed, ai.State);
				Assert.IsTrue (ai.IsCompleted);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsNull (action.Handler);
			Assert.AreEqual ("0", ai.Id);
		}
		[Test]
		[Ignore ("ActivityDelegate with null handler given Activity")]
		public void ScheduleActivityFunc_WithNullHandler_GivenHandlerButExceptionsIfCallback ()
		{
			var func = new ActivityFunc<int> ();
			ActivityInstance ai = null;
			Exception schedWithCBEx = null;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				try {
				ai = context.ScheduleFunc (func, (ctx, compAI, value) => {
					Assert.AreSame (0, value);
				});
				} catch (Exception ex2) {
					schedWithCBEx = ex2;
				}
				ai = context.ScheduleFunc (func);
				Assert.IsNotNull (ai);
				Assert.IsNotNull (ai.Activity);
				Assert.AreEqual (ActivityInstanceState.Closed, ai.State);
				Assert.IsTrue (ai.IsCompleted);
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsNull (func.Handler);
			Assert.AreEqual ("0", ai.Id);
			Assert.IsInstanceOfType (typeof (InvalidCastException), schedWithCBEx);
		}
		[Test]
		public void ScheduleDelegate_DelegateCompletionCallback_ActivityFunc_NoDelOutArg ()
		{
			var func = new ActivityFunc<string> {
				Handler = new Concat { String1 = "Hello", String2 = "\nWorld" }
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleDelegate (func, null, (ctx, compAI, outArgs) => {
					Assert.AreEqual (0, outArgs.Count);
				});
			});
			WorkflowInvoker.Invoke (wf);
			Assert.IsNull (func.Result);
			Assert.IsNotNull (((CodeActivity<string>) func.Handler).Result);
		}
		[Test]
		public void ScheduleDelegate_DelegateCompletionCallback_ActivityFunc_WithDelOutArg ()
		{
			var delOut = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = delOut,
				Handler = new Concat { String1 = "Hello", String2 = "\nWorld",
					Result = delOut }
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleDelegate (func, null, (ctx, compAI, outArgs) => {
					Assert.AreEqual (1, outArgs.Count);
					Console.WriteLine (outArgs ["Result"]);
				});
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void ScheduleDelegate_DelegateCompletionCallback_FaultCallback_ActivityFunc_WithDelOutArg ()
		{
			var delOut = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = delOut,
				Handler = new HelloWorldEx { Result = delOut }
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleDelegate (func, null, (ctx, compAI, outArgs) => {
					Console.WriteLine ("CompCB State:" + compAI.State);
					Assert.AreEqual (1, outArgs.Count);
					Console.WriteLine (outArgs ["Result"]);
				},(ctx, ex, propAI) => {
					Console.WriteLine ("FaultCB State:" + propAI.State);
					ctx.HandleFault ();
				});
			});
			//note helloWorldEx sets Result arg before it exceptions
			RunAndCompare (wf, String.Format ("FaultCB State:Faulted{0}CompCB State:Faulted{0}Hello\nWorld{0}", Environment.NewLine));
		}
		class NeverBoundResultArgDelegate : ActivityDelegate {
			public DelegateOutArgument<string> NeverBoundOutString { get; set; }
			protected override DelegateOutArgument GetResultArgument ()
			{
				return NeverBoundOutString;
			}
			protected override void OnGetRuntimeDelegateArguments (IList<RuntimeDelegateArgument> runtimeDelegateArguments)
			{
			}
		}
		[Test]
		public void GetResultArgument_ArgumentNotBound ()
		{
			var outArg = new DelegateOutArgument<string> ();
			var random = new NeverBoundResultArgDelegate {
				NeverBoundOutString = outArg,
				Handler = new WriteLine { Text = "Hello\nWorld" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (random);
			}, (context) => {
				context.ScheduleDelegate (random, null, (ctx, compAI, outArgs) => {
					Assert.AreEqual (0, outArgs.Count);
				});
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		[Ignore ("Validation")]
		public void GetResultArgument_ArgumentNotBound_ButUsed ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			 *'DelegateArgumentReference<String>': DelegateArgument '' must be included in an activity's ActivityDelegate before it is used.
			 *'DelegateArgumentReference<String>': The referenced DelegateArgument object ('') is not visible at this scope.
			*/
			var outArg = new DelegateOutArgument<string> ();
			var random = new NeverBoundResultArgDelegate {
				NeverBoundOutString = outArg,
				Handler = new Concat { String1 = "1", Result = outArg }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (random);
			}, (context) => {
				context.ScheduleDelegate (random, null, (ctx, compAI, outArgs) => {
					Assert.AreEqual (0, outArgs.Count);
				});
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		class MultiOutArgDelegate : ActivityDelegate {
			public DelegateOutArgument<string> OutStr1 { get; set; }
			public DelegateOutArgument<string> OutStr2 { get; set; }
			public DelegateOutArgument<string> OutStr3 { get; set; }
			public DelegateOutArgument<string> OutStr4 { get; set; }
			protected override DelegateOutArgument GetResultArgument ()
			{
				return OutStr2; // makes no difference
			}
			protected override void OnGetRuntimeDelegateArguments (IList<RuntimeDelegateArgument> runtimeDelegateArguments)
			{
				var outStr1 = new RuntimeDelegateArgument ("OutStr1",
									   typeof (String),
									   ArgumentDirection.Out,
									   OutStr1);
				runtimeDelegateArguments.Add (outStr1);
				var outStr2 = new RuntimeDelegateArgument ("OutStr2",
									   typeof (String),
									   ArgumentDirection.Out,
									   OutStr2);
				runtimeDelegateArguments.Add (outStr2);
				var outStr3 = new RuntimeDelegateArgument ("OutStr3",
									   typeof (String),
									   ArgumentDirection.Out,
									   OutStr3);
				runtimeDelegateArguments.Add (outStr3);
				var outStr4 = new RuntimeDelegateArgument ("OutStr4",
									   typeof (String),
									   ArgumentDirection.Out,
									   OutStr4);
				runtimeDelegateArguments.Add (outStr4);
			}
		}
		[Test]
		public void MultiOutArgDelegateTest ()
		{
			var delOutStr1 = new DelegateOutArgument<string> ();
			var delOutStr2 = new DelegateOutArgument<string> ();
			var delOutStr3 = new DelegateOutArgument<string> ();
			var random = new MultiOutArgDelegate {
				OutStr1 = delOutStr1,
				OutStr2 = delOutStr2,
				OutStr3 = delOutStr3,
				Handler = new Sequence { Activities = {
						new Concat { String1 = "1", String2 = "1", Result = delOutStr1 },
						new Concat { String1 = "2", String2 = "2", Result = delOutStr2 },
					} }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (random);
			}, (context) => {
				context.ScheduleDelegate (random, null, (ctx, compAI, outArgs) => {
					Assert.AreEqual (3, outArgs.Count);
					Console.WriteLine (outArgs ["OutStr1"]);
					Console.WriteLine (outArgs ["OutStr2"]);
					Console.WriteLine (outArgs ["OutStr3"]);
					Assert.IsNull (outArgs ["OutStr3"]);
				});
			});
			RunAndCompare (wf, String.Format ("11{0}22{0}{0}", Environment.NewLine));
		}
	}
}

