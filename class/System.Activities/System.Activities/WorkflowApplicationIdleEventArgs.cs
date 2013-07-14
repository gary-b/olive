using System;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace System.Activities
{
	public class WorkflowApplicationIdleEventArgs : WorkflowApplicationEventArgs
	{
		ReadOnlyCollection<BookmarkInfo> bookmarks;
		internal WorkflowApplicationIdleEventArgs (WorkflowApplication application) 
			:base (application)
		{
			bookmarks = application.GetBookmarks ();
		}

		public ReadOnlyCollection<BookmarkInfo> Bookmarks { get { return bookmarks; } }
	}
}
