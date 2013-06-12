using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	[TestFixture]
	class WorkflowInvokerTest : WFTest {
		[Test]
		[Ignore ("Extensions")]
		public void Ctor ()
		{
			var wi = new WorkflowInvoker (new WriteLine());
			Assert.IsNotNull (wi.Extensions);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Ctor__NullEx ()
		{
			var wi = new WorkflowInvoker (null);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void Invoke_Activity_NullEx ()
		{
			WorkflowInvoker.Invoke ((Activity) null);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Invoke_workflow ()
		{
			throw new NotImplementedException ();
		}
		class WorkflowInvokerMoreTestsClass {
			public void InvokeCompleted ()
			{
				throw new NotImplementedException ();
			}
			public void Extensions ()
			{ 
				throw new NotImplementedException ();
			}
			public void BeginInvoke_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void BeginInvoke_inputs_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void BeginInvoke_timeout_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void BeginInvoke_inputs_timeout_callback_state ()
			{
				throw new NotImplementedException ();
			}
			public void EndInvoke_result ()
			{
				throw new NotImplementedException ();
			}

			public void InvokeT_workflow ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_workflow_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_workflow_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeT_workflow_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_inputs_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void Invoke_workflow_inputs_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeT_workflow_inputs_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeT_workflow_inputs_additionalOutputs_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_userState ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs_userState ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs_timeout ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_timeout_userState ()
			{
				throw new NotImplementedException ();
			}
			public void InvokeAsync_inputs_timeout_userState ()
			{
				throw new NotImplementedException ();
			}
		}
	}
}
