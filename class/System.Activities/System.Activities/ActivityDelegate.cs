using System;
using System.Windows.Markup;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Activities
{
	[ContentProperty ("Handler")]
	public abstract class ActivityDelegate
	{
		String displayName;
		bool shouldSerializeDisplayName = false;
		public string DisplayName { 
			get { return String.IsNullOrEmpty (displayName) ? GetType ().Name : displayName; } 
			set { 
				shouldSerializeDisplayName = true;
				displayName = value; 
			}
		}
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
			return shouldSerializeDisplayName;
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
