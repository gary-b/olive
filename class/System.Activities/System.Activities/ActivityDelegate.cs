using System;
using System.Windows.Markup;
using System.Collections.Generic;

namespace System.Activities
{
	[ContentProperty ("Handler")]
	public abstract class ActivityDelegate
	{
		public string DisplayName { get; set; }
		public Activity Handler { get; set; }
		
		protected internal virtual DelegateOutArgument GetResultArgument ()
		{
			throw new NotImplementedException ();
		}
		
		protected virtual void OnGetRuntimeDelegateArguments (IList<RuntimeDelegateArgument> runtimeDelegateArguments)
		{
		}
		
		public bool ShouldSerializeDisplayName ()
		{
			throw new NotImplementedException ();
		}
		
		public override string ToString ()
		{
			throw new NotImplementedException ();
		}
	}
}
