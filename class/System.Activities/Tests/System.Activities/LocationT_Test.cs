using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.IO;

namespace Tests.System.Activities {
	[TestFixture]
	class LocationT_Test {
		
		[Test]
		public void ValueAndLocationType ()
		{
			var locInt = new Location<int> ();
			Assert.AreEqual (typeof (int), locInt.LocationType);
			Assert.AreEqual (0, locInt.Value); // default for int value type
			locInt.Value = 42;
			Assert.AreEqual (42, locInt.Value);

			var sw = new StringWriter ();
			var locTw = new Location<TextWriter> ();
			Assert.AreEqual (typeof (TextWriter), locTw.LocationType);
			Assert.IsNull (locTw.Value);
			locTw.Value = sw;
			Assert.AreEqual (sw, locTw.Value);
		}
	}
}
