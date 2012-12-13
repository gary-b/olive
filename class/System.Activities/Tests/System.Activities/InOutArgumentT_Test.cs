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
	class InOutArgumentT_Test {
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
			var ioArg = new InOutArgument<string> ();
			Assert.IsNull (ioArg.Expression);
			Assert.AreEqual (typeof (string), ioArg.ArgumentType);
		}

		[Test]
		public void Ctor_ActivityLocationT ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void Ctor_Expression  ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void Ctor_VariableT ()
		{
			var vStr = new Variable<string> ();
			var ioStr = new InOutArgument<string> (vStr);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), ioStr.Expression);
			Assert.AreSame (vStr, ((VariableReference<string>) ioStr.Expression).Variable);
		}

		[Test]
		public void Ctor_Variable ()
		{
			// what is the point of Variable and Variable<T> ctors?

			// .NET doesnt raise error when Variable type param doesnt match args
			var vInt = new Variable<int> ();
			var ioStr2 = new InOutArgument<string> (vInt);
			Assert.IsInstanceOfType (typeof (VariableReference<string>), ioStr2.Expression);
			Assert.AreEqual (typeof (string), ioStr2.ArgumentType);
			Assert.AreSame (vInt, ((VariableReference<string>) ioStr2.Expression).Variable);
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
		public void FromExpression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void FromVariable ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void Set_Get_GetT_GetLocation_ForVariable ()
		{
			//FIXME: no argument validation tests
			var varStr = new Variable<string> ("", "DefaultValue");
			var IOStr = new InOutArgument<string> (varStr);
			
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				var rtIOStr = new RuntimeArgument ("IOStr", typeof (string), ArgumentDirection.InOut);
				metadata.AddArgument (rtIOStr);
				metadata.Bind (IOStr, rtIOStr);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (IOStr)); // check default value is present
				Assert.AreEqual ("DefaultValue", IOStr.Get (context));// check Get returns same

				Location LocIOStr = IOStr.GetLocation (context);
				Assert.AreEqual (typeof (string), LocIOStr.LocationType); 
				Assert.AreEqual ("DefaultValue", LocIOStr.Value);

				IOStr.Set (context, (string) "SetT");
				Assert.AreEqual ("SetT", context.GetValue (IOStr)); // check Set affects value as its seen in this scope
				Assert.AreEqual ("SetT", IOStr.Get (context)); // check Get returns new value

				IOStr.Set (context, (object) "SetO");
				Assert.AreEqual ("SetO", context.GetValue (IOStr)); // check Set
				Assert.AreEqual ("SetO", IOStr.Get (context)); // check Get returns new value
				
				Assert.AreEqual ("SetO", LocIOStr.Value); // check location has been updated

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
		public void SetDoesAffectVariable ()
		{
			var varStr = new Variable<string> ("", "DefaultValue");
			var IOStr = new InOutArgument<string> (varStr);
			
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				var rtIOStr = new RuntimeArgument ("IOStr", typeof (string), ArgumentDirection.InOut);
				metadata.AddArgument (rtIOStr);
				metadata.Bind (IOStr, rtIOStr);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				Assert.AreEqual ("DefaultValue", context.GetValue (IOStr)); // default value is available
				IOStr.Set (context, (string) "SetT");
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
			RunAndCompare (wf, String.Format ("DefaultValue{0}SetT{0}", Environment.NewLine));
		}

		[Test]
		public void ToStringTest ()
		{
			var ioArg = new InOutArgument<string> ();
			Assert.AreEqual (ioArg.GetType ().ToString (), ioArg.ToString ());
		}
		#endregion
		#region operators
		[Test]
		public void Implicit_ActivityLocationT ()
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
