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
	class OutArgumentT_Test : WFTest {
		#region Ctors
		[Test]
		public void Ctor ()
		{
			var outArg = new OutArgument<string> ();
			Assert.AreEqual (ArgumentDirection.Out, outArg.Direction);
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
			Assert.AreEqual (ArgumentDirection.Out, argStr.Direction);
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
		public void FromVariable ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Set_SetT_OGet_GetT_TGetT_GetLocation_ForVariable ()
		{
			/* 3 Get methods:
			 * On Argument:
			 * 	public T Get<T> (ActivityContext) + public object Get (ActivityContext)
			 * On OutArgument:
			 * 		public T InArgument.Get (ActivityContext)
			 */
			//FIXME: no argument validation tests
			var varStr = new Variable<string> ("", "DefaultValue");
			var outStr = new OutArgument<string> (varStr);

			var tester = new NativeRunnerMock ((metadata) => {
				var rtOutStr = new RuntimeArgument ("outStr", typeof (string), ArgumentDirection.Out);
				metadata.AddArgument (rtOutStr);
				metadata.Bind (outStr, rtOutStr);
			}, (context) => {
				Assert.IsNull (context.GetValue (outStr)); // check default value is not used
				Assert.IsNull (outStr.Get (context));// check Get returns same
				
				Location locOutStr = outStr.GetLocation (context);
				Assert.AreEqual (typeof (string), locOutStr.LocationType); 
				Assert.IsNull (locOutStr.Value);
				
				outStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (outStr)); // check Set affects value as its seen in this scope
				Assert.AreEqual ("SetT", ((Argument) outStr).Get (context)); // check Get returns new value
				
				outStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (outStr)); // check Set
				Assert.AreEqual ("SetO", outStr.Get<string> (context)); // check Get returns new value

				Assert.AreEqual ("SetO", locOutStr.Value); // check location has been updated
				
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
		public void VariableUnaffectedIfNotSet ()
		{
			var PubVar = new Variable<string> ("", "HelloPublic");
			var OStr = new OutArgument<string> (PubVar);

			var tester = new NativeRunnerMock ((metadata) => {
				var rtOStr = new RuntimeArgument ("OStr", typeof (string), ArgumentDirection.Out);
				metadata.AddArgument (rtOStr);
				metadata.Bind (OStr, rtOStr);
			}, (context) => {
				Console.WriteLine (OStr.Get (context));
			});

			var wf = new Sequence {
				Variables = {
					PubVar
				},
				Activities = {
					new WriteLine {
						Text = PubVar
					},
					tester,
					new WriteLine {
						Text = PubVar
					}
				}
			};
			RunAndCompare (wf, String.Format ("HelloPublic{0}{0}HelloPublic{0}", Environment.NewLine));
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
		[Ignore ("Argument Implicit Casts")]
		public void Implicit_Variable ()
		{
			var v1 = new Variable<string> ("name","value");
			OutArgument<string> OA = v1;
			Assert.IsInstanceOfType (typeof (VariableReference<string>),OA.Expression);
			Assert.AreSame(v1, ((VariableReference<string>)OA.Expression).Variable);

			//FIXME: is this sufficient?
		}
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
		#endregion
	}
}
