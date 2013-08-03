using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Threading;
using System.Activities;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Tracking;

namespace System.Activities.Hosting
{
	public sealed class WorkflowInstanceProxy
	{
		//FIXME: implementation of WorkflowInstanceProxy is hackish to bypass need for WorkflowInstance
		internal WorkflowInstanceProxy (
			Func<Bookmark, object, TimeSpan, AsyncCallback, object, IAsyncResult> onBeginResumeBookmark,
			Func<IAsyncResult, BookmarkResumptionResult> onEndResumeBookmark, Guid id, Activity workflowDefinition)
		{
			if (onBeginResumeBookmark == null)
				throw new ArgumentNullException ("onBeginResumeBookmark");
			if (onEndResumeBookmark == null)
				throw new ArgumentNullException ("onEndResumeBookmark");
			if (workflowDefinition == null)
				throw new ArgumentNullException ("workflowDefinition");

			OnBeginResumeBookmark = onBeginResumeBookmark;
			OnEndResumeBookmark = onEndResumeBookmark;
			Id = id;
			WorkflowDefinition = workflowDefinition;
		}

		Func<IAsyncResult, BookmarkResumptionResult> OnEndResumeBookmark { get; set; }
		Func<Bookmark, object, TimeSpan, AsyncCallback, object, IAsyncResult> OnBeginResumeBookmark { get; set; }
		public Guid Id { get; private set; }
		public Activity WorkflowDefinition { get; private set; }

		public IAsyncResult BeginResumeBookmark (Bookmark bookmark, object value, AsyncCallback callback, object state)
		{
			//FIXME: test to check default timeout is 30 secs
			return OnBeginResumeBookmark (bookmark, value, new TimeSpan (0, 0, 30), callback, state);
		}
		public IAsyncResult BeginResumeBookmark (Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return OnBeginResumeBookmark (bookmark, value, timeout, callback, state);
		}
		public BookmarkResumptionResult EndResumeBookmark (IAsyncResult result)
		{
			return OnEndResumeBookmark (result);
		}
	}
}
