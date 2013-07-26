using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;
using System.IO;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	class InArgumentT_Test : WFTestHelper {

		#region Ctors
		[Test]
		public void Ctor ()
		{
			var inArg = new InArgument<string> ();
			Assert.IsNull (inArg.Expression);
			Assert.AreEqual (ArgumentDirection.In, inArg.Direction);
			Assert.AreEqual (typeof (string), inArg.ArgumentType);
		}
		[Test]
		public void Ctor_T ()
		{
			var inArgStr = new InArgument<string> ("Hello\nWorld");
			Assert.AreEqual (ArgumentDirection.In, inArgStr.Direction);
			Assert.IsInstanceOfType (typeof (Literal<string>), inArgStr.Expression);
			Assert.AreEqual ("Hello\nWorld", inArgStr.Expression.ToString ());
			Assert.AreEqual (typeof (string), inArgStr.ArgumentType);

			var inArgInt = new InArgument<int> (42);
			Assert.IsInstanceOfType (typeof (Literal<int>), inArgInt.Expression);
			Assert.AreEqual ("42", inArgInt.Expression.ToString ());
			Assert.AreEqual (typeof (int), inArgInt.ArgumentType);

			// These next 2 tests pass, but wouldnt compile as part of a workflow
			var list = new List<string> () { "first", "second" };
			var inArgList = new InArgument<List<string>> (list);
			Assert.IsInstanceOfType (typeof (Literal<List<string>>), inArgList.Expression);
			Assert.AreEqual (typeof (List<string>), inArgList.ArgumentType);
			
			var sw = new StringWriter ();
			sw.Write ("Hello\nWorld");
			var inArgTw = new InArgument<TextWriter> (sw);
			Assert.IsInstanceOfType (typeof (Literal<TextWriter>), inArgTw.Expression);
			Assert.AreEqual (typeof (TextWriter), inArgTw.ArgumentType);
			Assert.AreEqual ("Hello\nWorld", inArgTw.Expression.ToString ());

			var inArgNull = new InArgument<string> ((string) null);
			Assert.IsInstanceOfType (typeof (Literal<string>), inArgNull.Expression);
			Assert.AreEqual (typeof (string), inArgNull.ArgumentType);
			Assert.AreEqual ("null", inArgNull.Expression.ToString ());
		}
		[Test]
		public void Ctor_ActivityT ()
		{
			var actHello = new Literal<string> ("Hello\nWorld");
			var inArgStr = new InArgument<string> (actHello);
			Assert.AreEqual (ArgumentDirection.In, inArgStr.Direction);
			Assert.AreEqual (typeof (string), inArgStr.ArgumentType);
			Assert.AreSame (actHello, inArgStr.Expression);

			var act42 = new Literal<int> (42);
			var inArgInt = new InArgument<int> (act42);
			Assert.AreEqual (typeof (int), inArgInt.ArgumentType);
			Assert.AreSame (act42, inArgInt.Expression);

			var inArgStrNull = new InArgument<string> ((Activity<string>) null);
			Assert.IsNull (inArgStrNull.Expression);
		}
		[Test]
		public void Ctor_DelegateArgument ()
		{
			var delArg = new DelegateInArgument<string> ();
			var inArgDel = new InArgument<string> (delArg);
			Assert.AreEqual (ArgumentDirection.In, inArgDel.Direction);
			Assert.IsInstanceOfType (typeof(DelegateArgumentValue<string>), inArgDel.Expression);
			Assert.AreSame (delArg, ((DelegateArgumentValue<string>)inArgDel.Expression).DelegateArgument);
			// .NET doesnt raise error when DelegateInArgument type param doesnt match args
			// FIXME: might during WF execution
			var delArgStr = new DelegateInArgument<string> ();
			var inArgInt = new InArgument<int> (delArgStr);
			Assert.AreSame (typeof (int), inArgInt.ArgumentType);
			Assert.IsInstanceOfType (typeof (DelegateArgumentValue<int>), inArgInt.Expression);
			Assert.AreSame (delArgStr, ((DelegateArgumentValue<int>)inArgInt.Expression).DelegateArgument);
		}
		[Test]
		[Ignore ("Expressions")]
		public void Ctor_Expression  ()
		{
			Expression<Func<ActivityContext, string>> expString = ctx => "Hello\nWorld";
			var inArgStr = new InArgument<string> (expString);
			Assert.IsInstanceOfType (typeof (LambdaValue<string>), inArgStr.Expression);
			//FIXME: need to actually test the content of LambdaValue
			throw new NotImplementedException ();
		}
		[Test]
		public void Ctor_Variable ()
		{
			var vStr = new Variable<string> ();
			var argStr = new InArgument<string> (vStr);
			Assert.AreEqual (ArgumentDirection.In, argStr.Direction);
			Assert.IsInstanceOfType (typeof (VariableValue<string>), argStr.Expression);
			Assert.AreSame (vStr, ((VariableValue<string>) argStr.Expression).Variable);
			// .NET doesnt raise error when Variable type param doesnt match args
			//FIXME: may do during WF execution
			var vInt = new Variable<int> ();
			var argStr2 = new InArgument<string> (vInt);
			Assert.IsInstanceOfType (typeof (VariableValue<string>), argStr2.Expression);
			Assert.AreEqual (typeof (string), argStr2.ArgumentType);
			Assert.AreSame (vInt, ((VariableValue<string>) argStr2.Expression).Variable);
		}
		#endregion

		#region Properties
		/* tested in ctor tests
			public void ArgumentType ()
			public void Expression ()
		*/ 
		#endregion

		#region Methods
		[Test]
		[Ignore ("Not Implemented")]
		public void FromDelegateArgument ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void FromExpression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void FromValue ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void FromVariable ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Set_SetT_OGet_TGet__TGetT_GetLocation_ForLiteral ()
		{
			/* 3 Get methods:
			 * On Argument:
			 * 	public T Get<T> (ActivityContext) + public object Get (ActivityContext)
			 * On InArgument:
			 * 		public T InArgument.Get (ActivityContext)
			 */
			//FIXME: no argument validation tests

			var inStr = new InArgument<string> ("DefaultValue");

			var wf = new NativeActivityRunner ((metadata) => {
				var rtInStr = new RuntimeArgument ("inStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStr);
				metadata.Bind (inStr, rtInStr);
			}, (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (inStr)); // check default value used
				Assert.AreEqual ("DefaultValue", inStr.Get (context));// check Get returns value set in Ctor
				
				Location locInStr = inStr.GetLocation (context);
				Assert.AreEqual (typeof (string), locInStr.LocationType); 
				Assert.AreEqual ("DefaultValue", locInStr.Value);
				
				inStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (inStr)); // check Set
				Assert.AreEqual ("DefaultValue", inStr.Expression.ToString ()); // check Expression remains the same
				
				Assert.AreEqual ("SetT", ((Argument)inStr).Get (context)); // check GetO returns new value
				
				inStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (inStr)); // check Set
				Assert.AreEqual ("DefaultValue", inStr.Expression.ToString ()); // check Expression remains the same
				
				Assert.AreEqual ("SetO", inStr.Get<string> (context)); // check GetT returns new value
				
				Assert.AreEqual ("SetO", locInStr.Value); // check location has been updated
			});
			WorkflowInvoker.Invoke (wf);
		}
		//FIXME: separate ForVariable and ForLiteral tests might not be needed here, otherwise we need
		//DelegateArguments tests etc. Ultimately context is currently responsible to implementing this
		[Test]
		public void Set_SetT_Get_GetT_GetLocation_ForVariable ()
		{
			var varStr = new Variable<string> ("", "DefaultValue");
			var inStr = new InArgument<string> (varStr);

			var tester = new NativeActivityRunner ((metadata) => {
				var rtInStr = new RuntimeArgument ("inStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStr);
				metadata.Bind (inStr, rtInStr);
			}, (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (inStr)); // check default value used
				Assert.AreEqual ("DefaultValue", inStr.Get (context));// check Get returns value set in Ctor
				
				Location locInStr = inStr.GetLocation (context);
				Assert.AreEqual (typeof (string), locInStr.LocationType); 
				Assert.AreEqual ("DefaultValue", locInStr.Value);
				
				inStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (inStr)); // check Set affects value as its seen in this scope
				Assert.AreEqual ("SetT", inStr.Get (context)); // check Get returns new value
				
				inStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (inStr)); // check Set
				Assert.AreEqual ("SetO", inStr.Get (context)); // check Get returns new value
				
				Assert.AreEqual ("SetO", locInStr.Value); // check location has been updated
			});

			var wf = new Sequence {
				Variables = { varStr },
				Activities = { 
					tester,
				}
			};
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void SetDoesntAffectVariable ()
		{
			var varStr = new Variable<string> ("", "DefaultValue");
			var inStr = new InArgument<string> (varStr);

			var tester = new NativeActivityRunner ((metadata) => {
				var rtInStr = new RuntimeArgument ("inStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStr);
				metadata.Bind (inStr, rtInStr);
			}, (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (inStr));
				inStr.Set (context, (string) "SetT");
			});

			var wf = new Sequence {
				Variables = { varStr },
				Activities = {
					new WriteLine { Text = varStr },
					tester,
					new WriteLine { Text = varStr }
				}
			};
			RunAndCompare (wf, String.Format ("DefaultValue{0}DefaultValue{0}", Environment.NewLine));
		}
		[Test]
		public void ToStringTest ()
		{
			var inArg = new InArgument<string> ("Hello\nWorld");
			Assert.AreEqual (inArg.GetType ().ToString (), inArg.ToString ());
		}
		#endregion

		#region operators
		[Test]
		public void Implicit_T ()
		{
			InArgument<string> inArgStr = "Hello\nWorld";
			Assert.AreEqual (ArgumentDirection.In, inArgStr.Direction);
			Assert.IsInstanceOfType (typeof (Literal<string>), inArgStr.Expression);
			Assert.AreEqual (typeof (string), inArgStr.ArgumentType);
			Assert.AreEqual ("Hello\nWorld", inArgStr.Expression.ToString ());

			InArgument<int> inArgInt = 42;
			Assert.IsInstanceOfType (typeof (Literal<int>), inArgInt.Expression);
			Assert.AreEqual (typeof (int), inArgInt.ArgumentType);
			Assert.AreEqual ("42", inArgInt.Expression.ToString ());

			InArgument<bool> inArgBool = true;
			Assert.IsInstanceOfType (typeof (Literal<bool>), inArgBool.Expression);
			Assert.AreEqual (typeof (bool), inArgBool.ArgumentType);
			Assert.AreEqual ("True", inArgBool.Expression.ToString ());
			
			// the following could not be used in a workflow, but the test passes on .NET
			var sw = new StringWriter ();
			sw.Write ("Hello\nWorld");
			InArgument<TextWriter> inArgTw = sw;
			Assert.IsInstanceOfType (typeof (Literal<TextWriter>), inArgTw.Expression);
			Assert.AreEqual (typeof (TextWriter), inArgTw.ArgumentType);
			Assert.AreEqual ("Hello\nWorld", inArgTw.Expression.ToString ());

			InArgument<string> inArgStrNull = (string) null;
			Assert.IsInstanceOfType (typeof (Literal<String>), inArgStrNull.Expression);
			Assert.AreEqual (typeof (String), inArgStrNull.ArgumentType);
			Assert.AreEqual ("null", inArgStrNull.Expression.ToString ());
		}
		[Test]
		public void Implicit_Variable ()
		{
			var varStr = new Variable<string> ("name", "value");
			InArgument<string> inStr = varStr;
			Assert.AreEqual (ArgumentDirection.In, inStr.Direction);
			Assert.IsInstanceOfType (typeof (VariableValue<string>), inStr.Expression);
			Assert.AreSame(varStr, ((VariableValue<string>) inStr.Expression).Variable);

			//wrong type
			InArgument<int> inInt = varStr;
			Assert.IsInstanceOfType (typeof (VariableValue<int>), inInt.Expression);
			Assert.AreSame(varStr, ((VariableValue<int>) inInt.Expression).Variable);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Implicit_ActivityT ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Implicit_DelegateArg ()
		{
			var delInStr = new DelegateInArgument<string> ();
			InArgument<string> inStr = delInStr;
			Assert.AreEqual (ArgumentDirection.In, inStr.Direction);
			Assert.IsInstanceOfType (typeof (DelegateArgumentValue<string>), inStr.Expression);
			Assert.AreSame(delInStr, ((DelegateArgumentValue<string>) inStr.Expression).DelegateArgument);

			//wrong param type
			var delInDouble = new DelegateInArgument<double> ();
			InArgument<string> inWrongType = delInDouble;
			Assert.IsInstanceOfType (typeof (DelegateArgumentValue<string>), inWrongType.Expression);
			Assert.AreSame(delInDouble, ((DelegateArgumentValue<string>) inWrongType.Expression).DelegateArgument);

			//wrong param direction
			var delOutFloat = new DelegateOutArgument<float> ();
			InArgument<float> inWrongDir = delOutFloat;
			Assert.IsInstanceOfType (typeof (DelegateArgumentValue<float>), inWrongDir.Expression);
			Assert.AreSame(delOutFloat, ((DelegateArgumentValue<float>) inWrongDir.Expression).DelegateArgument);
		}
		#endregion
	}
}
