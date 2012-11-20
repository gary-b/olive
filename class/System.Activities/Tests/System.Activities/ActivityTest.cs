using NUnit.Framework;
using System;
using System.Activities;
using System.Collections.ObjectModel;
using System.Activities.Validation;
using System.Activities.Statements;
using System.Activities.Expressions;
using System.IO;

namespace Tests.System.Activities
{
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
		public void Constructor ()
		{
			var activity = new ActivityMock ();
			Assert.IsNull (activity.Id);
			Assert.IsNotNull (activity.Constraints);
			Assert.AreEqual (0, activity.Constraints.Count);
			Assert.AreEqual (0, activity.CacheId);
		}

		/* tested in ctor
		public void CacheId ()
		{

		}
		public void Id ()
		{
		}
		*/
		[Test]
		public void Constraints ()
		{
			throw new NotImplementedException ();
		}

		[Test]
		public void DisplayName ()
		{
			var activity = new ActivityMock ();
			Assert.AreEqual (activity.GetType ().Name, activity.DisplayName);
			activity.DisplayName = "Hello\nWorld";
			Assert.AreEqual ("Hello\nWorld", activity.DisplayName);
			activity.DisplayName = null;
			Assert.AreEqual (null, activity.DisplayName); //FIXME: does this pass on .NET?
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
		public void ShouldSerializeDisplayName ()
		{
			//??
			throw new NotImplementedException ();
		}

		[Test]
		public void ToStringTest ()
		{
			var activity = new ActivityMock ();
			activity.DisplayName = "hello\nworld";
			string expected = String.Concat (activity.Id, ": ", "hello\nworld");
			Assert.AreEqual (expected, activity.ToString ());
			WorkflowInvoker.Invoke (activity);
			expected = String.Concat (activity.Id, ": ", "hello\nworld");
			Assert.AreEqual (expected, activity.ToString ());
		}

		#endregion

	}
}

