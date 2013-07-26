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
	class OutArgumentT_Test : WFTestHelper {
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
		public void Ctor_ActivityLocationT ()
		{
			var loc = new Location<string> ();
			var locationSupplier = new CodeActivityTRunner<Location<string>> (null, (context) => {
				return loc;
			});
			var outArg = new OutArgument<string> (locationSupplier);
			Assert.AreEqual (ArgumentDirection.Out, outArg.Direction);
			Assert.AreSame (locationSupplier, outArg.Expression);
			Assert.AreEqual (typeof (string), outArg.ArgumentType);
		}
		[Test]
		public void Ctor_DelegateArgument ()
		{
			var delArg = new DelegateOutArgument<string> ();
			var outArg = new OutArgument<string> (delArg);
			Assert.AreEqual (ArgumentDirection.Out, outArg.Direction);
			Assert.IsInstanceOfType (typeof (DelegateArgumentReference<string>), outArg.Expression);
			Assert.AreSame (delArg, ((DelegateArgumentReference<string>)outArg.Expression).DelegateArgument);
			Assert.AreEqual (typeof (string), outArg.ArgumentType);
			//no type validation in ctor
			var outArg2 = new OutArgument<int> (delArg);
			Assert.IsInstanceOfType (typeof (DelegateArgumentReference<int>), outArg2.Expression);
			Assert.AreSame (delArg, ((DelegateArgumentReference<int>) outArg2.Expression).DelegateArgument);
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

			var tester = new NativeActivityRunner ((metadata) => {
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

			var tester = new NativeActivityRunner ((metadata) => {
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

			var tester = new NativeActivityRunner ((metadata) => {
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
		public void Implicit_Variable ()
		{
			var varStr = new Variable<string> ("name", "value");
			OutArgument<string> outStr = varStr;
			Assert.AreEqual (ArgumentDirection.Out, outStr.Direction);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), outStr.Expression);
			Assert.AreSame(varStr, ((VariableReference<string>) outStr.Expression).Variable);

			//wrong type
			OutArgument<int> outInt = varStr;
			Assert.IsInstanceOfType (typeof (VariableReference<int>), outInt.Expression);
			Assert.AreSame(varStr, ((VariableReference<int>) outInt.Expression).Variable);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Implicit_ActivityLocationT ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Implicit_DelegateArg ()
		{
			var delOutStr = new DelegateOutArgument<string> ();
			OutArgument<string> outStr = delOutStr;
			Assert.AreEqual (ArgumentDirection.Out, outStr.Direction);
			Assert.IsInstanceOfType (typeof (DelegateArgumentReference<string>), outStr.Expression);
			Assert.AreSame(delOutStr, ((DelegateArgumentReference<string>) outStr.Expression).DelegateArgument);

			//wrong param type
			var delOutDouble = new DelegateOutArgument<double> ();
			OutArgument<string> outWrongType = delOutDouble;
			Assert.IsInstanceOfType (typeof (DelegateArgumentReference<string>), outWrongType.Expression);
			Assert.AreSame(delOutDouble, ((DelegateArgumentReference<string>) outWrongType.Expression).DelegateArgument);

			//wrong param direction
			var delInFloat = new DelegateInArgument<float> ();
			OutArgument<float> outWrongDir = delInFloat;
			Assert.IsInstanceOfType (typeof (DelegateArgumentReference<float>), outWrongDir.Expression);
			Assert.AreSame(delInFloat, ((DelegateArgumentReference<float>) outWrongDir.Expression).DelegateArgument);
		}
		#endregion
	}
}
