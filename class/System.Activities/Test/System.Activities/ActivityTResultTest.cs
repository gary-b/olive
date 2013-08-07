using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.ObjectModel;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ActivityTResultTest {
		class ActivityTMock : Activity<string> {
			public ActivityTMock()
			{
				Implementation = () => new Assign<string> 
				{	
					To = new OutArgument<string> (new ArgumentReference<string> ("Result")),
					Value = new InArgument<string> ("Hello\nWorld")
				};
			}
		}

		#region Properties
		/* Tested in CodeActivity<T>
			public void Result ()
		*/
		[Test]
		public void ResultType ()
		{
			var activityT = new ActivityTMock ();
			Assert.AreEqual (typeof (string), activityT.ResultType);
		}
		#endregion

		#region Auto Initialization of Result
		class ActivityT_ResultNotDeclaredMock : Activity<string> {
			protected override void CacheMetadata (ActivityMetadata metadata)
			{
				// no declaration of RuntimeArgument for Result
			}
			public ActivityT_ResultNotDeclaredMock ()
			{
				Implementation = () => new Assign<string> 
				{	
					To = new OutArgument<string> (new ArgumentReference<string> ("Result")),
					Value = new InArgument<string> ("Hello\nWorld")
				};
			}
		}

		[Test]
		public void ResultRuntimeArgumentCreatedAutomatically ()
		{
			var wf = new ActivityT_ResultNotDeclaredMock ();
			Assert.AreEqual ("Hello\nWorld", WorkflowInvoker.Invoke<string> (wf));
		}
		#endregion

		[Test]
		public void Execute ()
		{
			var activityT = new ActivityTMock ();
			string result = WorkflowInvoker.Invoke (activityT);
			Assert.AreEqual ("Hello\nWorld", result);
		}

		#region Methods
		[Test]
		[Ignore ("Not Implemented")]
		public void FromValue ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void FromVariable ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void FromVariableT ()
		{
			throw new NotImplementedException ();
		}
		#endregion

		#region Operators
		[Test]
		[Ignore ("Not Implemented")]
		public void TResultToActivity_TResult ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void VariableToActivity_TResult ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Variable_TResultToActivity_TResult ()
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}
