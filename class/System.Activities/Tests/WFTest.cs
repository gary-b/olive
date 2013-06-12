using System;
using System.Activities;
using System.IO;
using NUnit.Framework;
using System.Activities.Statements;

namespace Tests.System.Activities
{
	public class WFTest {
		protected void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}

	}
	public class NativeRunnerMock : NativeActivity	{
		Action<NativeActivityMetadata> cacheMetadataAction;
		Action<NativeActivityContext> executeAction;
		public new int CacheId { get { return base.CacheId; } }
		public NativeRunnerMock (Action<NativeActivityMetadata> cacheMetadata, Action<NativeActivityContext> execute)
		{
			cacheMetadataAction = cacheMetadata;
			executeAction = execute;
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			if (cacheMetadataAction != null)
				cacheMetadataAction (metadata);
		}
		protected override void Execute (NativeActivityContext context)
		{
			if (executeAction != null)
				executeAction (context);
		}
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
	class TrackIdWrite : CodeActivity {
		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
		}
		protected override void Execute (CodeActivityContext context)
		{
			Console.WriteLine ("CacheId: {0} ActivityInstanceId: {1} Id: {2}",
			                   this.CacheId, context.ActivityInstanceId, this.Id);

		}
	}
	class WriteLineHolder : NativeActivity {
		public WriteLine ImplementationWriteLine { get; set; }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			metadata.AddImplementationChild (ImplementationWriteLine);
		}
		protected override void Execute (NativeActivityContext context)
		{
			context.ScheduleActivity (ImplementationWriteLine);
		}
	}
}

