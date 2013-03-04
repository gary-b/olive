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
	class OutArgumentT_Test {
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
			var outArg = new OutArgument<string> ();
			Assert.IsNull (outArg.Expression);
			Assert.AreEqual (typeof (string), outArg.ArgumentType);
		}

		[Test]
		[Ignore ("Not Implemented")]
		public void Ctor_ActivityLocationT ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		[Ignore ("Not Implemented")]
		public void Ctor_DelegateArgument ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		[Ignore ("Not Implemented")]
		public void Ctor_Expression  ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void Ctor_Variable ()
		{
			var vStr = new Variable<string> ();
			var argStr = new OutArgument<string> (vStr);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), argStr.Expression);
			Assert.AreSame (vStr, ((VariableReference<string>) argStr.Expression).Variable);
			// .NET doesnt raise error when Variable type param doesnt match args
			var vInt = new Variable<int> ();
			var argStr2 = new OutArgument<string> (vInt);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), argStr2.Expression);
			Assert.AreEqual (typeof (string), argStr2.ArgumentType);
			Assert.AreSame (vInt, ((VariableReference<string>) argStr2.Expression).Variable);
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
		public void FromVariable ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void Set_Get_GetT_GetLocation_ForVariable ()
		{
			//FIXME: no argument validation tests
			var varStr = new Variable<string> ("", "DefaultValue");
			var OutStr = new OutArgument<string> (varStr);

			var tester = new NativeRunnerMock ((metadata) => {
				var rtOutStr = new RuntimeArgument ("OutStr", typeof (string), ArgumentDirection.Out);
				metadata.AddArgument (rtOutStr);
				metadata.Bind (OutStr, rtOutStr);
			}, (context) => {
				Assert.IsNull (context.GetValue (OutStr)); // check default value is not used
				Assert.IsNull (OutStr.Get (context));// check Get returns same
				
				Location LocOutStr = OutStr.GetLocation (context);
				Assert.AreEqual (typeof (string), LocOutStr.LocationType); 
				Assert.IsNull (LocOutStr.Value);
				
				OutStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (OutStr)); // check Set affects value as its seen in this scope
				Assert.AreEqual ("SetT", OutStr.Get (context)); // check Get returns new value
				
				OutStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (OutStr)); // check Set
				Assert.AreEqual ("SetO", OutStr.Get (context)); // check Get returns new value
				
				Assert.AreEqual ("SetO", LocOutStr.Value); // check location has been updated
				
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
		public void SetDoesAffectVariable ()
		{
			var varStr = new Variable<string> ("", "DefaultValue");
			var OutStr = new OutArgument<string> (varStr);

			var tester = new NativeRunnerMock ((metadata) => {
				var rtOutStr = new RuntimeArgument ("OutStr", typeof (string), ArgumentDirection.Out);
				metadata.AddArgument (rtOutStr);
				metadata.Bind (OutStr, rtOutStr);
			}, (context) => {
				Assert.IsNull (context.GetValue (OutStr)); // default value isnt available
				OutStr.Set (context, (string) "SetT");
			});

			var wf = new Sequence {
				Variables = { varStr },
				Activities = { 
					new WriteLine { Text = varStr },
					tester,
					new WriteLine { Text = varStr }
				}
			};
			WorkflowInvoker.Invoke (wf);
			RunAndCompare (wf, String.Format ("DefaultValue{0}SetT{0}", Environment.NewLine));
		}

		[Test]
		public void ToStringTest ()
		{
			var outArg = new OutArgument<string> ();
			Assert.AreEqual (outArg.GetType ().ToString (), outArg.ToString ());
		}
		#endregion
		#region operators
		[Test]
		[Ignore ("Not Implemented")]
		public void Implicit_ActivityLocationT ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Implicit_DelegateArg ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Implicit_Variable ()
		{
			throw new NotImplementedException ();
		}

		#endregion
	}
}
