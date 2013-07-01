using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Activities
{
	public class WorkflowApplicationEventArgs : EventArgs
	{
		internal WorkflowApplication Application { get; set; }
		public Guid InstanceId { 
			get { return Application.Id; } 
		}

		internal WorkflowApplicationEventArgs (WorkflowApplication application)
		{
			Application = application;
		}

		public IEnumerable<T> GetInstanceExtensions<T>() where T : class
		{
			throw new NotImplementedException ();
		}
	}
}
