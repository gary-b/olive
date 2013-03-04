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
	class CodeActivityTResultTest {
		class CodeActivityTMock : CodeActivity<string> {
			new public Func<Activity> Implementation	{
				get { return base.Implementation; }
				set { base.Implementation = value; }
			}

			protected override string Execute (CodeActivityContext context)
			{
				throw new NotImplementedException ();
			}
		}
		class CodeActivityTResultRunner<T> : CodeActivity<T> {
			Action<CodeActivityMetadata, OutArgument<T>> cacheMetaDataAction;
			Func<CodeActivityContext, OutArgument<T>, T> executeFunc;
			public CodeActivityTResultRunner (Action<CodeActivityMetadata, OutArgument<T>> action, 
			                                  Func<CodeActivityContext, OutArgument<T>, T> execute)
			{
				cacheMetaDataAction = action;
				executeFunc = execute;
			}
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				var rtResult = new RuntimeArgument ("Result", typeof (T), ArgumentDirection.Out);
				metadata.Bind (Result, rtResult);

				if (cacheMetaDataAction != null)
					cacheMetaDataAction (metadata, Result);
			}
			protected override T Execute (CodeActivityContext context)
			{
				if (executeFunc != null)
					return executeFunc (context, Result);
				else
					return default (T);
			}
		}

		#region Properties
		[Test]
		public void Implementation_Get ()
		{
			var codeActivity = new CodeActivityTMock ();
			Assert.IsNull (codeActivity.Implementation);
		}
		[Test, ExpectedException (typeof (NotSupportedException))]
		public void Implementation_SetEx ()
		{
			var codeActivity = new CodeActivityTMock ();
			Assert.IsNull (codeActivity.Implementation);
			codeActivity.Implementation = () => new WriteLine ();
		}

		[Test]
		[Ignore ("Unsure about this test")]
		public void Result ()
		{
			Func<CodeActivityContext, OutArgument<string>, string> execute = (context, Result) => {
				var loc = Result.GetLocation (context);
				Assert.AreEqual (null, loc.Value);
				Assert.AreEqual (typeof (string),  loc.LocationType);
				return null;
			};
			var wf = new CodeActivityTResultRunner<string> (null, execute);
			WorkflowInvoker.Invoke (wf);
		}
		#endregion

		#region Methods

		[Test]
		[Ignore ("WorkflowInvoker.Invoke<TResult>")]
		public void Execute ()
		{
			Func<CodeActivityContext, OutArgument<string>, string> execute = (context, Result) => {
				return "Execute\nMock";
			};
			var wf = new CodeActivityTResultRunner<string> (null, execute);
			string result = WorkflowInvoker.Invoke (wf);
			Assert.AreEqual ("Execute\nMock", result);
		}

		#endregion

	}
}
