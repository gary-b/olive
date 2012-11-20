using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Collections.ObjectModel;
using System.IO;
using System.Activities.Statements;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	// CodeActivityContext has internal ctor so cant be instantiated
	class CodeActivityContextTest {

		class CodeContextMock : CodeActivity {
			public InArgument<string> InString1 { get; set; }
			public OutArgument<int> OutInt1 { get; set; }
			public InOutArgument<string> InOutString1 { get; set; }
			
			public CodeContextMock ()
			{
			}

			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				var rtInString1 = new RuntimeArgument ("InString1",typeof (string),ArgumentDirection.In);
				var rtOutInt1 = new RuntimeArgument ("OutInt1", typeof (int),ArgumentDirection.Out);
				var rtInOutString1 = new RuntimeArgument ("InOutString1", typeof (string), ArgumentDirection.InOut);
				
				metadata.AddArgument (rtInString1);
				InString1 = new InArgument<string> ();
				metadata.Bind (InString1, rtInString1);
				metadata.AddArgument (rtOutInt1);
				OutInt1 = new OutArgument<int> ();
				metadata.Bind (OutInt1, rtOutInt1);
				metadata.AddArgument (rtInOutString1);
				InOutString1 = new InOutArgument<string> ();
				metadata.Bind (InOutString1, rtInOutString1);
			}

			protected override void Execute (CodeActivityContext context)
			{
				// testing ActivityContext methods here too until find a way to access it directly

				var string1 = context.GetValue (InString1);
				//OutInt1.Set (context, 10);
				//Assert.AreEqual (10, OutInt1.Get (context));
				context.SetValue (OutInt1, 30);
				Assert.AreEqual (30, OutInt1.Get (context));//test SetValue_OutArgT
				Assert.AreEqual (30, context.GetValue (OutInt1));//test GetValue_OutArgT
				Assert.AreEqual (30, context.GetValue<int> (OutInt1)); //test GetValue<T>_OutArgT
				context.SetValue<int> (OutInt1, 60);
				Assert.AreEqual (60, OutInt1.Get (context));//test SetValue<T>_OutArgT

				context.SetValue (InString1, "str\n1");
				Assert.AreEqual ("str\n1", InString1.Get (context));//test SetValue_InArgT
				Assert.AreEqual ("str\n1", context.GetValue (InString1));//test GetValue_InArgT
				Assert.AreEqual ("str\n1", context.GetValue<string> (InString1)); //test GetValue<T>_InArgT
				context.SetValue<string> (InString1, "b\nbb");
				Assert.AreEqual ("b\nbb", InString1.Get (context));//test SetValue<T>_InArgT

				context.SetValue (InOutString1, "str\n1");
				Assert.AreEqual ("str\n1", InOutString1.Get (context));//test SetValue_InOutArgT
				Assert.AreEqual ("str\n1", context.GetValue (InOutString1));//test GetValue_InOutArgT
				Assert.AreEqual ("str\n1", context.GetValue<string> (InOutString1)); //test GetValue<T>_InOutArgT
				context.SetValue<string> (InOutString1, "b\nbb");
				Assert.AreEqual ("b\nbb", InOutString1.Get (context));//test SetValue<T>_InOutArgT

				context.SetValue ((Argument)InOutString1, "str\n1");
				Assert.AreEqual ("str\n1", InOutString1.Get (context));//test SetValue_ArgT
				Assert.AreEqual ("str\n1", context.GetValue ((Argument) InOutString1));//test GetValue_ArgT(Argument)

				//not tested SetValueT_LocationReference
				//not tested GetValue_RuntimeArgument
				//not tested GetValueT_LocationReference

				// what should context.ActivityInstanceId be?

				//below causes ex: IOE: as above Variable 'VarString1' of type 'System.String' cannot be used. Please make sure it is declared in an Activity or SymbolResolver.
				//context.GetValue<string> ((LocationReference) VarString1);

				//not tested context.GetLocation returns Location<T>
				//not tested context.GetExtension T gets an extension of type T
				
				//CodeActivityContext specific
				//context.GetProperty gets an execution property
				//context.Track
			}
		}

		[Test]
		public void RunTests ()
		{
			var cm = new CodeContextMock ();
			WorkflowInvoker.Invoke (cm);
		}
		
	}
}
