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
	class InArgumentT_Test {
		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}
		#region Ctors
		[Test]
		public void Ctor_Paramless ()
		{
			var inArg = new InArgument<string> ();
			Assert.IsNull (inArg.Expression);
			Assert.AreEqual (typeof (string), inArg.ArgumentType);
		}

		[Test]
		public void Ctor_T ()
		{
			var inArgStr = new InArgument<string> ("Hello\nWorld");
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
			throw new NotImplementedException ();
		}

		[Test]
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
			Assert.IsInstanceOfType (typeof (VariableValue<string>), argStr.Expression);
			Assert.AreSame (vStr, ((VariableValue<string>) argStr.Expression).Variable);
			// .NET doesnt raise error when Variable type param doesnt match args
			var vInt = new Variable<int> ();
			var argStr2 = new InArgument<string> (vInt);
			Assert.IsInstanceOfType (typeof (VariableValue<string>), argStr2.Expression);
			Assert.AreEqual (typeof (string), argStr2.ArgumentType);
			Assert.AreSame (vInt, ((VariableValue<string>) argStr2.Expression).Variable);
		}
		#endregion

		#region Properties
		/* tested in ctor tests
		[Test]
		public void ArgumentType ()
		{
			
		}
		[Test]
		public void Expression ()
		{
			
		}
		*/ 
		#endregion

		#region Methods
		[Test]
		public void FromDelegateArgument ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void FromExpression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void FromValue ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void FromVariable ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void Set_Get_GetT_GetLocation_ForLiteral ()
		{
			//FIXME: no argument validation tests

			var InStr = new InArgument<string> ("DefaultValue");

			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				var rtInStr = new RuntimeArgument ("InStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStr);
				metadata.Bind (InStr, rtInStr);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (InStr)); // check default value used
				Assert.AreEqual ("DefaultValue", InStr.Get (context));// check Get returns value set in Ctor
				
				Location LocInStrWithDef = InStr.GetLocation (context);
				Assert.AreEqual (typeof (string), LocInStrWithDef.LocationType); 
				Assert.AreEqual ("DefaultValue", LocInStrWithDef.Value);
				
				InStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (InStr)); // check Set
				Assert.AreEqual ("DefaultValue", InStr.Expression.ToString ()); // check Expression remains the same
				
				Assert.AreEqual ("SetT", InStr.Get (context)); // check Get returns new value
				
				InStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (InStr)); // check Set
				Assert.AreEqual ("DefaultValue", InStr.Expression.ToString ()); // check Expression remains the same
				
				Assert.AreEqual ("SetO", InStr.Get (context)); // check Get returns new value
				
				Assert.AreEqual ("SetO", LocInStrWithDef.Value); // check location has been updated
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			WorkflowInvoker.Invoke (wf);
		}

		[Test]
		public void Set_Get_GetT_GetLocation_ForVariable ()
		{
			var varStr = new Variable<string> ("", "DefaultValue");
			var InStr = new InArgument<string> (varStr);
			
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				var rtInStr = new RuntimeArgument ("InStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStr);
				metadata.Bind (InStr, rtInStr);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (InStr)); // check default value used
				Assert.AreEqual ("DefaultValue", InStr.Get (context));// check Get returns value set in Ctor

				Location LocInStr = InStr.GetLocation (context);
				Assert.AreEqual (typeof (string), LocInStr.LocationType); 
				Assert.AreEqual ("DefaultValue", LocInStr.Value);

				InStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (InStr)); // check Set affects value as its seen in this scope
				Assert.AreEqual ("SetT", InStr.Get (context)); // check Get returns new value

				InStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (InStr)); // check Set
				Assert.AreEqual ("SetO", InStr.Get (context)); // check Get returns new value
				
				Assert.AreEqual ("SetO", LocInStr.Value); // check location has been updated

			};
			var wf = new Sequence {
				Variables = { varStr },
				Activities = { 
					new NativeRunnerMock (cacheMetadata, execute),
				}
			};
			WorkflowInvoker.Invoke (wf);
		}

		[Test]
		public void SetDoesntAffectVariable ()
		{
			var varStr = new Variable<string> ("", "DefaultValue");
			var InStr = new InArgument<string> (varStr);
			
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				var rtInStr = new RuntimeArgument ("InStr", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStr);
				metadata.Bind (InStr, rtInStr);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (InStr));
				InStr.Set (context, (string) "SetT");
			};
			var wf = new Sequence {
				Variables = { varStr },
				Activities = { 
					new WriteLine { Text = varStr },
					new NativeRunnerMock (cacheMetadata, execute),
					new WriteLine { Text = varStr }
				}
			};
			WorkflowInvoker.Invoke (wf);
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
			
			// the following could not be used in a workflow, but the test pass on .NET
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
		public void Implicit_ActivityT ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Implicit_DelegateArg ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Implicit_Variable ()
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}
