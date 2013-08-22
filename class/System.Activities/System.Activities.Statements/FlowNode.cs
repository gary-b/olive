using System.Collections.Generic;

namespace System.Activities.Statements
{
	public abstract class FlowNode
	{
		internal int Id { get; set; }
		internal FlowNode ()
		{
		}

		internal abstract ICollection<FlowNode> GetChildNodes ();
		internal abstract ICollection<Activity> GetActivities ();
		internal abstract void Execute (NativeActivityContext context, Flowchart flowchart);
	}
}
