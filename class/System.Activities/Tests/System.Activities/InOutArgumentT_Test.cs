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
	class InOutArgumentT_Test : WFTestHelper {

		#region Ctors
		[Test]
		public void Ctor ()
		{
			var ioArg = new InOutArgument<string> ();
			Assert.AreEqual (ArgumentDirection.InOut, ioArg.Direction);
			Assert.IsNull (ioArg.Expression);
			Assert.AreEqual (typeof (string), ioArg.ArgumentType);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Ctor_ActivityLocationT ()
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
		public void Ctor_VariableT ()
		{
			var vStr = new Variable<string> ();
			var ioStr = new InOutArgument<string> (vStr);
			Assert.AreEqual (ArgumentDirection.InOut, ioStr.Direction);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), ioStr.Expression);
			Assert.AreSame (vStr, ((VariableReference<string>) ioStr.Expression).Variable);

			// .NET doesnt raise error when Variable type param doesnt match args
			// FIXME: might raise error when WF executed
			var vInt = new Variable<int> ();
			var ioStr2 = new InOutArgument<string> (vInt);
			Assert.AreSame (vInt, ((VariableReference<string>) ioStr2.Expression).Variable);
		}
		[Test]
		public void Ctor_Variable ()
		{
			// what is the point of Variable and Variable<T> ctors, InArg.. nor OutArg.. have both

			// .NET doesnt raise error when Variable type param doesnt match args
			var vInt = new Variable<int> ();
			var ioStr = new InOutArgument<string> ((Variable) vInt);
			Assert.AreEqual (ArgumentDirection.InOut, ioStr.Direction);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), ioStr.Expression);
			Assert.AreEqual (typeof (string), ioStr.ArgumentType);
			Assert.AreSame (vInt, ((VariableReference<string>) ioStr.Expression).Variable);
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
			 * On InOutArgument:
			 * 		public T InArgument.Get (ActivityContext)
			 */
			//FIXME: no argument validation tests
			var varStr = new Variable<string> ("", "DefaultValue");
			var iOStr = new InOutArgument<string> (varStr);

			var tester = new NativeActivityRunner ((metadata) => {
				var rtIOStr = new RuntimeArgument ("iOStr", typeof (string), ArgumentDirection.InOut);
				metadata.AddArgument (rtIOStr);
				metadata.Bind (iOStr, rtIOStr);
			}, (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (iOStr)); // check default value is present
				Assert.AreEqual ("DefaultValue", iOStr.Get (context));// check Get returns same
				
				Location locIOStr = iOStr.GetLocation (context);
				Assert.AreEqual (typeof (string), locIOStr.LocationType); 
				Assert.AreEqual ("DefaultValue", locIOStr.Value);
				
				iOStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (iOStr)); // check Set affects value as its seen in this scope
				Assert.AreEqual ("SetT", ((Argument)iOStr).Get (context)); // check Get returns new value
				
				iOStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (iOStr)); // check Set
				Assert.AreEqual ("SetO", iOStr.Get<string> (context)); // check Get returns new value
				
				Assert.AreEqual ("SetO", locIOStr.Value); // check location has been updated
				
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
			var iOStr = new InOutArgument<string> (varStr);

			var tester = new NativeActivityRunner ((metadata) => {
				var rtIOStr = new RuntimeArgument ("iOStr", typeof (string), ArgumentDirection.InOut);
				metadata.AddArgument (rtIOStr);
				metadata.Bind (iOStr, rtIOStr);
			}, (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (iOStr)); // default value is available
				iOStr.Set (context, (string) "SetT");
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
			var vStr = new Variable<string> ("vStr", "DefaultValue");
			var ioArg = new InOutArgument<string> (vStr);
			Assert.AreEqual (ioArg.GetType ().ToString (), ioArg.ToString ());
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
		public void Implicit_VariableT ()
		{
			var varStr = new Variable<string> ("name", "value");
			InOutArgument<string> inOutStr = varStr;
			Assert.AreEqual (ArgumentDirection.InOut, inOutStr.Direction);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), inOutStr.Expression);
			Assert.AreSame(varStr, ((VariableReference<string>) inOutStr.Expression).Variable);

			//doesnt allow variable with wrong type to be passed
		}
		#endregion
	}
}
