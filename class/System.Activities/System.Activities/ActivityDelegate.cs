using System;
using System.Windows.Markup;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Activities
{
	[ContentProperty ("Handler")]
	public abstract class ActivityDelegate
	{
		public string DisplayName { get; set; }
		public Activity Handler { get; set; }
		
		protected internal virtual DelegateOutArgument GetResultArgument ()
		{
			return null;
		}
		
		protected virtual void OnGetRuntimeDelegateArguments (IList<RuntimeDelegateArgument> runtimeDelegateArguments)
		{
			throw new NotImplementedException ();
		}
		
		public bool ShouldSerializeDisplayName ()
		{
			throw new NotImplementedException ();
		}
		
		public override string ToString ()
		{
			return DisplayName;
		}

		internal IList<RuntimeDelegateArgument> GetRuntimeDelegateArguments ()
		{
			var args = new Collection<RuntimeDelegateArgument> ();
			OnGetRuntimeDelegateArguments (args);
			return args;
		}
	}
}
