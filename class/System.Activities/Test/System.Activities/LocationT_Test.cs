using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.IO;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class LocationT_Test {
		[Test]
		public void Ctor ()
		{
			var locInt = new Location<int> ();
			Assert.AreEqual (typeof (int), locInt.LocationType);
			Assert.AreEqual (0, locInt.Value); // default for int value type

			var locTw = new Location<TextWriter> ();
			Assert.AreEqual (typeof (TextWriter), locTw.LocationType);
			Assert.IsNull (locTw.Value); // default for reference type
		}
		[Test]
		public void Value ()
		{
			var locInt = new Location<int> ();
			locInt.Value = 42;
			Assert.AreEqual (42, locInt.Value);
			locInt.Value = 0;
			Assert.AreEqual (0, locInt.Value);

			var sw = new StringWriter ();
			var locTw = new Location<TextWriter> ();
			locTw.Value = sw;
			Assert.AreEqual (sw, locTw.Value);
			locTw.Value = null;
			Assert.IsNull (locTw.Value);
		}
		//LocationType tested in ctor
	}
}
