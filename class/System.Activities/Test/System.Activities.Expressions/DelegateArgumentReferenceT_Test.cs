using System;
using NUnit.Framework;
using System.Activities.Expressions;
using System.Activities;

namespace MonoTests.System.Activities.Expressions {
	[TestFixture]
	public class DelegateArgumentReferenceT_Test : WFTestHelper {
		#region Ctors
		[Test]
		public void Ctor ()
		{
			var dar = new DelegateArgumentReference<string> ();
			Assert.IsNull (dar.DelegateArgument);
		}
		[Test]
		public void Ctor_DelegateArgument ()
		{
			var da = new DelegateInArgument<string> ();
			var dar = new DelegateArgumentReference<string> (da);
			Assert.AreSame (da, dar.DelegateArgument);

			// .NET does not throw error when type param of DelegateInArgument clashes with that of DAR
			//FIXME: does WF validate during wf execution / what would happen?
			var daInt = new DelegateInArgument<int> ();
			var darStr = new DelegateArgumentReference<string> (daInt);
			Assert.AreSame (daInt, darStr.DelegateArgument);
			// .NET doesnt throw error on null param
			var dar3 = new DelegateArgumentReference<string> (null);
		}
		#endregion

		#region Properties
		[Test]
		public void DelegateArgument ()
		{
			var daStr = new DelegateInArgument<string> ();
			var daInt = new DelegateInArgument<int> ();
			var dar = new DelegateArgumentReference<string> (daStr);
			Assert.AreSame (daStr, dar.DelegateArgument);

			dar.DelegateArgument = daInt;
			Assert.AreSame (daInt, dar.DelegateArgument);

			dar.DelegateArgument = null;
			Assert.IsNull (dar.DelegateArgument);
		}
		#endregion

		#region Methods
		[Test]
		[Ignore ("ToString fails on generics issue")]
		public void ToStringTest ()
		{
			var daStr = new DelegateInArgument<string> ("aStr");
			var daInt = new DelegateInArgument<int> ("anInt");

			var dar = new DelegateArgumentReference<string> (daStr);
			Assert.AreEqual (": DelegateArgumentReference<String>", dar.ToString ());

			dar.DelegateArgument = daInt;
			Assert.AreEqual (": DelegateArgumentReference<String>", dar.ToString ());

			var darNoArg = new DelegateArgumentReference<string> ();
			Assert.AreEqual (": DelegateArgumentReference<String>", darNoArg.ToString ());
		}
		[Test]
		public void Execute () //protected
		{
			//FIXME: convoluted test for this

			var argOut = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = argOut,
				Handler = new Concat { 
						String1 = "1", String2 = "2", 
						Result = new OutArgument<string> (new DelegateArgumentReference<string> (argOut))
					}
			};

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleDelegate (func, null, (ctx, compAI, outArgs) => {
					Console.WriteLine (outArgs ["Result"]);
				});
			});
			RunAndCompare (wf, "12" + Environment.NewLine);
		}
		#endregion
	}
}

