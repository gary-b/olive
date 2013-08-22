using System;
using System.Activities.Statements;
using System.Activities;

namespace MonoTests.System.Activities
{
	public class FlowchartTestHelper : WFTestHelper
	{
		public FlowchartTestHelper ()
		{
		}
		protected FlowStep GetNodeWriter (InArgument<string> inArg)
		{
			return new FlowStep {
				Action = new WriteLine { Text = inArg },
			};
		}
		protected FlowStep GetNodeWriter (InArgument<string> inArg, FlowNode nextNode)
		{
			return new FlowStep {
				Action = new WriteLine { Text = inArg },
				Next = nextNode
			};
		}
	}
}

