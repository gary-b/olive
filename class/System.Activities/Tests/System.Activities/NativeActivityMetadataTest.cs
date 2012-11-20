using NUnit.Framework;
using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;

namespace Tests.System.Activities {
	[TestFixture()]
	public class NativeActivityMetadataTest	{
		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}

		[Test]
		public void AddChild ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddChild (null); // .NET does not raise error
				metadata.AddChild (writeLine);
			};

			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (writeLine);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		[Test]
		public void AddImplementationChild ()
		{
			var writeLine = new WriteLine { Text = "Hello\nWorld" };
			Action<NativeActivityMetadata> cacheMetadata = (metadata) => {
				metadata.AddImplementationChild (null); // .NET does not raise error
				metadata.AddImplementationChild (writeLine);
			};
			// FIXME: move to NativeActivityContext tests?
			Action<NativeActivityContext> execute = (context) => {
				context.ScheduleActivity (writeLine);
			};
			var wf = new NativeRunnerMock (cacheMetadata, execute);
			RunAndCompare (wf, "Hello\nWorld" + Environment.NewLine);
		}
		/*
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
		public void AddDelgate ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void AddImplementationDelgate ()
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
		// Temp tryout
		public class VariableTryoutMock : NativeActivity
		{
			public InArgument<string> ArgText { get; set; }
			Variable<string> VarText { get; set; }
			WriteLine writeLine;

			public VariableTryoutMock ()
			{
				VarText = new Variable<string> ();
				writeLine = new WriteLine {
					Text = new InArgument<string>(VarText),
				};
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				var rtText = new RuntimeArgument ("Text2", typeof (string), ArgumentDirection.In);
				if (ArgText == null)
					ArgText = new InArgument<string> ();
				metadata.AddArgument (rtText);
				metadata.Bind (ArgText, rtText);
				metadata.AddImplementationVariable (VarText);
				metadata.AddImplementationChild (writeLine);
			}
			protected override void Execute (NativeActivityContext context)
			{
				string text = context.GetValue (ArgText);
				context.SetValue (VarText, text);
				context.ScheduleActivity (writeLine);
			}
		}

		[Test()]
		public void TempTryout ()
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			var wf = new VariableTryoutMock {
				ArgText = new InArgument<string> ("Hello\nWorld")
			};
			WorkflowInvoker.Invoke (wf);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, sw.ToString ());
		}
	}

	public class NativeRunnerMock : NativeActivity
	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext> executeAction;
		public NativeRunnerMock (Action<NativeActivityMetadata> cacheMetadata, Action<NativeActivityContext> execute)
		{
			if (cacheMetadata == null)
				throw new NotImplementedException (); // ArgNullEx or IOE might fool a test
			
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			cacheMetadataAction (metadata);
		}
		protected override void Execute (NativeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context);
		}
	}
}

