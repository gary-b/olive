using System;
using System.Activities;
using System.Activities.Statements;
using NUnit.Framework;
using System.Activities.Expressions;
using System.Collections.Generic;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class FlowSwitchTest : FlowchartTestHelper {
		[Test]
		public void Ctor ()
		{
			var switchNode = new FlowSwitch<string> ();
			Assert.IsNotNull (switchNode.Cases);
			Assert.IsNull (switchNode.Default);
			Assert.IsNull (switchNode.Expression);
		}
		[Test, ExpectedException (typeof (InvalidWorkflowException))]
		[Ignore ("Validation")]
		public void Execute_Expression_NullEx ()
		{
			//System.Activities.InvalidWorkflowException : The following errors were encountered while processing the workflow tree:
			//'Flowchart': Expression must be set before the FlowSwitch in Flowchart 'Flowchart' can be used.
			var nodeA = GetNodeWriter ("A");
			var nodeSwitch = new FlowSwitch<char> {
				Expression = null,
				Cases = {
					{ 'A', nodeA }
				}
			};
			WorkflowInvoker.Invoke (new Flowchart { StartNode = nodeSwitch });
		}
		[Test]
		public void Execute_Default_NullOk ()
		{
			var nodeA = GetNodeWriter ("A");

			var nodeSwitch = new FlowSwitch<char> {
				Expression = new Literal<char> ('c'),
				Cases = {
					{ 'A', nodeA }
				}
			};
			RunAndCompare (new Flowchart { StartNode = nodeSwitch }, String.Empty);
		}
		[Test]
		public void Execute_Cases_NullCaseWhenTNullableOk ()
		{
			var nodeA = GetNodeWriter ("was null");
			string str = null;
			//can't use Literal<> because reference types not allowed
			var expressionActivity = new CodeActivityTRunner<string> (null, (context) => {
				return str;
			});
			var nodeSwitch = new FlowSwitch<string> {
				Expression = expressionActivity,
				Cases = {
					{ null, nodeA }
				}
			};

			RunAndCompare (new Flowchart { StartNode = nodeSwitch }, "was null" + Environment.NewLine);
		}
		[Test]
		public void Cases_NullAppendedToEnd ()
		{
			var nodeSwitch = new FlowSwitch<string> {
				Cases = {
					{ "a", null },
					{ null, null },
					{ "b", null }
				}
			};
			var en = nodeSwitch.Cases.Keys.GetEnumerator ();
			var keys = new string [] { "a", "b", null };
			int i = 0;
			foreach (var key in nodeSwitch.Cases.Keys) {
				Assert.AreEqual (keys [i++], key);
			}
			i = 0;
			foreach (var kvp in nodeSwitch.Cases) {
				Assert.AreEqual (keys [i++], kvp.Key);
			}
		}
		[Test]
		public void Cases_NullKeyedItemDoesntInvalidateEnumerators ()
		{
			var node = new FlowStep ();
			var nodeSwitch = new FlowSwitch<string> {
				Cases = {
					{ null, node },
					{ "a", node }
				}
			};

			nodeSwitch.Cases.Clear ();
			nodeSwitch.Cases.Add ("str", node);
			int loop = 0;
			//Add (Key, Value)
			foreach (var kv in nodeSwitch.Cases) {
				if (loop++ == 0)
					nodeSwitch.Cases.Add (null, node);
			}
			Assert.AreEqual (2, nodeSwitch.Cases.Count);
			//Remove (Key)
			foreach (var kv in nodeSwitch.Cases) {
				nodeSwitch.Cases.Remove (null);
			}
			Assert.AreEqual (1, nodeSwitch.Cases.Count);
			// add using index and set using index
			foreach (var kv in nodeSwitch.Cases) {
				nodeSwitch.Cases [null] = node;
			}
			Assert.AreEqual (2, nodeSwitch.Cases.Count);
			nodeSwitch.Cases.Remove (null);
			loop = 0;
			//Add (Kvp)
			foreach (var kv in nodeSwitch.Cases) {
				if (loop++ == 0)
					nodeSwitch.Cases.Add (new KeyValuePair<string, FlowNode> (null, node));
			}
			//Remove (Kvp)
			foreach (var kv in nodeSwitch.Cases) {
				nodeSwitch.Cases.Remove (new KeyValuePair<string, FlowNode> (null, node));
			}
			//changing a non null keyed item does invalidate enumerator
			Exception exception = null;
			try {
				foreach (var kvp in nodeSwitch.Cases) {
					nodeSwitch.Cases ["a"] = null;
				}
			} catch (Exception ex) {
				exception = ex;
			}
			Assert.IsNotNull (exception);

			// null keyed item changes ignored for Keys and Values as well
			foreach (var key in nodeSwitch.Cases.Values) {
				nodeSwitch.Cases [null] = null;
			}
			foreach (var key in nodeSwitch.Cases.Keys) {
				nodeSwitch.Cases [null] = null;
			}
		}
		[Test]
		public void Execute_Cases_FlowNodeNullOk ()
		{
			var nodeSwitch = new FlowSwitch<char> {
				Expression = new Literal<char> ('A'),
				Cases = {
					{ 'A', null }
				},
				Default = GetNodeWriter ("default")
			};
			RunAndCompare (new Flowchart { StartNode = nodeSwitch }, String.Empty);
		}
		[Test]
		public void Execute_Cases_DupeFlowNodes ()
		{
			var nodeA = GetNodeWriter ("A");

			var nodeSwitch = new FlowSwitch<char> {
				Expression = new Literal<char> ('A'),
				Cases = {
					{ 'X', nodeA },
					{ 'A', nodeA }
				}
			};
			RunAndCompare (new Flowchart { StartNode = nodeSwitch }, "A" + Environment.NewLine);
		}
		[Test]
		public void Execute_CasesAndDefault ()
		{
			char c = ' ';
			//can't use Literal<> because the value it returns cant be changed after instantiation
			var expressionActivity = new CodeActivityTRunner<char> (null, (context) => {
				return c;
			});
			var nodeSwitch = new FlowSwitch<char> {
				Expression = expressionActivity,
				Cases = {
					{ 'A', GetNodeWriter ("A") },
					{ 'B', GetNodeWriter ("B") },
					{ 'C', GetNodeWriter ("C") }
				},
				Default = GetNodeWriter ("Default")
			};
			var flow = new Flowchart {
				StartNode = nodeSwitch,
				//Nodes = { nodeA, nodeB, nodeC, nodeDefault } // not required
			};
			c = 'A';
			RunAndCompare (flow, String.Format ("A{0}", Environment.NewLine));
			c = 'B';
			RunAndCompare (flow, String.Format ("B{0}", Environment.NewLine));
			c = 'C';
			RunAndCompare (flow, String.Format ("C{0}", Environment.NewLine));
			c = 'X';
			RunAndCompare (flow, String.Format ("Default{0}", Environment.NewLine));
		}
		[Test]
		[Ignore ("FlowChart Activity Id Order - maybe doesnt really matter")]
		public void ActivityIds ()
		{
			var nodeA = GetNodeWriter ("A");
			var nodeB = GetNodeWriter ("B");
			var nodeC = GetNodeWriter ("C");
			var nodeDefault = GetNodeWriter ("Default");

			var nodeSwitch = new FlowSwitch<char> {
				Expression = new Literal<char> ('A'),
				Cases = {
					{ 'A', nodeA },
					{ 'B', nodeB },
					{ 'C', nodeC }
				},
				Default = nodeDefault
			};

			WorkflowInvoker.Invoke (new Flowchart { StartNode = nodeSwitch });

			Assert.AreEqual ("2", nodeA.Action.Id); 
			// skips 1 each time for argument expression on WriteLine
			Assert.AreEqual ("4", nodeB.Action.Id);
			Assert.AreEqual ("6", nodeC.Action.Id);
			Assert.AreEqual ("8", nodeDefault.Action.Id);
			Assert.AreEqual ("10", nodeSwitch.Expression.Id); 
		}
		[Test]
		public void NullableDictionary ()
		{


		}
	}
}

