using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Collections.ObjectModel;
using System.IO;
using System.Activities.Statements;
using System.Activities.Expressions;

namespace MonoTests.System.Activities {
	// CodeActivityContext has internal ctor so cant be instantiated
	[TestFixture]
	public class CodeActivityContextTest {
		// **Testing ActivityContext methods here too as cant access directly to test on .NET**

		#region Methods
		[Test]
		public void SetValueT_OutArgT_GetValueT_OutArgT ()
		{
			var outInt1 = new OutArgument<int> ();
			var rtOutInt1 = new RuntimeArgument ("outInt1", typeof (int), ArgumentDirection.Out);
			
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtOutInt1);
				metadata.Bind (outInt1, rtOutInt1);
			};
			Action<CodeActivityContext> execute = (context) => {
				Assert.AreEqual (0, context.GetValue<int> (outInt1));
				context.SetValue<int> (outInt1, 30);
				Assert.AreEqual (30, context.GetValue<int> (outInt1));
				context.SetValue<int> (outInt1, 42);
				Assert.AreEqual (42, context.GetValue<int> (outInt1));
				//.NET Ignores call to Set with null
				context.SetValue<int> ((OutArgument<int>) null, 42);
			};
			var wf = new CodeActivityRunner (metadataAction, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValueT_OutArgT_NotDeclaredEx ()
		{
			var outInt1 = new OutArgument<int> ();
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<int> (outInt1);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValueT_OutArgT_NotDeclaredEx ()
		{
			//The argument of type 'System.Int32' cannot be used.  Make sure that it is declared on an activity.
			var outInt1 = new OutArgument<int> ();
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue<int> (outInt1, 42);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValueT_OutArgT_NullEx ()
		{
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<int> ((OutArgument<int>) null);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void SetValueT_InArgT_GetValueT_InArgT ()
		{
			var inString1 = new InArgument<string> ();
			var rtInString1 = new RuntimeArgument ("inString1", typeof (string), ArgumentDirection.In);
			
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtInString1);
				metadata.Bind (inString1, rtInString1);
			};
			Action<CodeActivityContext> execute = (context) => {
				Assert.AreEqual (null, context.GetValue<string> (inString1));
				context.SetValue<string> (inString1, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue<string> (inString1));
				context.SetValue<string> (inString1, "another");
				Assert.AreEqual ("another", context.GetValue<string> (inString1));
				//.NET Ignores call to Set with null
				context.SetValue<string> ((InArgument<string>) null, "another");
			};
			var wf = new CodeActivityRunner (metadataAction, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValueT_InArgT_NotDeclaredEx ()
		{
			// The argument of type 'System.String' cannot be used.  Make sure that it is declared on an activity.
			var inString1 = new InArgument<string> ();
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<string> (inString1);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValueT_InArgT_NotDeclaredEx ()
		{
			var inString1 = new InArgument<string> ();
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue<string> (inString1, "sdasdas");
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValueT_InArgT_NullEx ()
		{
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<string> ((InArgument<string>) null);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void SetValueT_InOutArgT_GetValueT_InOutArgT ()
		{
			var inOutString1 = new InOutArgument<string> ();
			var rtInOutString1 = new RuntimeArgument ("inOutString1", typeof (string), ArgumentDirection.InOut);
			
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtInOutString1);
				metadata.Bind (inOutString1, rtInOutString1);
			};
			Action<CodeActivityContext> execute = (context) => {
				Assert.AreEqual (null, context.GetValue<string> (inOutString1));
				context.SetValue<string> (inOutString1, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue<string> (inOutString1));
				context.SetValue<string> (inOutString1, "another");
				Assert.AreEqual ("another", context.GetValue<string> (inOutString1));
				//.NET Ignores call to Set with null
				context.SetValue<string> ((InOutArgument<string>) null, "another");
			};
			var wf = new CodeActivityRunner (metadataAction, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValueT_InOutArgT_NotDeclaredEx ()
		{
			var inOutString1 = new InOutArgument<string> ();
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<string> (inOutString1);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValueT_InOutArgT_NotDeclaredEx ()
		{
			var inOutString1 = new InOutArgument<string> ();
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue<string> (inOutString1, "sdasdas");
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValueT_InOutArgT_NullEx ()
		{
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<string> ((InOutArgument<string>) null);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void SetValue_Arg_GetValue_Arg ()
		{
			var inOutString1 = new InOutArgument<string> ();
			var rtInOutString1 = new RuntimeArgument ("inOutString1", typeof (string), ArgumentDirection.InOut);
			
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtInOutString1);
				metadata.Bind (inOutString1, rtInOutString1);
			};
			Action<CodeActivityContext> execute = (context) => {
				Assert.AreEqual (null, context.GetValue ((Argument) inOutString1));
				context.SetValue ((Argument) inOutString1, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue ((Argument) inOutString1));
				context.SetValue ((Argument) inOutString1, "another");
				Assert.AreEqual ("another", context.GetValue ((Argument) inOutString1));
			};
			var wf = new CodeActivityRunner (metadataAction, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValue_Arg_NotDeclaredEx ()
		{
			var inOutString1 = new InOutArgument<string> ();
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue ((Argument) inOutString1);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValue_Arg_NotDeclaredEx ()
		{
			var inOutString1 = new InOutArgument<string> ();
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue ((Argument)inOutString1, "sdasdas");
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void SetValue_Arg_NullEx ()
		{
			// .NET does not ignore call to set with null this time
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue ((Argument) null, "sads");
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValue_Arg_NullEx ()
		{
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue ((Argument) null);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void SetValueT_LocationReference_GetValueT_LocationReference ()
		{
			var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);
			
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtInOutString1);
			};
			Action<CodeActivityContext> execute = (context) => {
				Assert.AreEqual (null, context.GetValue<string> ((LocationReference) rtInOutString1));
				context.SetValue<string> ((LocationReference) rtInOutString1, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue<string> ((LocationReference) rtInOutString1));
				context.SetValue<string> ((LocationReference) rtInOutString1, "another");
				Assert.AreEqual ("another", context.GetValue<string> ((LocationReference) rtInOutString1));
			};
			var wf = new CodeActivityRunner (metadataAction, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValueT_LocationReference_NotDeclaredEx ()
		{
			//The argument 'InOutString1' cannot be used.  Make sure that it is declared on an activity.
			var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<string> ((LocationReference) rtInOutString1);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetValueT_LocationReference_NotDeclaredEx ()
		{
			var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue<string> ((LocationReference) rtInOutString1, "sdasdas");
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void SetValueT_LocationReference_NullEx ()
		{
			// .NET does not ignore call to set with null this time
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue<string> ((LocationReference) null, "sads");
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValueT_LocationReference_NullEx ()
		{
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue<string> ((LocationReference) null);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void GetValue_RuntimeArgument ()
		{
			var inOutString1 = new InOutArgument<string> ();
			var rtInOutString1 = new RuntimeArgument ("inOutString1", typeof (string), ArgumentDirection.InOut);
			
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtInOutString1);
				metadata.Bind (inOutString1, rtInOutString1);
			};
			Action<CodeActivityContext> execute = (context) => {
				Assert.AreEqual (null, context.GetValue (rtInOutString1));
				context.SetValue<string> ((LocationReference) rtInOutString1, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue(rtInOutString1));
			};
			var wf = new CodeActivityRunner (metadataAction, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetValue_RuntimeArgument_NotDeclaredEx ()
		{
			var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue (rtInOutString1);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetValue_RuntimeArgument_NullEx ()
		{
			Action<CodeActivityContext> execute = (context) => {
				context.GetValue ((RuntimeArgument) null);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void GetLocationT_LocationReference ()
		{
			var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);
			
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtInOutString1);
			};
			Action<CodeActivityContext> execute = (context) => {
				var loc = context.GetLocation<string> ((LocationReference) rtInOutString1);
				Assert.IsNull (loc.Value);
				context.SetValue<string> ((LocationReference) rtInOutString1, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", loc.Value);
			};
			var wf = new CodeActivityRunner (metadataAction, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void GetLocationT_LocationReference_NotDeclaredEx ()
		{
			var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);
			Action<CodeActivityContext> execute = (context) => {
				context.GetLocation<string> ((LocationReference) rtInOutString1);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void GetLocationT_LocationReference_NullEx ()
		{
			Action<CodeActivityContext> execute = (context) => {
				context.GetLocation<string> ((LocationReference) null);
			};
			var wf = new CodeActivityRunner (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void GetExtensionT ()
		{
			throw new NotImplementedException ();
		}
		// CodeActivityContext specific
		[Test]
		[Ignore ("Not Implemented")]
		public void GetPropertyT ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Track ()
		{
			throw new NotImplementedException ();
		}
		#endregion

		#region Properties
		[Test]
		[Ignore ("Not Implemented")]
		public void ActivityInstanceId ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void DataContext ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void WorkflowInstanceId ()
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}
