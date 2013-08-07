using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;
using System.Activities.Hosting;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class WorkflowInvokerTest : WFTestHelper {
		[Test]
		[Ignore ("Extensions")]
		public void Ctor ()
		{
			var wi = new WorkflowInvoker (new WriteLine());
			Assert.IsNotNull (wi.Extensions);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor__NullEx ()
		{
			var wi = new WorkflowInvoker (null);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Invoke_Activity_NullEx ()
		{
			WorkflowInvoker.Invoke ((Activity) null);
		}
		[Test]
		public void Invoke_Activity_IDictionary ()
		{
			//note RuntimeArguments not bound to Arguments here and it still works
			RuntimeArgument rtString1 = null, rtString2 = null, rtConcat = null, rtReverseConcatInOut = null;
			var wf = new NativeActivityRunner ((metadata) => {
				rtString1 = new RuntimeArgument ("String1", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtString1);
				rtString2 = new RuntimeArgument ("String2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtString2);
				rtConcat = new RuntimeArgument ("Concat", typeof (string), ArgumentDirection.Out);
				metadata.AddArgument (rtConcat);
				rtReverseConcatInOut = new RuntimeArgument ("ReverseConcatInOut", typeof (string), 
									    ArgumentDirection.InOut);
				metadata.AddArgument (rtReverseConcatInOut);
			}, (context) => {
				Assert.IsNull (rtConcat.Get (context));
				Assert.AreEqual ("reverse", rtReverseConcatInOut.Get (context));
				rtConcat.Set (context, ((string)(rtString1.Get (context))) + 
					      		((string)(rtString2.Get (context))));
				rtReverseConcatInOut.Set (context, ((string)(rtString2.Get (context))) + 
								    ((string)(rtString1.Get (context))));
			});
			var results = WorkflowInvoker.Invoke (wf, 
							      new Dictionary<string, object> {{"String1", "Hello\n"},
												{"String2", "World"},
												{"ReverseConcatInOut", "reverse"}});
			Assert.AreEqual (2, results.Count);
			Assert.AreEqual ("Hello\nWorld", results ["Concat"]);
			Assert.AreEqual ("WorldHello\n", results ["ReverseConcatInOut"]);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void Invoke_Activity_IDictionary_UnknownKeysEx ()
		{
			/*System.ArgumentException : The values provided for the root activity's arguments did not satisfy the root activity's requirements:
			  'Concat': The following keys from the input dictionary do not map to arguments and must be removed: string2.  Please note that argument 
			  names are case sensitive.
			  Parameter name: rootArgumentValues
			*/
			var concat = new Concat ();
			WorkflowInvoker.Invoke ((Activity) concat, 
						new Dictionary<string, object> {{"String1", "Hello\n"},
										{"string2", "World"}});
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void Invoke_Activity_IDictionary_PassToOutArgEx ()
		{
			/*System.ArgumentException : The values provided for the root activity's arguments did not satisfy the root activity's requirements:
			  'Concat': The following keys from the input dictionary do not map to arguments and must be removed: Result.  Please note that 
			  argument names are case sensitive.
			  Parameter name: rootArgumentValues
			*/
			var concat = new Concat ();
			WorkflowInvoker.Invoke ((Activity) concat, 
						new Dictionary<string, object> {{"String1", "Hello\n"},
										{"String2", "World"},
										{"Result",  "hello"}});
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Invoke_Activity_IDictionary_NullDictEx ()
		{
			var concat = new Concat ();
			WorkflowInvoker.Invoke ((Activity) concat, null);
		}
		[Test]
		public void Invoke_Activity_IDictionary_EmptyDict ()
		{
			var concat = new Concat ();
			var results = WorkflowInvoker.Invoke ((Activity) concat, new Dictionary<string, object> ());
			Assert.AreEqual (String.Empty, results ["Result"]);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Invoke_Activity_IDictionary_NullActivityEx ()
		{
			WorkflowInvoker.Invoke ((Activity) null, new Dictionary<string, object> ());
		}
		[Test]
		public void InvokeT_Activity ()
		{
			var concat = new Concat {
				String1 = "Hello\n",
				String2 = "World"
			};
			var result = WorkflowInvoker.Invoke<string> (concat);
			Assert.AreEqual ("Hello\nWorld", result);
		}
		[Test]
		public void InvokeT_ActivityT_IDictionary ()
		{
			var concat = new Concat ();
			var result = WorkflowInvoker.Invoke<string> (concat, 
								     new Dictionary<string, object> {{"String1", "Hello\n"},
												     {"String2", "World"}});
			Assert.AreEqual ("Hello\nWorld", result);
		}
		[Test]
		public void InvokeT_ActivityT_IDictionary_NullDict ()
		{
			//no error here on .NET, unlike the Invoke_Activity_IDictionary overload
			var concat = new Concat ();
			var result = WorkflowInvoker.Invoke<string> (concat, null);
			Assert.AreEqual (String.Empty, result);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void InvokeT_ActivityT_IDictionary_NullActivityEx ()
		{
			WorkflowInvoker.Invoke<string> (null, new Dictionary<string, object> ());
		}
		[Test]
		public void InvokeT_ActivityT_IDictionary_AdditionalOutArgsIgnored ()
		{
			RuntimeArgument rtString1 = null, rtString2 = null, rtReverseConcat = null;
			var wf = new CodeActivityTRunner<string> ((metadata) => {
				//RuntimeArgument for Result will be created by base
				rtString1 = new RuntimeArgument ("String1", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtString1);
				rtString2 = new RuntimeArgument ("String2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtString2);
				rtReverseConcat = new RuntimeArgument ("ReverseConcat", typeof (string), ArgumentDirection.Out);
				metadata.AddArgument (rtReverseConcat);
			}, (context) => {
				Assert.IsNull (rtReverseConcat.Get (context));
				rtReverseConcat.Set (context, ((string)(rtString2.Get (context))) + 
						     		((string)(rtString1.Get (context))));
				return ((string)(rtString1.Get (context))) + ((string)(rtString2.Get (context)));
			});
			var result = WorkflowInvoker.Invoke<string> (wf, 
								     new Dictionary<string, object> {{"String1", "Hello\n"},
												     {"String2", "World"}});
			Assert.AreEqual ("Hello\nWorld", result);
		}
		class WorkflowInvokerMoreTestsClass {
			public void InvokeCompleted ()
			{
				throw new NotImplementedException ();
			}
			public void Extensions ()
			{ 
				throw new NotImplementedException ();
			}
			public void BeginInvoke_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void BeginInvoke_inputs_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void BeginInvoke_Timeout_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void BeginInvoke_inputs_Timeout_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void EndInvoke_result ()
			{
				throw new NotImplementedException ();
			}

			public void InvokeT_workflow ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_workflow_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_workflow_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeT_workflow_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_inputs_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_workflow_inputs_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeT_workflow_inputs_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeT_workflow_inputs_additionalOutputs_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_userState ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs_userState ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs_Timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_Timeout_userState ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs_Timeout_userState ()
			{
				throw new NotImplementedException ();
			}
		}
	}
}
