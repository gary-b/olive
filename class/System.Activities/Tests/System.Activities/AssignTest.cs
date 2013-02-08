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

		[Test]
		public void To_Empty ()
		{
			// doesnt Raise Error
			var AssignNewValue = new Assign {
				To = new OutArgument<string> (),
				Value = new InArgument<string> ("NewValue")
			};
			WorkflowInvoker.Invoke (AssignNewValue);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Value_EmptyEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			// Value for a required activity argument 'Value' was not supplied.
			var ImpVar = new Variable<string> ("", "DefaultVar"); 
			var AssignNewValue = new Assign {
				To = new OutArgument<string> (ImpVar),
				Value = new InArgument<string> ()
			};

			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddImplementationVariable (ImpVar);
				metadata.AddImplementationChild (AssignNewValue);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				Assert.AreEqual ("DefaultVar", ImpVar.Get (context));
				context.ScheduleActivity (AssignNewValue);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (NullReferenceException))]
		public void To_NullEx ()
		{
			var AssignNewValue = new Assign {
				Value = new InArgument<string> ("NewValue")
			};
			Assert.IsNull (AssignNewValue.To);
			WorkflowInvoker.Invoke (AssignNewValue);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("Validation")]
		public void Value_RootNullEx ()
		{
			//System.ArgumentException : The root activity's argument settings are incorrect.  Either fix the workflow definition or supply input values to fix these errors:
			//'Assign': Value for a required activity argument 'Value' was not supplied.
			//Parameter name: program

			var AssignNewValue = new Assign {
				To = new OutArgument<string> (),
			};
			Assert.IsNull (AssignNewValue.Value);
			WorkflowInvoker.Invoke (AssignNewValue);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Value_NotRootNullEx ()
		{
			//Value_NotRootNullEx and Value_RootNullEx show that different exceptions are raised depending
			// on whether the invalid argument is supplied on the root / non root activity

			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			// Value for a required activity argument 'Value' was not supplied.
			var ImpVar = new Variable<string> ("", "DefaultVar"); 
			var AssignNewValue = new Assign {
				To = new OutArgument<string> (ImpVar),
			};
			Assert.IsNull (AssignNewValue.Value);

			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddImplementationVariable (ImpVar);
				metadata.AddImplementationChild (AssignNewValue);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				Assert.AreEqual ("DefaultVar", ImpVar.Get (context));
				context.ScheduleActivity (AssignNewValue);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}
	}
	[TestFixture]
	class AssignT_Test {
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
			var AssignNewValue = new Assign<string> {
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

		[Test]
		public void To_Empty ()
		{
			// doesnt Raise Error
			var AssignNewValue = new Assign<string> {
				To = new OutArgument<string> (),
				Value = new InArgument<string> ("NewValue")
			};
			WorkflowInvoker.Invoke (AssignNewValue);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Value_EmptyEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			// Value for a required activity argument 'Value' was not supplied.
			var ImpVar = new Variable<string> ("", "DefaultVar"); 
			var AssignNewValue = new Assign<string> {
				To = new OutArgument<string> (ImpVar),
				Value = new InArgument<string> ()
			};
			
			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddImplementationVariable (ImpVar);
				metadata.AddImplementationChild (AssignNewValue);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				Assert.AreEqual ("DefaultVar", ImpVar.Get (context));
				context.ScheduleActivity (AssignNewValue);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void To_Null ()
		{
			// does not cause Exception on generic implementation
			var AssignNewValue = new Assign<string> {
				Value = new InArgument<string> ("NewValue")
			};
			Assert.IsNull (AssignNewValue.To);
			WorkflowInvoker.Invoke (AssignNewValue);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		[Ignore ("Validation")]
		public void Value_RootNullEx ()
		{
			//System.ArgumentException : The root activity's argument settings are incorrect.  Either fix the workflow definition or supply input values to fix these errors:
			//'Assign': Value for a required activity argument 'Value' was not supplied.
			//Parameter name: program
			
			var AssignNewValue = new Assign<string> {
				To = new OutArgument<string> (),
			};
			Assert.IsNull (AssignNewValue.Value);
			WorkflowInvoker.Invoke (AssignNewValue);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Value_NotRootNullEx ()
		{
			//Value_NotRootNullEx and Value_RootNullEx show that different exceptions are raised depending
			// on whether the invalid argument is supplied on the root / non root activity
			
			// System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			// 'NativeRunnerMock': The private implementation of activity '1: NativeRunnerMock' has the following validation error:   
			// Value for a required activity argument 'Value' was not supplied.
			var ImpVar = new Variable<string> ("", "DefaultVar"); 
			var AssignNewValue = new Assign<string> {
				To = new OutArgument<string> (ImpVar),
			};
			Assert.IsNull (AssignNewValue.Value);
			Action<NativeActivityMetadata> cacheMetadataParent = (metadata) => {
				metadata.AddImplementationVariable (ImpVar);
				metadata.AddImplementationChild (AssignNewValue);
			};
			
			Action<NativeActivityContext> executeParent = (context) => {
				Assert.AreEqual ("DefaultVar", ImpVar.Get (context));
				context.ScheduleActivity (AssignNewValue);
			};
			
			var wf = new NativeRunnerMock (cacheMetadataParent, executeParent);
			WorkflowInvoker.Invoke (wf);
		}
	}
}
