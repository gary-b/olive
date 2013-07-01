using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;

namespace Tests.System.Activities {
	[TestFixture]
	public class BookmarkHandlingRuntimeTest : WFTest {
		public class NativeActWithCBRunner : NativeActivity	{
			Action<NativeActivityMetadata> cacheMetadataAction;
			Action<NativeActivityContext, CompletionCallback> executeAction;
			Action<NativeActivityContext, ActivityInstance, CompletionCallback> callbackAction;
			public NativeActWithCBRunner (Action<NativeActivityMetadata> cacheMetadata, 
							Action<NativeActivityContext, CompletionCallback> execute,
							Action<NativeActivityContext, ActivityInstance, CompletionCallback> callback)
			{
				cacheMetadataAction = cacheMetadata;
				executeAction = execute;
				callbackAction = callback;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				if (cacheMetadataAction != null)
					cacheMetadataAction (metadata);
			}
			protected override void Execute (NativeActivityContext context)
			{
				if (executeAction != null)
					executeAction (context, Callback);
			}
			void Callback (NativeActivityContext context, ActivityInstance completedInstance)
			{
				if (callbackAction != null)
					callbackAction (context, completedInstance, Callback);
			}

		}
		public class NativeActWithCBRunner<CallbackType> : NativeActivity	{
			Action<NativeActivityMetadata> cacheMetadataAction;
			Action<NativeActivityContext, CompletionCallback<CallbackType>> executeAction;
			Action<NativeActivityContext, ActivityInstance, CompletionCallback<CallbackType>, CallbackType> callbackAction;

			public NativeActWithCBRunner (Action<NativeActivityMetadata> cacheMetadata, 
			                              Action<NativeActivityContext, CompletionCallback<CallbackType>> execute,
			                              Action<NativeActivityContext, ActivityInstance, 
			                              CompletionCallback<CallbackType>, CallbackType> callback)
			{
				cacheMetadataAction = cacheMetadata;
				executeAction = execute;
				callbackAction = callback;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				if (cacheMetadataAction != null)
					cacheMetadataAction (metadata);
			}
			protected override void Execute (NativeActivityContext context)
			{
				if (executeAction != null)
					executeAction (context, Callback);
			}
			void Callback (NativeActivityContext context, ActivityInstance completedInstance, CallbackType result)
			{
				if (callbackAction != null)
					callbackAction (context, completedInstance, Callback, result);
			}

		}
		[Test]
		public void OnCompleted_AnonymousDelegate ()
		{
			var writeLine = new WriteLine { Text = "WriteLine" };
			var wf = new NativeActivityRunner ((metadata)=> {
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine, (cbContext, completedInstance) => {
					Console.WriteLine ("Callback");
				});
			});
			RunAndCompare (wf, "WriteLine" + Environment.NewLine + "Callback" + Environment.NewLine);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void OnCompleted_AnonymousDelegateCantScheduleEx ()
		{
			//System.ArgumentException : 'System.Activities.CompletionCallback' is not a valid activity execution callback. 
			//The execution callback used by '1: NativeActivityRunner' must be an instance method on '1: NativeActivityRunner'.
			//Parameter name: onCompleted
			var writeLine = new WriteLine { Text = "WriteLine" };
			var wf = new NativeActivityRunner ((metadata)=> {
				metadata.AddChild (writeLine);
			}, (context) => {
				context.ScheduleActivity (writeLine, (cbContext, completedInstance) => {
					Console.WriteLine ("sdsadsa");
					cbContext.ScheduleActivity (writeLine);
				});
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void OnCompleted_AnonymousDelegateCantAccessVarEx ()
		{
			//System.ArgumentException : 'System.Activities.CompletionCallback' is not a valid activity execution callback. 
			//The execution callback used by '1: NativeActivityRunner' must be an instance method on '1: NativeActivityRunner'.
			//Parameter name: onCompleted
			var vStr = new Variable<string> ();
			var writeLine = new WriteLine { Text = "WriteLine" };
			var wf = new NativeActivityRunner ((metadata)=> {
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				context.ScheduleActivity (writeLine, (cbContext, completedInstance) => {
					Console.WriteLine (cbContext.GetValue (vStr));
				});
			});
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		public void CompletionCallback_Simple ()
		{
			var writeLine = new WriteLine { Text = "W" };
			var wf = new NativeActWithCBRunner ((metadata)=> {
				metadata.AddImplementationChild (writeLine);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine, callback);
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("CompletionCallback");
			});
			RunAndCompare (wf, String.Format ("W{0}CompletionCallback{0}", Environment.NewLine));
		}
		[Test]
		public void CompletionCallback_VarAccessAndMultipleSchedules ()
		{
			var vStr = new Variable<string> ("","O");
			var writeLine = new WriteLine { Text = vStr };
			var appendToVar = new Assign { Value = new InArgument<string> (new Concat {String1 = vStr, String2 = "M"}),
				To = new OutArgument<string> (vStr)
			};
			var wf = new NativeActWithCBRunner ((metadata)=> {
				metadata.AddImplementationVariable (vStr);
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationChild (appendToVar);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine);
				context.ScheduleActivity (appendToVar);
				context.ScheduleActivity (writeLine, callback);
				context.ScheduleActivity (appendToVar);
				context.ScheduleActivity (writeLine);
			}, (context, completedInstance, callback) => {
				Console.WriteLine ("CompletionCallback");
			});
			RunAndCompare (wf, String.Format ("O{0}OM{0}CompletionCallback{0}OMM{0}", Environment.NewLine));
		}
		[Test]
		public void CompletionCallback_VarAccessAndSchedulesOtherActivities ()
		{
			var vInt = new Variable<int> ("", 0);
			var writeLine = new WriteLine { Text = "W" };
			var wf = new NativeActWithCBRunner ((metadata)=> {
				metadata.AddImplementationChild (writeLine);
				metadata.AddImplementationVariable (vInt);
			}, (context, callback) => {
				context.ScheduleActivity (writeLine, callback);
			}, (context, completedInstance, callback) => {
				context.SetValue (vInt, context.GetValue (vInt) + 1);
				if (context.GetValue (vInt) < 4)
					context.ScheduleActivity (writeLine, callback);
			});
			RunAndCompare (wf, String.Format ("W{0}W{0}W{0}W{0}", Environment.NewLine));
		}
		[Test]
		public void CompletionCallbackT_VarAccessAndSchedulesOtherActivities ()
		{
			var vInt = new Variable<int> ("", 0);
			var concat = new Concat { String1 = "H", String2 = "W" };
			var wf = new NativeActWithCBRunner<string> ((metadata)=> {
				metadata.AddImplementationChild (concat);
				metadata.AddImplementationVariable (vInt);
			}, (context, callback) => {
				context.ScheduleActivity (concat, callback);
			}, (context, completedInstance, callback, result) => {
				Console.WriteLine (result);
				context.SetValue (vInt, context.GetValue (vInt) + 1);
				if (context.GetValue (vInt) < 4)
					context.ScheduleActivity (concat, callback);
			});
			RunAndCompare (wf, String.Format ("HW{0}HW{0}HW{0}HW{0}", Environment.NewLine));
		}
	}
}

