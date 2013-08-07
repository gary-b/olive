using NUnit.Framework;
using System;
using System.Activities;
using System.Collections.ObjectModel;
using System.Activities.Validation;
using System.Activities.Statements;
using System.Activities.Expressions;
using System.IO;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ActivityTest 	{
		class ActivityMock : Activity {
			new public int CacheId { get { return base.CacheId; } }
			new public Collection<Constraint> Constraints { get { return base.Constraints; } }
			new public Func<Activity> Implementation { 
				get { return base.Implementation; }
				set { base.Implementation = value; }
			}
		}

		#region Properties
		[Test]
		public void Ctor ()
		{
			var activity = new ActivityMock ();
			Assert.IsNull (activity.Id);
			Assert.IsNotNull (activity.Constraints);
			Assert.AreEqual (0, activity.Constraints.Count);
			Assert.AreEqual (0, activity.CacheId);
		}
		/* tested in ctor
			public void CacheId ()
			public void Id ()
		*/
		[Test]
		[Ignore ("Not Implemented")]
		public void Constraints ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Same as ToString Generics Issue")]
		public void DisplayName ()
		{
			var activity = new ActivityMock ();
			Assert.AreEqual ("ActivityMock", activity.DisplayName);
			activity.DisplayName = "Hello\nWorld";
			Assert.AreEqual ("Hello\nWorld", activity.DisplayName);
			activity.DisplayName = null;
			Assert.AreEqual (String.Empty, activity.DisplayName); //.NET returns String.Empty
			activity.DisplayName = String.Empty;
			Assert.AreEqual (String.Empty, activity.DisplayName); //.NET returns String.Empty
			activity.DisplayName = "Bob";
			Assert.AreEqual ("Bob", activity.DisplayName);

			var activityMockT = new ActivityMock<string> ();
			Assert.AreEqual ("ActivityMock<String>", activityMockT.DisplayName);
		}
		class ActivityMock<T> : Activity {
		}
		[Test]
		public void Implementation ()
		{
			var activity = new ActivityMock ();
			Assert.IsNull (activity.Implementation);
			var activity2 = new ActivityMock ();
			Func<Activity> imp = (() => activity2);
			activity.Implementation = imp;
			Assert.AreEqual (imp, activity.Implementation);
		}
		#endregion

		#region Methods
		// see activitymetadatatests for CacheMetadata
		[Test]
		[Ignore ("Not Implemented")]
		public void ShouldSerializeDisplayName ()
		{
			//??
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("ToString Generics Issue")]
		public void ToStringTest ()
		{
			var activity = new ActivityMock ();
			Assert.AreEqual (": ActivityMock", activity.ToString ());
			activity.DisplayName = "hello\nworld";
			string expected = String.Concat (activity.Id, ": ", "hello\nworld");
			Assert.AreEqual (expected, activity.ToString ());
			WorkflowInvoker.Invoke (activity);
			expected = String.Concat (activity.Id, ": ", "hello\nworld");
			Assert.AreEqual (expected, activity.ToString ());

			var activityMockT = new ActivityMock<string> ();
			Assert.AreEqual (": ActivityMock<String>", activityMockT.ToString ());
		}
		#endregion
	}
}

