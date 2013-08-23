using NUnit.Framework;
using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class NativeActivityMetadataTest : WFTestHelper {
		[Test]
		public void AddChild ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (null); // .NET does not raise error
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AddChild_Dupe ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The activity 'WriteLine' cannot be referenced by activity 'NativeActivityRunner' because the 
			//latter is not in another activity's implementation.  An activity can only be referenced by the implementation of an activity
			//which specifies that activity as a child or import.  Activity 'WriteLine' is declared by activity 'NativeActivityRunner'.
			var writeLine = new WriteLine { Text = "Hello\nWorld" };

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (writeLine);
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AddImplementationChild ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (null); // .NET does not raise error
				metadata.AddImplementationChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AddImplementationChild_Dupe ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AddDelegate ()
		{
			var writeAction = new ActivityAction {
				Handler = new WriteLine { Text = "Hello\nWorld" }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (null); // .NET does not raise error
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AddDelegate_Dupe ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'NativeActivityRunner': The activity delegate with Handler 'WriteLine' cannot be referenced by activity 'NativeActivityRunner' 
			//because the latter is not in another activity's implementation.  An activity delegate can only be referenced by the implementation
			//of an activity which specifies that activity delegate as a child or import.  The activity delegate with Handler 'WriteLine' is 
			//declared by activity 'NativeActivityRunner'.
			var writeAction = new ActivityAction {
				Handler = new WriteLine { Text = "Hello\nWorld" }
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (writeAction);
				metadata.AddDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AddImplementationDelegate ()
		{
			var writeAction = new ActivityAction {
				Handler = new WriteLine { Text = "Hello\nWorld" }
			};
			
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (null); // .NET does not raise error
				metadata.AddImplementationDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void AddImplementationDelegate_Dupe ()
		{
			var writeAction = new ActivityAction {
				Handler = new WriteLine { Text = "Hello\nWorld" }
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationDelegate (writeAction);
				metadata.AddImplementationDelegate (writeAction);
			}, (context) => {
				context.ScheduleAction (writeAction);
			});
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		/*TODO
		[Test]
		public void OperatorEqual ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void OperatorNotEqual ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Environment_Get ()
		{ 
			throw new NotImplementedException (); 
		}
		[Test]
		public void HasViolations_Get ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void AddArgument ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void AddDefaultExtensionProvider_T ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void AddImplementationVariable ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void AddImportedChild ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void AddImportedDelgate ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void AddValidationError ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void AddValidationError ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void AddVariable ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Bind ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Equals (object obj)
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void GetArgumentsWithReflection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void GetChildrenWithReflection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void GetDelegatesWithReflection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void GetHashCode ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void GetVariablesWithReflection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void RequireExtension_T ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void RequireExtension ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetArgumentsCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetChildrenCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetDelegatesCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetImplementationChildrenCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetImplementationDelegatesCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetImplementationVariablesCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetImportedChildrenCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetImportedDelegatesCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetValidationErrorsCollection ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void SetVariablesCollection ()
		{
			throw new NotImplementedException ();
		}
		*/

	}
}

