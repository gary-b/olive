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
	[ContentProperty ("Nodes")]
	public sealed class Flowchart : NativeActivity
	{
		/* A flowchart is made up of FlowNode objects, which are themselves NOT activities. The user
		 * defines a flowchart in WF by creating a graph of these FlowNodes and specifying links between them.
		 * The FlowNode subclasses include
		 * 	FlowStep: user specifies an Activity to execute and the next FlowNode to move to
		 * 	FlowDecision: user specifies an Activity<bool> to execute and FlowNodes to move to for a true or false result
		 * 	FlowSwitch<T>: user specifies an Activity<T> to execute and FlowNodes to move to for different return values of type T
		 * Nodes are contained within a Flowchart Activity. This orchastrates the execution of the FlowNodes. 
		 * When its CacheMetadata is run it retrieves all the Activities referenced in FlowNodes 
		 * and adds them to metadata as its own children. 
		 * When it is executed, it starts processing the FlowNodes starting with that referenced in StartNode.
		 * 
		 * Each FlowNode has some logic to process when it is entered, namely scheduling an Activity,
		 * and some logic to process once that activity has completed, namely determining which FlowNode should 
		 * be processed next.
		 * 
		 * FIXME: The implementation is currently a bit messy, calling into the FlowNode class to execute 
		 * entering logic (passing the context as param) while the FlowNodes pass a method on the Flowchart
		 * to callback to which implements the completion logic. Flowchart has a callback method for each type 
		 * of FlowNode. The cause relates to a) ms.net's validation of callback delegates to make sure target object 
		 * is the parent activity, b) the fact FlowSwitch is generic and needs to call ScheduleActivity<T> and 
		 * c) the need to access other FlowNodes.
		*/
		public Collection<FlowNode> Nodes { get; private set; }
		public FlowNode StartNode { get; set; }
		public Collection<Variable> Variables { get; private set; }

		Variable<int> CurrentNodeId { get; set; }
		List<FlowNode> FoundNodes { get; set; }

		public Flowchart ()
		{
			Nodes = new Collection<FlowNode> ();
			Variables = new Collection<Variable> ();
			CurrentNodeId = new Variable<int> ("", 0);
			FoundNodes = new List<FlowNode> ();
		}
		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			foreach (var v in Variables)
				metadata.AddVariable (v);
			metadata.AddImplementationVariable (CurrentNodeId);
			var activities = new List<Activity> ();
			var nodes = new Collection<FlowNode> ();
			int id = 1;
			ProcessFlowNode (StartNode, ref id, activities, nodes);
			//FIXME: potential thread safety issue? multiple workflow instances using this .net class instance 
			//will be sharing FoundNodes
			//FIXME: works on premise that workflow (flowchart) definition doesnt change after metadata 
			//1st retrieved
			lock (FoundNodes) {
				FoundNodes.Clear ();
				FoundNodes.AddRange (nodes);
			}
			foreach (var a in activities)
				metadata.AddChild (a);
		}
		void ProcessFlowNode (FlowNode node, ref int id, List<Activity> activities, Collection<FlowNode> nodes)
		{
			node.Id = id++;
			activities.AddRange (node.GetActivities ());
			nodes.Add (node);
			foreach (var childNode in node.GetChildNodes ()) {
				if (!nodes.Contains (childNode)) //nodes can have more than 1 parent
					ProcessFlowNode (childNode, ref id, activities, nodes);
			}
		}
		protected override void Execute (NativeActivityContext context)
		{
			ExecuteFlowNode (StartNode, context);
		}
		void ExecuteFlowNode (FlowNode node, NativeActivityContext context)
		{
			CurrentNodeId.Set (context, node.Id);
			node.Execute (context, this);
		}
		T GetCurrentNode<T> (NativeActivityContext context) where T : FlowNode
		{
			return (T) FoundNodes.Single (n => n.Id == CurrentNodeId.Get (context));
		}
		internal void FlowStepCallback (NativeActivityContext context, ActivityInstance compAI)
		{
			var node = GetCurrentNode<FlowStep> (context);
			if (node.Next != null)
				ExecuteFlowNode (node.Next, context);
		}
		internal void FlowDecisionCallback (NativeActivityContext context, ActivityInstance compAI, bool result)
		{
			var node = GetCurrentNode<FlowDecision> (context);
			if (result) {
				if (node.True != null)
					ExecuteFlowNode (node.True, context);
			} else if (node.False != null) {
				ExecuteFlowNode (node.False, context);
			}
		}
		internal void FlowSwitchCallback<T> (NativeActivityContext context, ActivityInstance compAI, T result)
		{
			var node = GetCurrentNode<FlowSwitch<T>> (context);
			if (node.Cases.ContainsKey (result)) {
				if (node.Cases [result] != null)
					ExecuteFlowNode (node.Cases [result], context);
			} else if (node.Default != null) {
				ExecuteFlowNode (node.Default, context);
			}
		}
	}
}
