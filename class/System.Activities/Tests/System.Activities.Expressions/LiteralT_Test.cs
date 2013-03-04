using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Expressions;
using System.IO;
using System.Activities.Statements;

namespace Tests.System.Activities.Expressions {
	[TestFixture]
	class LiteralT_Test {
		[Test]
		public void Ctor ()
		{
			var litStr = new Literal<string> ();
			Assert.IsNull (litStr.Value);

			var litInt = new Literal<int> ();
			Assert.AreEqual (0, litInt.Value);

			var litTw = new Literal<TextWriter> ();
			Assert.IsNull (litTw.Value);
		}
		[Test]
		public void Ctor_T ()
		{
			var litStr = new Literal<string> ("Hello\nWorld");
			Assert.AreEqual ("Hello\nWorld", litStr.Value);

			var litNullStr = new Literal<string> (null);
			Assert.IsNull (litNullStr.Value);

			var litInt = new Literal<int> (42);
			Assert.AreEqual (42, litInt.Value);

			var sw = new StringWriter ();
			var litTw = new Literal<TextWriter> (sw);
			Assert.AreEqual (sw, litTw.Value);
		}
		
		#region Properties
		[Test]
		public void Value ()
		{
			var litStr = new Literal<string> ();
			Assert.IsNull (litStr.Value);
			litStr.Value = "Hello\nWorld";
			Assert.AreEqual ("Hello\nWorld", litStr.Value);
			litStr.Value = "Another";
			Assert.AreEqual ("Another", litStr.Value);
			litStr.Value = null;
			Assert.IsNull (litStr.Value);
		}
		#endregion

		#region Methods
		[Test]
		[Ignore ("Not Implemented")]
		public void CanConvertToString ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void ConvertToString ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("ShouldSerializeValue")]
		public void ShouldSerializeValue ()
		{
			// seems to be true by default?
			var litStrNoVal = new Literal<string> ();
			Assert.IsTrue (litStrNoVal.ShouldSerializeValue ());
			
			var litStrVal = new Literal<string> ("Hello\nWorld");
			Assert.IsTrue (litStrVal.ShouldSerializeValue ());

			// FIXME: what should this do?
			throw new NotImplementedException ();
		}
		[Test]
		public void ToStringTest ()
		{
			var litStrNull = new Literal<string> ();
			Assert.AreEqual ("null", litStrNull.ToString ()); // FIXME: test this passes on .NET

			var litStr = new Literal<string> ("Hello\nWorld");
			Assert.AreEqual ("Hello\nWorld", litStr.ToString ());

			var litInt = new Literal<int> (42);
			Assert.AreEqual ("42", litInt.ToString ());

			var sw = new StringWriter ();
			sw.Write ("Hello\nWorld");
			var litTw = new Literal<TextWriter> (sw);
			Assert.AreEqual ("Hello\nWorld", litTw.ToString ());
		}
		[Test]
		public void Execute ()
		{
			var litStr = new Literal<string> ("hello\nworld");
			var sw = new StringWriter ();
			Console.SetOut (sw);
			var w = new WriteLine () {
				Text = litStr
			};
			WorkflowInvoker.Invoke (w);
			Assert.AreEqual ("hello\nworld" + Environment.NewLine, sw.ToString ());

			// FIXME: test setting Result
		}
		#endregion
	}
}
