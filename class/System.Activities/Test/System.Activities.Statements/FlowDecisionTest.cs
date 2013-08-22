using System;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class FlowDecisionTest : FlowchartTestHelper {
		[Test]
		public void Ctor ()
		{
			var decision = new FlowDecision ();
			Assert.IsNull (decision.Condition);
			Assert.IsNull (decision.True);
			Assert.IsNull (decision.False);
		}
		[Test]
		public void Ctor_ActivityT ()
		{
			var lit = new Literal<bool> (true);
			var decision = new FlowDecision (lit);
			Assert.AreSame (lit, decision.Condition);
			Assert.IsNull (decision.True);
			Assert.IsNull (decision.False);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor_ActivityTNullEx ()
		{
			new FlowDecision ((Activity<bool>) null);
		}
		[Test]
		[Ignore ("NotImplemented")]
		public void Ctor_Expression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Execute ()
		{
			var nodeTrue = GetNodeWriter ("true");
			var nodeFalse = GetNodeWriter ("false");

			bool decider = false;
			var decisionActivity = new CodeActivityTRunner<bool> (null, (context) => {
				return decider;
			});
			var nodeDecision = new FlowDecision {
				Condition = decisionActivity,
				True = nodeTrue,
				False = nodeFalse
			};
			var flow = new Flowchart {
				StartNode = nodeDecision,
				//Nodes = { nodeDecision, nodeTrue, nodeFalse } // not required
			};
			decider = true;
			RunAndCompare (flow, String.Format ("true{0}", Environment.NewLine));
			decider = false;
			RunAndCompare (flow, String.Format ("false{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_TrueAndFalse_NullOk ()
		{
			var decision = new FlowDecision (new Literal<bool> (true));
			WorkflowInvoker.Invoke (new Flowchart { StartNode = decision });
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_Condition_NullEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'Flowchart': Condition must be set before the FlowDecision in Flowchart 'Flowchart' can be used.
			var decision = new FlowDecision ();
			WorkflowInvoker.Invoke (new Flowchart { StartNode = decision });
		}
		[Test]
		public void Execute_TrueAndFalse_DupesOk ()
		{
			var nodeTrue = GetNodeWriter ("true");

			var decision = new FlowDecision (new Literal<bool> (false));
			decision.True = nodeTrue;
			decision.False = nodeTrue;
			RunAndCompare (new Flowchart { StartNode = decision }, "true" + Environment.NewLine);
		}
		[Test]
		[Ignore ("FlowChart Activity Id Order - maybe doesnt really matter")]
		public void ActivityIds ()
		{
			var nodeTrue = GetNodeWriter ("true");
			var nodeFalse = GetNodeWriter ("false");

			var decision = new FlowDecision (new Literal<bool> (true));
			decision.True = nodeTrue;
			decision.False = nodeFalse;

			WorkflowInvoker.Invoke (new Flowchart { StartNode = decision });

			Assert.AreEqual ("2", nodeTrue.Action.Id); 
			// skips 1 each time for argument expression on WriteLine
			Assert.AreEqual ("4", nodeFalse.Action.Id);
			Assert.AreEqual ("6", decision.Condition.Id);
		}
	}
}

