using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class LocationReferenceTest {
		class LocRefMock : LocationReference {
			public override Location GetLocation (ActivityContext context)
			{
				throw new NotImplementedException ();
			}
			protected override string NameCore
			{
				get { return "Hello\nWorld"; }
			}
			protected override Type TypeCore
			{
				get { return typeof (Guid); }
			}
		}
		[Test]
		public void Name ()
		{
			var locRef = new LocRefMock ();
			Assert.AreEqual ("Hello\nWorld", locRef.Name); // seems to call NameCore
		}
		[Test]
		public void Type ()
		{
			var locRef = new LocRefMock ();
			Assert.AreEqual (typeof (Guid), locRef.Type); // seems to call TypeCore
		}
		[Test]
		public void ToStringTest ()
		{
			var locRef = new LocRefMock ();
			Assert.AreEqual (locRef.GetType().ToString(), locRef.ToString ());
		}
	}
}
