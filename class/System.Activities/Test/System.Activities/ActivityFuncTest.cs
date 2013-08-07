using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;
using System.IO;
using System.Collections.Generic;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ActivityFuncTest : ActivityDelegateTestHelper {
		CompletionCallback<string> compCBWriter = (ctx, compAI, value) => {
			Console.WriteLine ((string) value);
		};
		[Test]
		public void Handler_Activity_NoOutArgs_NullReturned ()
		{
			var func = new ActivityFunc<string> {
				Handler = new WriteLine { Text = "WriteLine" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, (ctx, compAI, value) => {
					Assert.IsNull (value);
					Console.WriteLine ((string) value);
				});
			});
			RunAndCompare (wf, String.Format ("WriteLine{0}{0}", Environment.NewLine));
			//null was passed into completioncallbackT
		}
		[Test]
		public void Handler_Activity_NoOutArgs_DefaultReturnedWhenValuetype ()
		{
			var func = new ActivityFunc<int> {
				Handler = new WriteLine { Text = "WriteLine" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, String.Format ("WriteLine{0}0{0}", Environment.NewLine));
			//null was passed into completioncallbackT
		}
		[Test]
		public void ActivityFuncInt_Handler_ActivityTString_IncompatibleResultIgnored ()
		{
			var func = new ActivityFunc<int> {
				Handler = new Concat { String1 = "Hello", String2 = "\nWorld" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, String.Format ("0{0}", Environment.NewLine));
		}
		[Test]
		public void ActivityFuncObject_Handler_ActivityTString_ResultReturned ()
		{
			var func = new ActivityFunc<object> {
				Handler = new Concat { String1 = "Hello", String2 = "\nWorld" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, (ctx, compAI, value) => {
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test]
		public void ActivityFuncString_Handler_ActivityTInt_IncompatibleResultIgnored ()
		{
			var intReturner = new CodeActivityTRunner<int> (null, (context) => {
				return 5;
			});
			var func = new ActivityFunc<string> {
				Handler = intReturner
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, compCBWriter);
			});
			RunAndCompare (wf, String.Format ("{0}", Environment.NewLine));
		}
		[Test]
		public void Handler_ActivityT_NoOutDelArg_ResultReturned ()
		{
			var func = new ActivityFunc<string> {
				Handler = new Concat { String1 = "Hello", String2 = "\nWorld" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, compCBWriter);
			});
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test]
		public void Handler_ActivityT_HasOutDelArgButNotSetOnResult ()
		{
			var outArg = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = outArg,
				Handler = new Concat { String1 = "Hello", String2 = "\nWorld" }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, (ctx, compAI, value) => {
					Assert.IsNull (value);
					Console.WriteLine (value);
				});
			});
			RunAndCompare (wf, String.Format ("{0}", Environment.NewLine));
		}
		[Test]
		public void Handler_Activity_OutArg_NoOutDelArg_NullReturned ()
		{
			var func = new ActivityFunc<string> {
				Handler = new ActivityWithOutArg ()
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, (ctx, compAI, value) => {
					Assert.IsNull (value);
					Console.WriteLine ((string) value);
				});
			});
			RunAndCompare (wf, String.Format ("{0}", Environment.NewLine));
			//ActivityWithOutArg returned Hello\nWorld to its ReturnValue out arg and was ignored
		}
		[Test]
		public void Handler_Activity_WithResultArg_NoOutDelArg_NullReturned ()
		{
			var func = new ActivityFunc<string> {
				Handler = new NonActivityWithResultWithResult ()
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, compCBWriter);
			});
			RunAndCompare (wf, String.Format ("{0}", Environment.NewLine));
		}
		[Test]
		public void Handler_Activity_WithResultArg_WithOutDelArg_ArgReturned ()
		{
			var outDelArg = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = outDelArg,
				Handler = new NonActivityWithResultWithResult { Result = outDelArg }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, compCBWriter);
			});
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test]
		public void Handler_Activity_OutArg_HasOutDelArg_OutArgReturned ()
		{
			var outArg = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = outArg,
				Handler = new ActivityWithOutArg { ReturnValue = outArg }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, compCBWriter);
			});
			RunAndCompare (wf, String.Format ("Hello\nWorld{0}", Environment.NewLine));
		}
		[Test]
		public void Handler_ActivityT_And2ndOutArg_HasOutDelArgPointingTo2ndOutArg_2ndOutArgReturned ()
		{
			var outArg = new DelegateOutArgument<string> ();
			var func = new ActivityFunc<string> {
				Result = outArg,
				Handler = new ActivityWithResultWith2ndOutArg { SecondOutArg = outArg }
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDelegate (func);
			}, (context) => {
				context.ScheduleFunc (func, compCBWriter);
			});
			RunAndCompare (wf, String.Format ("SecondOutArg{0}", Environment.NewLine));
		}
		class ActivityWithOutArg : CodeActivity {
			public OutArgument<string> ReturnValue { get; set; }
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				var rt = new RuntimeArgument ("ReturnValue", typeof(string), ArgumentDirection.Out);
				metadata.AddArgument (rt);//autobind + intialisation takes care of rest
				Assert.IsNotNull (ReturnValue);
			}
			protected override void Execute (CodeActivityContext context)
			{
				context.SetValue (ReturnValue, "Hello\nWorld");
			}
		}
		class NonActivityWithResultWithResult : CodeActivity {
			public OutArgument<string> Result { get; set; }
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				var rt = new RuntimeArgument ("Result", typeof(string), ArgumentDirection.Out);
				metadata.AddArgument (rt);//autobind + intialisation takes care of rest
				Assert.IsNotNull (Result);
			}
			protected override void Execute (CodeActivityContext context)
			{
				context.SetValue (Result, "Hello\nWorld");
			}
		}
		class ActivityWithResultWith2ndOutArg : CodeActivity<string> {
			public OutArgument<string> SecondOutArg { get; set; }
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				var rt = new RuntimeArgument ("SecondOutArg", typeof(string), ArgumentDirection.Out);
				metadata.AddArgument (rt);//autobind + intialisation takes care of rest
			}
			protected override string Execute (CodeActivityContext context)
			{
				context.SetValue (SecondOutArg, "SecondOutArg");
				return "Result";
			}
		}
		DelegateCompletionCallback delCompCBWriter = (ctx, compAI, outArgs) => {
			Console.WriteLine (outArgs ["Result"]);
		};
		Dictionary<string, object> GetInputDictionaryForDelegate (int noParams)
		{
			if (noParams < 0 || noParams > 16)
				throw new Exception ("noParams must be between 1 and 16");

			var dict = new Dictionary<string, object> ();
			if (noParams == 1) {
				dict.Add ("Argument", "1");
			} else {
				//if noPrams is 0 the empty dictionary is returned
				for (int i = 1; i < (noParams + 1); i++)
					dict.Add ("Argument" + i, i.ToString ());
			}
			return dict;
		}
		void RunConcatManyDelegateAndCompare (ActivityDelegate del, int noParams)
		{
			//using ScheduleDelegate demonstrates that the DelegateOutArgument is working in each class as 
			//ScheduleFunc will return the Result arg of the Handler regardless
			var wf = new NativeActivityRunner (metadata =>  {
				metadata.AddDelegate (del);
			}, context =>  {
				context.ScheduleDelegate (del, GetInputDictionaryForDelegate (noParams), delCompCBWriter);
			});
			var expected = ExpectedConcatManyConsoleOutput (noParams);
			RunAndCompare (wf, expected);
		}
 		[Test]
		public void ActivityFunc_TResult ()
		{
			var func = GetActivityFuncConcatMany0 ();
			RunConcatManyDelegateAndCompare (func, 0);
		}
		[Test]
		public void ActivityFuncT_TResult ()
		{
			var func = GetActivityFuncConcatMany1 ();
			RunConcatManyDelegateAndCompare (func, 1);
		}
		[Test]
		public void ActivityFuncT1T2_TResult ()
		{
			var func = GetActivityFuncConcatMany2 ();
			RunConcatManyDelegateAndCompare (func, 2);
		}
		[Test]
		public void ActivityFuncT1T2T3_TResult ()
		{
			var func = GetActivityFuncConcatMany3 ();
			RunConcatManyDelegateAndCompare (func, 3);
		}
	}
}

