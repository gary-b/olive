using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Transactions;
using System.Activities;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Hosting;
using System.Windows.Markup;

namespace System.Activities.Statements
{
	[ContentProperty ("Text")]
	public sealed class WriteLine : CodeActivity
	{
		public InArgument<string> Text { get; set; }
		public InArgument<TextWriter> TextWriter { get; set; }

		protected override void CacheMetadata (CodeActivityMetadata metadata)
		{
			var rtText = new RuntimeArgument ("Text", typeof (string), ArgumentDirection.In);
			metadata.AddArgument (rtText);
			if (Text == null)
				Text = new InArgument<string> ();
			metadata.Bind (Text, rtText);
			var rtTextWriter = new RuntimeArgument ("TextWriter", typeof (TextWriter), ArgumentDirection.In);
			metadata.AddArgument (rtTextWriter);
			if (TextWriter == null)
				TextWriter = new InArgument<TextWriter> ();
			metadata.Bind (TextWriter, rtTextWriter);
		}

		protected override void Execute (CodeActivityContext context)
		{
			var tw = TextWriter.Get<TextWriter> (context);
			var text = Text.Get<string> (context);

			if (tw == null)
				Console.WriteLine (text);
			else
				tw.WriteLine (text);

			// (the above WriteLine methods can handle nulls)
		}
	}
}
