using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.IO;
using System.Collections.ObjectModel;

namespace Tests.System.Activities {
	// testing CodeActivityMetadata outside CacheMetadata causes exceptions on method calls, so tests are within
	class CodeActivityMetadataTest {
		//helper method
		void Run (Action<CodeActivityMetadata> cacheMetaData, Action<CodeActivityContext> execute)
		{
			var codeMeta = new CodeActivityRunner (cacheMetaData, execute);
			WorkflowInvoker.Invoke (codeMeta);
		}

		// requires properties so not using CodeActivityRunner
		class CodeMetaGetArgMock : CodeActivity {
			public InArgument<string> InString1 { get; set; }
			public OutArgument<int> OutInt1 { get; set; }
			public InOutArgument<TextWriter> InOutTw1 { get; set; }
			public Argument ArgumentDontInitialiseMe { get; set; } // be ignored
			public Argument ArgumentLessSpecific { get; set; } // still be ignored even if initialized
			public CodeMetaGetArgMock ()
			{
				ArgumentLessSpecific = new InArgument<string> ();
			}
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				// test arguments
				Collection<RuntimeArgument> runtimeArgs = metadata.GetArgumentsWithReflection ();
				Assert.AreEqual (3, runtimeArgs.Count);

				RuntimeArgument argInString1 = runtimeArgs [0];
				Assert.AreEqual (ArgumentDirection.In, argInString1.Direction);
				Assert.IsFalse (argInString1.IsRequired);
				Assert.AreEqual ("InString1", argInString1.Name);
				Assert.AreEqual (0, argInString1.OverloadGroupNames.Count);
				Assert.AreEqual (typeof (string), argInString1.Type);

				RuntimeArgument argOutInt1 = runtimeArgs [1];
				Assert.AreEqual (ArgumentDirection.Out, argOutInt1.Direction);
				Assert.IsFalse (argOutInt1.IsRequired);
				Assert.AreEqual ("OutInt1", argOutInt1.Name);
				Assert.AreEqual (0, argOutInt1.OverloadGroupNames.Count);
				Assert.AreEqual (typeof (int), argOutInt1.Type);

				RuntimeArgument argInOutTw1 = runtimeArgs [2];
				Assert.AreEqual (ArgumentDirection.InOut, argInOutTw1.Direction);
				Assert.IsFalse (argInOutTw1.IsRequired);
				Assert.AreEqual ("InOutTw1", argInOutTw1.Name);
				Assert.AreEqual (0, argInOutTw1.OverloadGroupNames.Count);
				Assert.AreEqual (typeof (TextWriter), argInOutTw1.Type);
				// FIXME: test ICollection of Arguments
			}
			protected override void Execute (CodeActivityContext context)
			{
			}
		}

		[Test]
		[Ignore ("GetArgumentsWithReflection")]
		public void GetArgumentsWithReflection ()
		{
			var codeMeta = new CodeMetaGetArgMock ();
			WorkflowInvoker.Invoke (codeMeta);
		}

