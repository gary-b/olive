using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ActivityMetadataTest {
		Activity GetMetadataWriter (string msg)
		{
			return new NativeActivityRunner ((metadata)=> { 
				Console.WriteLine (msg); 
			}, null);
		}
		ActivityAction GetMetadataWriterAction (string msg)
		{
			return new ActivityAction {
				Handler = new NativeActivityRunner ((metadata)=> { 
					Console.WriteLine (msg); 
				}, null),
			};
		}
		Activity<string> GetMetadataWriterT (string msg)
		{
			return new CodeActivityTRunner<string> ((metadata)=> { 
				Console.WriteLine (msg); 
			}, (context) => {
				return msg;
			});
		}
		[Test]
		public void CacheMetadata_Vars_Args_Children_Delegates ()
		{
			var impChild1 = GetMetadataWriter ("impChild1");
			var pubChild1 = GetMetadataWriter ("pubChild1");
			var impChild2 = GetMetadataWriter ("impChild2");
			var pubChild2 = GetMetadataWriter ("pubChild2");
			var arg1 = new InArgument<string> (GetMetadataWriterT ("arg1"));
			var arg2 = new InArgument<string> (GetMetadataWriterT ("arg2"));
			var impV1 = new Variable<string> ();
			var impV2 = new Variable<string> ();
			var pubV1 = new Variable<string> ();
			var pubV2 = new Variable<string> ();
			impV1.Default = GetMetadataWriterT ("impV1");
			impV2.Default = GetMetadataWriterT ("impV2");
			pubV1.Default = GetMetadataWriterT ("pubV1");
			pubV2.Default = GetMetadataWriterT ("pubV2");
			var pubDel1 = GetMetadataWriterAction ("pubDel1");
			var pubDel2 = GetMetadataWriterAction ("pubDel2");
			var impDel1 = GetMetadataWriterAction ("impDel1");
			var impDel2 = GetMetadataWriterAction ("impDel2");

			var wf = new NativeActivityRunner ((metadata) => {
				Console.WriteLine ("wf");
				metadata.AddImplementationChild (impChild2);
				metadata.AddChild (pubChild2);

				var rtArg2 = new RuntimeArgument ("arg2", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg2);
				metadata.Bind (arg2, rtArg2);

				metadata.AddImplementationVariable (impV2);
				metadata.AddVariable (pubV2);

				metadata.AddImplementationDelegate (impDel2);
				metadata.AddDelegate (pubDel2);

				var rtArg1 = new RuntimeArgument ("arg1", typeof (string), ArgumentDirection.In);
				metadata.AddArgument (rtArg1);
				metadata.Bind (arg1, rtArg1);

				metadata.AddImplementationVariable (impV1);
				metadata.AddVariable (pubV1);

				metadata.AddChild (pubChild1);
				metadata.AddImplementationChild (impChild1);

				metadata.AddImplementationDelegate (impDel1);
				metadata.AddDelegate (pubDel1);
			}, null);
			var app = new WFAppWrapper (wf);
			app.Run ();
			//Test Order Called
			var split = app.ConsoleOut.Split (new string [] { Environment.NewLine }, StringSplitOptions.None);
			//remove trailing empty string
			var actualOrder = new string [split.Length - 1];
			for (int i = 0; i < split.Length - 1; i++)
				actualOrder [i] = split [i];
			var expected = new string [] {
				"wf",
				"impDel1", "impDel2", "pubDel1", "pubDel2",
				"impV1", "impV2", "pubV1", "pubV2",
				"arg1", "arg2",
				"impChild1", "impChild2", "pubChild1", "pubChild2"
			};
			Assert.AreEqual (expected, actualOrder);

			// Test Activity Ids Generated
			Assert.AreEqual ("1", wf.Id);
			Assert.AreEqual ("8", pubChild1.Id);
			Assert.AreEqual ("9", pubChild2.Id);
			Assert.AreEqual ("1.5", impChild1.Id);
			Assert.AreEqual ("1.6", impChild2.Id);
			Assert.AreEqual ("2", pubDel1.Handler.Id);
			Assert.AreEqual ("3", pubDel2.Handler.Id);
			Assert.AreEqual ("1.1", impDel1.Handler.Id);
			Assert.AreEqual ("1.2", impDel2.Handler.Id);
			Assert.AreEqual ("6", arg1.Expression.Id);
			Assert.AreEqual ("7", arg2.Expression.Id);
			Assert.AreEqual ("4", pubV1.Default.Id);
			Assert.AreEqual ("5", pubV2.Default.Id);
			Assert.AreEqual ("1.3", impV1.Default.Id);
			Assert.AreEqual ("1.4", impV2.Default.Id);
		}
		class ActivityMetaMock : Activity {
			public InArgument<string> InString1 { get; set; }
			public OutArgument<int> OutInt1 { get; set; }
			public InOutArgument<TextWriter> InOutTw1 { get; set; }
			public Argument ArgumentDontInitialiseMe { get; set; } // be ignored
			public Argument ArgumentLessSpecific { get; set; } // still be ignored even if initialized

			public Variable<string> VarDontInitialiseMe { get; set; }  // be ignored
			public Variable<string> VarString1 { get; set; }

			public Activity ActivityDontInitialiseMe { get; set; }  // be ignored
			public WriteLine ActWriteLineDontInitialiseMe { get; set; }  // be ignored
			public Activity ActWriteLine { get; set; }
			public WriteLine ActWriteLineSpecific { get; set; }
			public Activity<string> ActLiteral { get; set; }

			public ActivityDelegate ADelDontInitialiseMe { get; set; }

			public ActivityMetaMock ()
			{
				this.Implementation = () => new WriteLine { Text = "Hello" };
				ArgumentLessSpecific = new InArgument<string> ();

				ActWriteLine = new WriteLine ();
				ActWriteLineSpecific = new WriteLine ();
				ActLiteral = new Literal<string> ();
				VarString1 = new Variable<string> ();
			}
			protected override void CacheMetadata (ActivityMetadata metadata)
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
				// Variables
				Collection<Variable> variables = metadata.GetVariablesWithReflection ();
				Assert.AreEqual (1, variables.Count); // uninitialised variables are ignored
				Assert.AreSame (VarString1, variables [0]);
				// Activities
				Collection<Activity> activities = metadata.GetImportedChildrenWithReflection ();
				Assert.AreEqual (3, activities.Count); // 2 uninitialised activities are ignored
				Assert.AreSame (ActWriteLine, activities [0]);
				Assert.AreSame (ActWriteLineSpecific, activities [1]);
				Assert.AreSame (ActLiteral, activities [2]);
			}
		}
		[Test]
		[Ignore ("Get...WithReflection")]
		public void RunTests ()
		{
			var activityMeta = new ActivityMetaMock ();
			WorkflowInvoker.Invoke (activityMeta); // tests in override

		}

		/* TODO test
		   operatorEquals
		 * operatorNotEqual
		 * Environment
		 * HasViolations
		 * AddArgument
		 * AddDefaultExtensionProvider
		 * AddImportedChild
		 * AddImportedDelegate
		 * AddValidationError_String
		 * AddValidationError_Error
		 * AddVariable
		 * Bind
		 * Equals?
		 * GetHashCode?
		 * GetImportedDelegatesWithReflection
		 * RequireExtensionT
		 * RequireExtensionT_T
		 * SetArgumentsCollection
		 * SetImportedChildrenCollection
		 * SetImportedDelegatesCollection
		 * SetValidationErrorsCollection
		 * SetVariablesCollection
		 */ 
	}
}
