using System;
using System.Activities;
using System.Activities.Statements;
using NUnit.Framework;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class FlowStepTest : FlowchartTestHelper {
		[Test]
		public void Ctor ()
		{
			var node = new FlowStep ();
			Assert.IsNull (node.Action);
			Assert.IsNull (node.Next);
		}
		[Test]
		public void Execute ()
		{
			var node3 = GetNodeWriter ("node3");
			var node2 = GetNodeWriter ("node2", node3);
			var node1 = GetNodeWriter ("node1", node2);

			var flow = new Flowchart {
				StartNode = node1,
				//Nodes =  { node1, node2, node3 } // not required
			};
			RunAndCompare (flow, String.Format ("node1{0}node2{0}node3{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_ActionNullOk ()
		{
			var node = new FlowStep ();

			WorkflowInvoker.Invoke (new Flowchart { StartNode = node });
		}
		[Test]
		[Ignore ("FlowChart Activity Id Order - maybe doesnt really matter")]
		public void ActivityIds ()
		{
			var node3 = GetNodeWriter ("node3");
			var node2 = GetNodeWriter ("node2", node3);
			var node1 = GetNodeWriter ("node1", node2);
			WorkflowInvoker.Invoke (new Flowchart { StartNode = node1 });
			Assert.AreEqual ("2", node3.Action.Id); 
			// skips 1 each time for argument expression on WriteLine
			Assert.AreEqual ("4", node2.Action.Id);
			Assert.AreEqual ("6", node1.Action.Id);
		}
	}
}