		[Test]
		public void AddArgument ()
		{
			var rtArg = new RuntimeArgument ("name", typeof (string), ArgumentDirection.In);

			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (null); // no exception on passing null
				metadata.AddArgument (rtArg);
			};
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue (rtArg, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue (rtArg));
			};
			Run (metadataAction, execute);
		}
		// requires properties so not using CodeActivityRunner
		class AutoInitBindMock : CodeActivity {
			public InArgument<string> InString1 { get; set; }
			public OutArgument<int> OutInt1 { get; set; }
			public InOutArgument<string> InOutString1 { get; set; }

			public InArgument<string> InString2 { get; set; }
			public InArgument<string> InString3 { get; set; }
			public InArgument<string> InString4 { get; set; }
			public InArgument<string> InString5 { get; set; }

			public AutoInitBindMock ()
			{
			}
			
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				// When an Argument is added with a name that matches the name, type and direction of a Argument property
				// then that property is initialized with a default value and seeminly automatically bound

				var rtInString1 = new RuntimeArgument ("InString1", typeof (string), ArgumentDirection.In);
				var rtOutInt1 = new RuntimeArgument ("OutInt1", typeof (int), ArgumentDirection.Out);
				var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);

				Assert.IsNull (InString1);
				metadata.AddArgument (rtInString1);
				Assert.IsNotNull (InString1);

				Assert.IsNull (OutInt1);
				metadata.AddArgument (rtOutInt1);
				Assert.IsNotNull (OutInt1);

				Assert.IsNull (InOutString1);
				metadata.AddArgument (rtInOutString1);
				Assert.IsNotNull (InOutString1);

				// metadata.Bind (null, rtInString1); // in .NET binding to null causes exception after method completes

				var rtInString2_WrongName = new RuntimeArgument ("inString2", typeof (string), ArgumentDirection.In);
				var rtInString3_WrongType = new RuntimeArgument ("InString3", typeof (int), ArgumentDirection.In);
				var rtInString4_WrongDir = new RuntimeArgument ("InString4", typeof (int), ArgumentDirection.InOut);
				var rtInString5_WrongDir = new RuntimeArgument ("InString5", typeof (int), ArgumentDirection.Out);
				metadata.AddArgument (rtInString2_WrongName);
				metadata.AddArgument (rtInString3_WrongType);
				metadata.AddArgument (rtInString4_WrongDir);
				metadata.AddArgument (rtInString5_WrongDir);
				// metadata.Bind (null, rtInString2_WrongName); // in .NET this would also cause error after method completes
			}
			
			protected override void Execute (CodeActivityContext context)
			{
				// check Argument properties where no rt arg with matching name exists havnt been initialised
				Assert.IsNull (InString2);
				Assert.IsNull (InString3);
				Assert.IsNull (InString4);
				Assert.IsNull (InString5);
				// check initialised arguments are useable
				Assert.AreEqual (null, InString1.Get (context));
				Assert.AreEqual (0, OutInt1.Get (context));
				Assert.AreEqual (null, InOutString1.Get (context));
			}
		}

		[Test]
		[Ignore ("AutoInitAndBinds")]
		public void AddArgument_AutoInitAndBinds ()
		{
			var wf = new AutoInitBindMock ();
			WorkflowInvoker.Invoke (wf);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void AddArgument_DupeNamesEx ()
		{
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				var rtArg = new RuntimeArgument ("name", typeof (string), ArgumentDirection.In);
				var rtArg2 = new RuntimeArgument ("name", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg);
				metadata.AddArgument (rtArg2); //raises exception if same name used twice: InvalidWorkflowException
			};
			Run (metadataAction, null);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void AddArgument_DupeArgsEx ()
		{
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				var rtArg = new RuntimeArgument ("name", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg); 
				metadata.AddArgument (rtArg); // raises exception if same arg added twice: InvalidWorkflowException
			};
			Run (metadataAction, null);
		}

		[Test]
		public void Bind ()
		{
			var arg = new InArgument<string> ();
			var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddArgument (rtArg);
				metadata.Bind (arg, rtArg);
				// no error on .NET if bound twice to same thing
				metadata.Bind (arg, rtArg);
			};
			Action<CodeActivityContext> execute = (context) => {
				arg.Set (context, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue (rtArg));
			};
			Run (metadataAction, execute);
		}

		[Test]
		public void Bind_RuntimeArgNotDeclared ()
		{
			// note calling Bind with a rtArg that hasnt been passed to AddArgument doesnt raise error but rtArg cant be used later
			var arg = new InArgument<string> ();
			var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.Bind (arg, rtArg);
			};
			Run (metadataAction, null);
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Bind_RuntimeArgNotDeclaredEx ()
		{
			//The argument of type 'System.String' cannot be used.  Make sure that it is declared on an activity.
			var arg = new InArgument<string> ();
			var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.Bind (arg, rtArg);
			};
			Action<CodeActivityContext> execute = (context) => {
				arg.Set (context, "Hello\nWorld"); // exception raised here
			};
			Run (metadataAction, execute);
		}

		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Bind_NullArgumentEx ()
		{
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var arg = new InArgument<string> ();
				metadata.Bind (arg, null); // this should raise exception
			};
			Run (metadataAction, null);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Bind_2RTArgsTo1ArgEx ()
		{
			/*
			The following errors were encountered while processing the workflow tree:
			'CodeActMetaRunner': RuntimeArgument 'arg1' refers to an Argument which in turn is bound to RuntimeArgument 
			named 'arg2'. Please ensure that the Argument object is not bound to more than one RuntimeArgument object or 
			shared by more than one public Argument property.
			*/
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var rtArg1 = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
				var rtArg2 = new RuntimeArgument ("arg2", typeof (string), ArgumentDirection.In);
				var arg = new InArgument<string> ();
				metadata.AddArgument (rtArg1);
				metadata.AddArgument (rtArg2);
				metadata.Bind (arg, rtArg1);
				metadata.Bind (arg, rtArg2);
			};
			Run (metadataAction, null);
		}

		[Test]
		public void Bind_1RTArgTo2Args ()
		{
			// no error binding 1 runtimeargument to 2 args
			var rtArg1 = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			var arg = new InArgument<string> ();
			var arg2 = new InArgument<string> ();
			Action<CodeActivityMetadata> metadataAction = metadata => {
				metadata.AddArgument (rtArg1);
				metadata.Bind (arg, rtArg1);
				metadata.Bind (arg2, rtArg1);
			};
			Action<CodeActivityContext> execute = (context) => {
				arg.Set (context, "SetByArg");
				Assert.AreEqual ("SetByArg", context.GetValue (rtArg1));
				arg2.Set (context, "SetByArg2");
				Assert.AreEqual ("SetByArg2", context.GetValue (rtArg1));
			};
			Run (metadataAction, execute);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Bind_TypeMismatchEx ()
		{
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
				var arg = new InArgument<int> ();
				metadata.Bind (arg, rtArg);
			};
			Run (metadataAction, null);
		}

		[Test]
		[Ignore ("SetArgumentsCollection")]
		public void SetArgumentsCollection ()
		{
			var rtArg = new RuntimeArgument ("NAMEarg0", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var args = new Collection<RuntimeArgument> {
					new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In),
					new RuntimeArgument ("arg2", typeof (string), ArgumentDirection.In),
					new RuntimeArgument ("arg3", typeof (string), ArgumentDirection.In)
				};

				metadata.SetArgumentsCollection (null); // passing null doesnt raise error on .NET
				//metadata.AddArgument (rtArg);
				metadata.SetArgumentsCollection (args);
				metadata.AddArgument (rtArg);
			};
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue (rtArg, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue (rtArg));
			};
			Run (metadataAction, execute);
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void SetArgumentsCollection_ReplacesExistingEx ()
		{
			// The argument 'NAMEarg0' cannot be used. Make sure it is declared on an activity
			var rtArg = new RuntimeArgument ("NAMEarg0", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var args = new Collection<RuntimeArgument> {
					new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In)
				};
				metadata.AddArgument (rtArg);
				metadata.SetArgumentsCollection (args);
			};
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue (rtArg, "Hello\nWorld");
				Assert.AreEqual ("Hello\nWorld", context.GetValue (rtArg));
			};
			Run (metadataAction, execute);
		}

		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void SetArgumentsCollection_DupeNameInCollectionEx ()
		{
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var args = new Collection<RuntimeArgument> {
					new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In), // dupe name
					new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In), // dupe name
					new RuntimeArgument ("arg3", typeof (string), ArgumentDirection.In)
				};

				metadata.SetArgumentsCollection (args);
			};
			Run (metadataAction, null);
		}

		//FIXME: type mismatch errors

		/* TODO test
		   operatorEquals
		 * operatorNotEqual
		 * Environment
		 * HasViolations
		 * AddDefaultExtensionProvider
		 * AddValidationError_String
		 * AddValidationError_Error
		 * Equals?
		 * GetHashCode?
		 * RequireExtensionT
		 * RequireExtension_T
		 * SetValidationErrorsCollection
		 */ 
	}

	class CodeActivityRunner : CodeActivity {
		Action<CodeActivityMetadata> cacheMetaDataAction;
		Action<CodeActivityContext> executeAction;
		public CodeActivityRunner (Action<CodeActivityMetadata> action, Action<CodeActivityContext> execute)
		{
			cacheMetaDataAction = action;
			executeAction = execute;
		}
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			if (cacheMetaDataAction != null)
				cacheMetaDataAction (metadata);
		}
		protected override void Execute (CodeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context);
		}
	}
}
