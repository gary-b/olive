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
					To = new OutArgument<string> (new ArgumentReference<string> ("Result")),
					Value = new InArgument<string> ("Hello\nWorld")
				};
			}
		}

		#region Properties
		/* Tested in CodeActivity<T>
		[Test]
		public void Result ()
		{

		}
		*/
		[Test]
		public void ResultType ()
		{
			var activityT = new ActivityTMock ();
			Assert.AreEqual (typeof (string), activityT.ResultType);
		}
		#endregion

		[Test]
		[Ignore ("WorkflowInvoker.Invoke<TResult>")]
		public void Execution ()
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
