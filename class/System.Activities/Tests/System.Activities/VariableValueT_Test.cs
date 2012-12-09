using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	class VariableValueT_Test {
		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}

		#region Ctors
		[Test]
		public void VariableValue_Ctor ()
		{
			var vv = new VariableValue<string> ();
			Assert.IsNull (vv.Variable);
		}
		[Test]
		public void VariableValueVariable_Ctor ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vv = new VariableValue<string> (vStr);
			Assert.AreSame (vStr, vv.Variable);

			// .NET does not throw error when type param of Variable clashes with that if VV
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
		public void ToStringTest ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vInt = new Variable<int> ("intName", 42);
			var vv = new VariableValue<string> (vStr);
			Assert.AreEqual ("aname", vv.ToString ());

			vv.Variable = vInt;
			Assert.AreEqual ("intName", vv.ToString ());
		}
		[Test]
		public void Execute () //protected
		{
			var ImplementationVariable = new Variable<string> ("", "HelloImplementation");
			var ImplementationWrite = new WriteLine {
				Text = ImplementationVariable // this evaluates to InArg with Expression set to VariableValue
			};
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationVariable (ImplementationVariable);
				metadata.AddImplementationChild (ImplementationWrite);
			};
			
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (ImplementationWrite);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			RunAndCompare (wf, "HelloImplementation" + Environment.NewLine);
		}
		#endregion
	}
}
