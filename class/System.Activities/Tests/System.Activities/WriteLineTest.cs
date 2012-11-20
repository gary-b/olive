using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	class WriteLineTest {
		
		[Test]
		public void Text () // FIXME: test on .NET
		{
			var writeLine = new WriteLine ();
			Assert.IsNull (writeLine.Text);
			var litStr = new Literal<string> ("Hello\nWorld");
			var argText = new InArgument<string> (litStr);
			writeLine.Text = argText;
			Assert.AreSame (argText, writeLine.Text);
			writeLine.Text = null;
			Assert.IsNull (writeLine.Text);
		}

		[Test]
		public void TextWriter () // FIXME: test on .NET
		{
			var writeLine = new WriteLine ();
			Assert.IsNull (writeLine.TextWriter);
			var sw = new StringWriter ();
			var litSw = new Literal<TextWriter> (sw);
			var argTw = new InArgument<TextWriter> (litSw); // this would exception if activity run as sw not string or value type(n => sw)
			writeLine.TextWriter = argTw;
			Assert.AreSame (argTw, writeLine.TextWriter);
			writeLine.TextWriter = null;
			Assert.IsNull (writeLine.TextWriter);
		}

		[Test]
		public void OutputsToConsoleWhenNoTextWriter () // FIXME: test on .NET
		{
			var litStr = new Literal<string> ("Hello\nWorld");
			var argText = new InArgument<string> (litStr);
			var writeLine = new WriteLine () { Text = argText };
			Assert.IsNull (writeLine.TextWriter);
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (writeLine);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, sw.ToString ());
		}

		[Test]
		public void OutputsToTextWriter () // FIXME: test on .NET
		{
			var sw = new StringWriter ();
			var argTw = new InArgument<TextWriter> (n => sw);
			var litStr = new Literal<string> ("Hello\nWorld");
			var argText = new InArgument<string> (litStr);
			var writeLine = new WriteLine () { 
				Text = argText,
				TextWriter = argTw};
			WorkflowInvoker.Invoke (writeLine);
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, sw.ToString ());
		}

	}
}
