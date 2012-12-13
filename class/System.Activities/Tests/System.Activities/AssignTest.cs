using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.IO;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	class AssignTest {
		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}

		[Test]
		public void Execute ()
		{
			var ImpVar = new Variable<string> ("", "DefaultVar"); 
			var AssignNewValue = new Assign {
				To = new OutArgument<string> (ImpVar),
				Value = new InArgument<string> ("NewValue")
			};
			var Write = new WriteLine {
				Text = ImpVar
			};

			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddImplementationVariable (ImpVar);
				metadata.AddImplementationChild (AssignNewValue);
				metadata.AddImplementationChild (Write);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				Assert.AreEqual ("DefaultVar", ImpVar.Get (context));
				context.ScheduleActivity (Write);
				context.ScheduleActivity (AssignNewValue);
				context.ScheduleActivity (Write);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			RunAndCompare (wf, String.Format ("DefaultVar{0}NewValue{0}", Environment.NewLine));
		}

	}
}
