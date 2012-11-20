using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.ObjectModel;

namespace Tests.System.Activities {
	[TestFixture]
	class ActivityTResultTest {
		class ActivityTMock : Activity<string> {
			public ActivityTMock()
			{
				Implementation = () => new Assign<string> 
				{	
					To = new ArgumentReference<string> ("Result"),
					Value = new InArgument<string> ("Hello\nWorld")
				};
			}
		}

		class ActivityTResultMock : Activity<string> {
			public ActivityTResultMock ()
			{
			}

			protected override void  CacheMetadata(ActivityMetadata metadata)
			{
				base.CacheMetadata(metadata);
				Collection<RuntimeArgument> runtimeArgs = metadata.GetArgumentsWithReflection ();
				Assert.AreEqual (1, runtimeArgs.Count);
				RuntimeArgument argOutResult = runtimeArgs [0];
				Assert.AreEqual (ArgumentDirection.Out, argOutResult.Direction);
				Assert.IsFalse (argOutResult.IsRequired);
				Assert.AreEqual ("Result", argOutResult.Name);
				Assert.AreEqual (0, argOutResult.OverloadGroupNames.Count);
				Assert.AreEqual (typeof (string), argOutResult.Type);
			}
		}

		#region Properties
		[Test]
		public void Result ()
		{
			var activityT = new ActivityTMock ();
			Assert.IsNull (activityT.Result);
			//check its been detected in CacheMetadata
			var actResult = new ActivityTResultMock ();
			WorkflowInvoker.Invoke (actResult);
		}
		[Test]
		public void ResultType ()
		{
			var activityT = new ActivityTMock ();
			Assert.AreEqual (typeof (string), activityT.ResultType);
		}
		#endregion

		[Test]
		public void Execution ()
		{
			var activityT = new ActivityTMock ();
			string result = WorkflowInvoker.Invoke (activityT);
			Assert.AreEqual ("Hello\nWorld", result);
		}

		#region Methods
		[Test]
		public void FromValue ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void FromVariable ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void FromVariableT ()
		{
			throw new NotImplementedException ();
		}
		#endregion

		#region Operators
		[Test]
		public void TResultToActivity_TResult ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void VariableToActivity_TResult ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Variable_TResultToActivity_TResult ()
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}
