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

namespace Tests.System.Activities {
	class ActivityMetadataTest {
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
