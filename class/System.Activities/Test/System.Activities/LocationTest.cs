using System;
using NUnit.Framework;
using System.Activities;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class LocationTest {
		class LocationMock : Location {
			private string value;
			public override Type LocationType {
				get { return typeof (string); }
			}
			protected override object ValueCore {
				get { return value; }
				set { this.value = (string) value; }
			}
			public string GetValueCore ()
			{
				return (string) ValueCore;
			}
		}
		[Test]
		public void Value ()
		{
			var loc = new LocationMock ();
			Assert.IsNull (loc.Value);
			loc.Value = "avalue";
			Assert.AreSame ("avalue", loc.Value);
			Assert.AreSame ("avalue", loc.GetValueCore ());
		}
		[Test, ExpectedException (typeof (InvalidCastException))]
		public void ValueEx ()
		{
			var loc = new LocationMock ();
			Assert.IsNull (loc.Value);
			loc.Value = 15;
		}
	}
}

