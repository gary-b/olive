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
				metadata.AddArgument (rtResult);
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

		#region Auto Initialisation of Result


		class BareCodeActivityTRunner<T> : CodeActivity<T> {
			Action<CodeActivityMetadata> cacheMetadataAction;
			Func<CodeActivityContext, T> executeAction;
			public BareCodeActivityTRunner (Action<CodeActivityMetadata> cacheMetadata, Func<CodeActivityContext, T> execute)
			{
				cacheMetadataAction = cacheMetadata;
				executeAction = execute;
			}
			protected override void CacheMetadata (CodeActivityMetadata metadata)
			{
				if (cacheMetadataAction != null)
					cacheMetadataAction (metadata);
			}
			protected override T Execute (CodeActivityContext context)
			{
				return executeAction (context);
			}
		}
		[Test]
		public void ResultRuntimeArgumentCreatedAutomatically ()
		{
			var wf = new BareCodeActivityTRunner<string> ((metadata) => {
				// no RuntimeArgument for Result
			}, (context) => {
				return "Hello\nWorld";
			});
			Assert.AreEqual ("Hello\nWorld", WorkflowInvoker.Invoke<string> (wf));
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void ResultRuntimeArgumentTypeClash ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'BareCodeActivityTRunner<String>': The activity author supplied RuntimeArgument named 'Result' must have ArgumentDirection 
			//Out and type System.String.  Instead, it has ArgumentDirection Out and type System.Int32.
			var wf = new BareCodeActivityTRunner<string> ((metadata) => {
				var rtResultInt = new RuntimeArgument ("Result", typeof(int), ArgumentDirection.Out);
				metadata.AddArgument (rtResultInt);
			}, (context) => {
				return "Hello\nWorld";
			});
			WorkflowInvoker.Invoke<string> (wf);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		public void ResultRuntimeArgumentDirectionClash ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'BareCodeActivityTRunner<String>': The activity author supplied RuntimeArgument named 'Result' must have ArgumentDirection 
			// Out and type System.String.  Instead, it has ArgumentDirection InOut and type System.String.
			var wf = new BareCodeActivityTRunner<string> ((metadata) => {
				var rtResultInt = new RuntimeArgument ("Result", typeof(string), ArgumentDirection.InOut);
				metadata.AddArgument (rtResultInt);
			}, (context) => {
				return "Hello\nWorld";
			});
			WorkflowInvoker.Invoke<string> (wf);
		}

		#endregion
	}
}
