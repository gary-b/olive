using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities.Expressions {
	class VariableValueT_Test : WFTestHelper {

		#region Ctors
		[Test]
		public void Ctor ()
		{
			var vv = new VariableValue<string> ();
			Assert.IsNull (vv.Variable);
		}
		[Test]
		public void Ctor_Variable ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vv = new VariableValue<string> (vStr);
			Assert.AreSame (vStr, vv.Variable);

			// .NET does not throw error when type param of Variable clashes with that if VV
			// FIXME: might do during execution though
			var vInt = new Variable<int> ("aname", 42);
			var vv2 = new VariableValue<string> (vInt);
			Assert.AreSame (vInt, vv2.Variable);
			// .NET doesnt throw error on null param
			var vv3 = new  VariableValue<string> (null);
		}
		#endregion

		#region Properties
		[Test]
		public void Variable ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vInt = new Variable<int> ("aname", 42);
			var vv = new VariableValue<string> (vStr);
			Assert.AreSame (vStr, vv.Variable);

			vv.Variable = vInt;
			Assert.AreSame (vInt, vv.Variable);

			vv.Variable = null;
			Assert.IsNull (vv.Variable);
		}
		#endregion

		#region Methods
		[Test]
		[Ignore ("ToString fails on generics issue")]
		public void ToStringTest ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vInt = new Variable<int> ("intName", 42);
			var vv = new VariableValue<string> (vStr);
			Assert.AreEqual ("aname", vv.ToString ());

			vv.Variable = vInt;
			Assert.AreEqual ("intName", vv.ToString ());

			var vv2 = new VariableValue<string> ();
			Assert.AreEqual (": VariableValue<String>", vv2.ToString ());

			vv2.Variable = vInt;
			Assert.AreEqual ("intName", vv2.ToString ());

			var vStrEmptyName = new Variable<string> ("", "avalue");
			var vv3 = new VariableValue<string> (vStrEmptyName);
			Assert.AreEqual (": VariableValue<String>", vv3.ToString ());

			var vStrNullName = new Variable<string> (null, "avalue");
			Assert.IsNull (vStrNullName.Name);
			var vv4 = new VariableValue<string> (vStrNullName);
			Assert.AreEqual (": VariableValue<String>", vv4.ToString ());
		}
		//FIXME: convoluted test
		[Test]
		public void Execute () //protected
		{
			var impVariable = new Variable<string> ("", "HelloImplementation");
			var impWrite = new WriteLine {
				Text = new InArgument<string> (impVariable) // InArg's Expression will be set to VariableValue
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (impVariable);
				metadata.AddImplementationChild (impWrite);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (impWrite);
			};
			var wf = new NativeActivityRunner (cacheMetadata, execute);
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		#endregion
	}
}
