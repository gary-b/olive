using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.IO;
using System.Collections.ObjectModel;
using System.Activities.Statements;

namespace Tests.System.Activities {
	class CodeActivityTResultTest {
		class CodeActivityTMock : CodeActivity<string> {
			new public Func<Activity> Implementation	{
				get { return base.Implementation; }
				set { base.Implementation = value; }
			}

			protected override string Execute (CodeActivityContext context)
			{
				throw new NotImplementedException ();
			}
		}

		#region Properties
		[Test]
		public void Implementation_Get ()
		{
			var codeActivity = new CodeActivityTMock ();
			Assert.IsNull (codeActivity.Implementation);
		}
		[Test, ExpectedException (typeof (NotSupportedException))]
		public void Implementation_Set ()
		{
			var codeActivity = new CodeActivityTMock ();
			Assert.IsNull (codeActivity.Implementation);
			codeActivity.Implementation = () => new WriteLine ();
		}
		
		class CodeTMetaMock : CodeActivity<string> {
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				Collection<RuntimeArgument> runtimeArgs = metadata.GetArgumentsWithReflection ();
				Assert.AreEqual (1, runtimeArgs.Count);
				
				// standard Return argument
				RuntimeArgument argOutResult = runtimeArgs [0];
				Assert.AreEqual (ArgumentDirection.Out, argOutResult.Direction);
				Assert.IsFalse (argOutResult.IsRequired);
				Assert.AreEqual ("Result", argOutResult.Name);
				Assert.AreEqual (0, argOutResult.OverloadGroupNames.Count);
				Assert.AreEqual (typeof (string), argOutResult.Type);
			}
			
			protected override string Execute (CodeActivityContext context)
			{
				return "Hello\nWorld";
			}
		}
		
		[Test]
		public void Result ()
		{
			var codeMeta = new CodeTMetaMock ();
			WorkflowInvoker.Invoke (codeMeta);
		}
		#endregion

		#region Methods

		class CodeTExecMock : CodeActivity<string> {
			protected override string Execute (CodeActivityContext context)
			{
				return "Execute\nMock";
			}
		}

		[Test]
		public void Execute ()
		{
			var codeMeta = new CodeTExecMock ();
			string result = WorkflowInvoker.Invoke (codeMeta);
			Assert.AreEqual ("Execute\nMock", result);
		}

		#endregion

	}
}
