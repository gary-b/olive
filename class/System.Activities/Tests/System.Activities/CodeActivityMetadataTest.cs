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
	// testing CodeActivityMetadata outside CacheMetadata causes exceptions on method calls, so tests are within
	class CodeActivityMetadataTest {
		//helper method
		void Run (Action<CodeActivityMetadata> cacheMetaData, Action<CodeActivityContext> execute)
		{
			var codeMeta = new CodeActivityRunner (cacheMetaData, execute);
			WorkflowInvoker.Invoke (codeMeta);
		}
		// requires properties so not using CodeActivityRunner
		class CodeMetaGetArgActivity : CodeActivity {
			public InArgument<string> InString1 { get; set; }
			public OutArgument<int> OutInt1 { get; set; }
			public InOutArgument<TextWriter> InOutTw1 { get; set; }
			public Argument ArgumentDontInitialiseMe { get; set; } // be ignored
			public Argument ArgumentLessSpecific { get; set; } // still be ignored even if initialized
			public CodeMetaGetArgActivity ()
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
			var codeMeta = new CodeMetaGetArgActivity ();
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
		class AutoInitBindActivity : CodeActivity {
			InArgument<string> inStringNoGetter;
			RuntimeArgument rtInString1, rtInStringPreInitialized1;

			public InArgument<string> InString1 { get; set; }
			public OutArgument<int> OutInt1 { get; set; }
			public InOutArgument<string> InOutString1 { get; set; }

			public InArgument<string> InString2 { get; set; }
			public InArgument<string> InString3 { get; set; }
			public InArgument<string> InString4 { get; set; }
			public InArgument<string> InString5 { get; set; }

			protected InArgument<string> InStringProtected { get; set; }
			private InArgument<string> InStringPrivate { get; set; }
			static InArgument<string> InStringStatic { get; set; }
			internal InArgument<string> InStringInternal { get; set; }
			protected internal InArgument<string> InStringProtectedInternal { get; set; }

			public Argument ArgumentString { get; set; }
			public InArgument ArgumentInString { get; set; }

			public InArgument<string> InStringPreInitialized1 { get; set; }

			public InArgument<string> IllBeDupeA { get; set; }
			public InArgument<string> IllBeDupeB { get; set; }

			public string [] Strings { get; set; }

			public InArgument<string> InStringNoGetter {
				set {
					inStringNoGetter = value;
				}
			}

			public AutoInitBindActivity ()
			{
				InStringPreInitialized1 = new InArgument<string> ("1");
			}
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				// When an Argument is added with a name that matches the name, type and direction of a Argument property
				// then that property is initialized with a default value and seeminly automatically bound

				rtInString1 = new RuntimeArgument ("InString1", typeof (string), ArgumentDirection.In);
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

				var rtInString2_WrongName = new RuntimeArgument ("inString2", typeof (string), ArgumentDirection.In);
				var rtInString3_WrongType = new RuntimeArgument ("InString3", typeof (int), ArgumentDirection.In);
				var rtInString4_WrongDir = new RuntimeArgument ("InString4", typeof (string), ArgumentDirection.InOut);
				var rtInString5_WrongDir = new RuntimeArgument ("InString5", typeof (string), ArgumentDirection.Out);
				metadata.AddArgument (rtInString2_WrongName);
				metadata.AddArgument (rtInString3_WrongType);
				metadata.AddArgument (rtInString4_WrongDir);
				metadata.AddArgument (rtInString5_WrongDir);

				var rtInStringProtected = new RuntimeArgument ("InStringProtected", typeof (string), ArgumentDirection.In);
				var rtInStringPrivate = new RuntimeArgument ("InStringPrivate", typeof (string), ArgumentDirection.In);
				var rtInStringStatic = new RuntimeArgument ("InStringStatic", typeof (string), ArgumentDirection.In);
				var rtInStringInternal = new RuntimeArgument ("InStringInternal", typeof (string), ArgumentDirection.In);
				var rtInStringProtectedInternal = new RuntimeArgument ("InStringProtectedInternal", typeof (string), ArgumentDirection.In);

				metadata.AddArgument (rtInStringProtected);
				metadata.AddArgument (rtInStringPrivate);
				metadata.AddArgument (rtInStringStatic);
				metadata.AddArgument (rtInStringInternal);
				metadata.AddArgument (rtInStringProtectedInternal);

				var rtArgumentString = new RuntimeArgument ("ArgumentString", typeof (string), ArgumentDirection.In);
				var rtArgumentInString = new RuntimeArgument ("ArgumentInString", typeof (string), ArgumentDirection.In);

				metadata.AddArgument (rtArgumentString);
				metadata.AddArgument (rtArgumentInString);

				rtInStringPreInitialized1 = new RuntimeArgument ("InStringPreInitialized1", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStringPreInitialized1);

				var rtIllBeDupe = new RuntimeArgument ("IllBeDupeA", typeof (string), ArgumentDirection.In);
				IllBeDupeB = new InArgument<string> ();
				metadata.AddArgument (rtIllBeDupe);
				metadata.Bind (IllBeDupeB, rtIllBeDupe);

				var rtInStringNoGetter = new RuntimeArgument ("InStringNoGetter", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtInStringNoGetter);
			}
			protected override void Execute (CodeActivityContext context)
			{
				// it seems if the properties are automatically set they are done so during the call to metadata.AddArgument
				// just checking here to prove the properties that arnt set by AddArgument arnt set on down the line either
				
				// check Argument properties where no rt arg with matching name exists havnt been initialised
				Assert.IsNull (InString2);
				Assert.IsNull (InString3);
				Assert.IsNull (InString4);
				Assert.IsNull (InString5);
				// check initialised arguments are useable
				Assert.AreEqual (null, InString1.Get (context));
				Assert.AreEqual (0, OutInt1.Get (context));
				Assert.AreEqual (null, InOutString1.Get (context));
				// and can set them
				InOutString1.Set(context, "inout");
				Assert.AreEqual ("inout", InOutString1.Get (context));
				InString1.Set(context, "in");
				Assert.AreEqual ("in", InString1.Get (context));
				// Check automatically bound to rtArg
				Assert.AreEqual ("in", rtInString1.Get (context)); 
				// check less specific argument properties not initialised
				Assert.IsNull (ArgumentString);
				Assert.IsNull (ArgumentInString);
				// check argument properties with incorrect modifiers not initialised
				Assert.IsNull (InStringProtected);
				Assert.IsNull (InStringPrivate);
				Assert.IsNull (InStringStatic);
				Assert.IsNull (InStringInternal);
				Assert.IsNull (InStringProtectedInternal);
				// check preinitialized argument was automatically bound and returned correct value
				Assert.AreEqual ("1", InStringPreInitialized1.Get (context));
				Assert.AreEqual ("1", rtInStringPreInitialized1.Get (context)); 
				// check autobind can cause 2 args to be bound to 1 rtArg
				IllBeDupeA.Set (context, "hello");
				Assert.AreEqual (IllBeDupeA.Get (context), IllBeDupeB.Get (context));
				// check the arg property with no getter was not set
				Assert.IsNull (inStringNoGetter);
			}
		}
		[Test]
		public void AddArgument_AutoInitAndBinds ()
		{
			var wf = new AutoInitBindActivity ();
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void AddArgument_AutoInitAndBinds_WhenNotRoot ()
		{
			var wf = new Sequence { Activities = { new AutoInitBindActivity () } };
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Need to look into param validation in CacheMetadataMethods")]
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
		[Ignore ("Need to look into param validation in CacheMetadataMethods")]
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
				Assert.AreEqual ("Hello\nWorld", rtArg.Get(context));
			};
			Run (metadataAction, execute);
		}
		[Test]
		public void Bind_RuntimeArgNotDeclared ()
		{
			// note calling Bind with a rtArg that hasnt been passed to AddArgument doesnt raise error but 
			// rtArg cant be used later
			var arg = new InArgument<string> ();
			var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				metadata.Bind (arg, rtArg);
			};
			Run (metadataAction, null);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Bind_RuntimeArgNotDeclaredExecuteEx ()
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
		[Test, ExpectedException (typeof (NullReferenceException))]
		[Ignore ("Need to look into param validation in CacheMetadataMethods")]
		public void Bind_ArgDeclaredNullBindingEx ()
		{
			var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = metadata => {
				metadata.AddArgument(rtArg);
				metadata.Bind (null, rtArg); 
			};
			Run (metadataAction, null);
		}
		[Test]
		public void Bind_ArgNotDeclaredNullBinding ()
		{
			var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = metadata => {
				metadata.Bind (null, rtArg); 
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
				Assert.AreEqual ("SetByArg2", arg.Get(context));
			};
			Run (metadataAction, execute);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void Bind_TypeMismatchEx ()
		{
			/*System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			  'CodeActivityRunner': The Argument provided for the RuntimeArgument 'arg1' cannot be bound because of a type mismatch.  
			  The RuntimeArgument declares the type to be System.String and the Argument has a type of System.Int32.  Both types must 
			  be the same.*/
			Action<CodeActivityMetadata> metadataAction = (metadata) => {
				var rtArg = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
				var arg = new InArgument<int> ();
				metadata.AddArgument (rtArg);
				metadata.Bind (arg, rtArg);
			};
			Run (metadataAction, null);
		}
		[Test]
		public void SetArgumentsCollection ()
		{
			var rtArg = new RuntimeArgument ("NAMEarg0", typeof (string), ArgumentDirection.In);
			var rtArg2 = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var args = new Collection<RuntimeArgument> {
					rtArg2,
					new RuntimeArgument ("arg2", typeof (string), ArgumentDirection.In),
					new RuntimeArgument ("arg3", typeof (string), ArgumentDirection.In)
				};

				metadata.SetArgumentsCollection (null); // passing null doesnt raise error on .NET
				metadata.SetArgumentsCollection (args);
				metadata.AddArgument (rtArg);
				Assert.Contains(rtArg, args);
			};
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue (rtArg, "rtArg");
				context.SetValue (rtArg2, "rtArg2");
				Assert.AreEqual ("rtArg", context.GetValue (rtArg));
				Assert.AreEqual ("rtArg2", context.GetValue (rtArg2));
			};
			Run (metadataAction, execute);
		}
		[Test, ExpectedException(typeof (InvalidOperationException))]
		public void SetArgumentsCollectionNull_ClearsExisting ()
		{
			// The argument 'NAMEarg0' cannot be used. Make sure it is declared on an activity
			var rtArg = new RuntimeArgument ("NAMEarg0", typeof (string), ArgumentDirection.In);
			Action<CodeActivityMetadata> metadataAction = metadata => {
				metadata.AddArgument (rtArg);
				metadata.SetArgumentsCollection (null);
			};
			Action<CodeActivityContext> execute = (context) => {
				context.SetValue (rtArg, "rtArg");
				Assert.AreEqual ("rtArg", context.GetValue (rtArg));
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
		[Test, ExpectedException(typeof (InvalidWorkflowException))]
		[Ignore("Validation")]
		public void SetArgumentsCollection_DupeNameInCollectionEx ()
		{
			//the Exception should not be raised from SetArgumentsCollection
			Action<CodeActivityMetadata> metadataAction = metadata => {
				var args = new Collection<RuntimeArgument> {
					new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In), // dupe name
					new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In), // dupe name
					new RuntimeArgument ("arg3", typeof (string), ArgumentDirection.In)
				};
				bool errorRaised = false;
				try {
					metadata.SetArgumentsCollection (args);
				} catch (InvalidWorkflowException ex) {
					errorRaised = true;
				} finally {
					Assert.IsFalse(errorRaised);
				}
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
}
