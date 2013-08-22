using System;
using NUnit.Framework;
using System.Activities.Statements;
using System.Activities;
using System.Activities.Expressions;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class FlowchartTest : FlowchartTestHelper {
		[Test]
		public void Ctor ()
		{
			var flow = new Flowchart ();
			Assert.IsNotNull (flow.Nodes);
			Assert.IsNull (flow.StartNode);
			Assert.IsNotNull (flow.Variables);
		}
		[Test]
		public void Execute_Variables ()
		{
			var v1Str = new Variable<string> ("", "v1");
			var node2 = GetNodeWriter (v1Str);
			var node1 = GetNodeWriter (v1Str, node2);

			var flow = new Flowchart {
				Variables = { v1Str },
				StartNode = node1,
				//Nodes =  { node1, node2 } // not required
			};
			RunAndCompare (flow, String.Format ("v1{0}v1{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_Nodes_NotDeclaredOk ()
		{
			var node2 = GetNodeWriter ("node2");
			var node1 = GetNodeWriter ("node1", node2);
			var flow = new Flowchart {
				StartNode = node1,
				Nodes =  { }
			};
			RunAndCompare (flow, String.Format ("node1{0}node2{0}", Environment.NewLine));
		}
		[Test]
		public void Execute_Nodes_DeclaredButNotUsedOk ()
		{
			var node2 = GetNodeWriter ("node2");
			var node1 = GetNodeWriter ("node1"); //no link to node2

			var flow = new Flowchart {
				StartNode = node1,
				Nodes =  { node1, node2 }
			};
			RunAndCompare (flow, String.Format ("node1{0}", Environment.NewLine));
		}
		[Test]
		[ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_StartNode_NullEx ()
		{
			var node = GetNodeWriter ("node");
			var flow = new Flowchart {
				//StartNode = node1,
				Nodes =  { node }
			};
			WorkflowInvoker.Invoke (flow);
		}
		[Test]
		[ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_ReuseFlowNodesEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'Flowchart': FlowNode cannot be shared across different Flowcharts. It is already in Flowchart 'Flowchart' and cannot be used in Flowchart 'Flowchart'.
			var node = GetNodeWriter ("node");
			var flow1 = new Flowchart {
				StartNode = node,
				//Nodes =  { node }
			};
			var flow2 = new Flowchart {
				StartNode = node,
				//Nodes =  { node }
			};
			var wf = new Sequence {
				Activities = {
					flow1,
					flow2
				}
			};
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		[ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_ReuseFlowNodes_HiddenEx ()
		{
			//System.Activities.InvalidWorkflowException : : The following errors were encountered while processing the workflow tree:
			//'Flowchart': FlowNode cannot be shared across different Flowcharts. It is already in Flowchart 'Flowchart' and cannot be used in Flowchart 'Flowchart'.
			var sharedNode =  GetNodeWriter ("shared");
			var node1 = GetNodeWriter ("node1", sharedNode);
			var node2 = GetNodeWriter ("node2", sharedNode);
			var flow1 = new Flowchart {
				StartNode = node1,
			};
			var flow2 = new Flowchart {
				StartNode = node2,
			};
			var wf = new Sequence {
				Activities = {
					flow1,
					flow2
				}
			};
			WorkflowInvoker.Invoke (wf);
		}
		[Test]
		[Ignore ("FlowChart Activity Id Order - maybe doesnt really matter")]
		public void ActivityIds ()
		{
			var nodeA = GetNodeWriter ("A");
			var nodeB = GetNodeWriter ("B");
			var nodeC = GetNodeWriter ("C");
			var nodeDefault = GetNodeWriter ("Default");

			var nodeTrueSwitch = new FlowSwitch<char> {
				Expression = new Literal<char> ('A'),
				Cases = {
					{ 'A', nodeA },
					{ 'B', nodeB },
					{ 'C', nodeC }
				},
				Default = nodeDefault
			};

			var nodeFalse = GetNodeWriter ("false");

			var decision = new FlowDecision (new Literal<bool> (true));
			decision.True = nodeTrueSwitch;
			decision.False = nodeFalse;

			var nodeStep3 = GetNodeWriter ("node3", decision);
			var nodeStep2 = GetNodeWriter ("node2", nodeStep3);
			var nodeStep1 = GetNodeWriter ("node1", nodeStep2);

			WorkflowInvoker.Invoke (new Flowchart { StartNode = nodeStep1 });

			Assert.AreEqual ("2", nodeA.Action.Id); 
			Assert.AreEqual ("4", nodeB.Action.Id);
			Assert.AreEqual ("6", nodeC.Action.Id);
			Assert.AreEqual ("8", nodeDefault.Action.Id);
			Assert.AreEqual ("10", nodeTrueSwitch.Expression.Id);

			Assert.AreEqual ("11", nodeFalse.Action.Id);
			Assert.AreEqual ("13", decision.Condition.Id);

			Assert.AreEqual ("14", nodeStep3.Action.Id); 
			Assert.AreEqual ("16", nodeStep2.Action.Id); 
			Assert.AreEqual ("18", nodeStep1.Action.Id); 
		}
		[Test]
		public void FaultHandling ()
		{
			var nodeStep3 = GetNodeWriter ("node3");
			var nodeStep2 = new FlowStep { Action = new HelloWorldEx (), Next = nodeStep3 };
			var nodeStep1 = GetNodeWriter ("node1", nodeStep2);

			var flowchart = new Flowchart {
				StartNode = nodeStep1
			};
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddChild (flowchart);
			}, (context) => {
				context.ScheduleActivity (flowchart, (ctx, ex, compAi) => {
					Console.WriteLine ("faultHandled");
					ctx.HandleFault ();
				});
			});
			RunAndCompare (wf, String.Format ("node1{0}faultHandled{0}node3{0}", Environment.NewLine));
		}
		[Test]
		public void IntroducesNoExecutionProperties ()
		{
			var execWriter = new NativeActivityRunner (null, (context) => {
				foreach (var prop in context.Properties)
					Console.WriteLine (prop.Key + " : " + prop.Value.GetType().ToString());
			});
			var nodeStep1 = new FlowStep { Action = execWriter };

			var flowchart = new Flowchart {
				StartNode = nodeStep1
			};

			RunAndCompare (flowchart, String.Empty);
		}
	}
}

