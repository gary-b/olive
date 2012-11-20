using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;
using System.IO;

namespace Tests.System.Activities {
	[TestFixture]
	class InArgumentT_Test {
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
			throw new NotImplementedException ();
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

		class CodeArgsMock : CodeActivity {
			public InArgument<string> InString1 { get; set; }
			public InArgument<string> CtorString { get; set; }

			public CodeArgsMock ()
			{
				CtorString = new InArgument<string> ("SetInCtor");
			}

			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				var rtInString1 = new RuntimeArgument ("InString1", typeof (string), ArgumentDirection.In);
				var rtCtorString = new RuntimeArgument ("CtorString", typeof (string), ArgumentDirection.In);

				metadata.AddArgument (rtInString1);
				InString1 = new InArgument<string> ();
				metadata.Bind (InString1, rtInString1);

				metadata.AddArgument (rtCtorString);
				metadata.Bind (CtorString, rtCtorString);
			}

			protected override void Execute (CodeActivityContext context)
			{
				// Note: Even if this workflow invoked twice and set() was used at the end of the first run to change value of CtorString, 
				// this test will still pass. Seems value of Expression (which never changes), 
				// is used to initialise context?
				Assert.AreEqual ("SetInCtor", context.GetValue (CtorString)); // check value set in Ctor 
				Assert.AreEqual ("SetInCtor", CtorString.Get (context));// check Get returns value set in Ctor

				Location LocCtorString = CtorString.GetLocation (context);
				Assert.AreEqual (typeof (string), LocCtorString.LocationType);  //check Location type and value
				Assert.AreEqual ("SetInCtor", LocCtorString.Value);

				CtorString.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (CtorString)); // check Set
				Assert.AreEqual ("SetInCtor", CtorString.Expression.ToString ()); // check Expression remains the same

				Assert.AreEqual ("SetT", CtorString.Get (context)); // check Get returns new value
				
				CtorString.Set (context, (object)"SetO");
				Assert.AreEqual ("SetO", context.GetValue (CtorString)); // check Set
				Assert.AreEqual ("SetInCtor", CtorString.Expression.ToString ()); // check Expression remains the same

				Assert.AreEqual ("SetO", CtorString.Get (context)); // check Get returns new value

				Assert.AreEqual ("SetO", LocCtorString.Value); // check location has been updated
			}
		}

		[Test]
		public void Set_Get_GetT_GetLocation ()
		{
			//FIXME: no argument validation tests
			var wf = new CodeArgsMock ();
			WorkflowInvoker.Invoke (wf);
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
