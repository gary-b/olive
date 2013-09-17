using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Transactions;
using System.Windows.Markup;
using System.Xaml;
using System.Xml.Linq;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Activities.Tracking;
using System.Activities.Validation;

namespace System.Activities
{
	public class NativeActivityContext : ActivityContext
	{
		// FIXME: when attempting to get or set a value for a variable which cannot be accessed .NET returns 
		// an InvalidOperationException ex. If the variable is declared in the workflow (but out of scope), 
		// it advises on which activity it has been declared.

		public BookmarkScope DefaultBookmarkScope { get { throw new NotImplementedException (); } }
		public bool IsCancellationRequested { get { return Instance.IsCancellationRequested; } }
		public ExecutionProperties Properties { get { return Instance.Properties; } }

		internal bool HasBlockingBookmarksOrAnyResumptions {
			get { return Runtime.HasBlockingBookmarksOrAnyResumptions (Instance); }
		}

		internal NativeActivityContext ()
		{
		}

		internal NativeActivityContext (ActivityInstance instance, WorkflowRuntime runtime) : base (instance, runtime)
		{
		}

		public void Abort (Exception reason)
		{
			throw new NotImplementedException ();
		}
		public void AbortChildInstance (ActivityInstance activity)
		{
			throw new NotImplementedException ();
		}
		public void CancelChild (ActivityInstance activityInstance)
		{
			Runtime.ScheduleCancel (activityInstance, Instance);
		}
		public void CancelChildren ()
		{
			Runtime.ScheduleCancelChildren (Instance);
		}
		public Bookmark CreateBookmark ()
		{
			return CreateBookmark (null, BookmarkOptions.None);
		}
		public Bookmark CreateBookmark (BookmarkCallback callback)
		{
			return CreateBookmark (callback, BookmarkOptions.None);
		}
		public Bookmark CreateBookmark (string name)
		{
			if (!Instance.Activity.InternalCanInduceIdle)
				throw new InvalidOperationException ("Activity must override CanInduceIdle to be " +
									"true if creating bookmarks"); //FIXME: err msg
	
			return CreateBookmarkAndAdd (name, null, BookmarkOptions.None);
		}
		public Bookmark CreateBookmark (BookmarkCallback callback, BookmarkOptions options)
		{
			if (!Instance.Activity.InternalCanInduceIdle)
				throw new InvalidOperationException ("Activity must override CanInduceIdle to be " +
									"true if creating bookmarks"); //FIXME: err msg
			//CreateBookmark overloads with no name do not have callback param null check
			var bookmark = new Bookmark ();
			AddBookmarkToRuntime (bookmark, callback, options);
			return bookmark;
		}
		public Bookmark CreateBookmark (string name, BookmarkCallback callback)
		{
			return CreateBookmark (name, callback, BookmarkOptions.None);
		}
		public Bookmark CreateBookmark (string name, BookmarkCallback callback, BookmarkOptions options)
		{
			if (!Instance.Activity.InternalCanInduceIdle)
				throw new InvalidOperationException ("Activity must override CanInduceIdle to be " +
									"true if creating bookmarks"); //FIXME: err msg
			if (callback == null)
				throw new ArgumentNullException ("callback");

			return CreateBookmarkAndAdd (name, callback, options);
		}
		Bookmark CreateBookmarkAndAdd (string name, BookmarkCallback callback, BookmarkOptions options)
		{
			var bookmark = new Bookmark (name);
			AddBookmarkToRuntime (bookmark, callback, options);
			return bookmark;
		}
		void AddBookmarkToRuntime (Bookmark bookmark, BookmarkCallback callback, BookmarkOptions options)
		{
			if (options != BookmarkOptions.None &&
				options != BookmarkOptions.MultipleResume && options != BookmarkOptions.NonBlocking &&
				options != (BookmarkOptions.MultipleResume | BookmarkOptions.NonBlocking))
				throw new InvalidEnumArgumentException (); //FIXME: err msg 

			var record = new BookmarkRecord (bookmark, options, callback, Instance);
			Runtime.AddBookmark (record);
		}
		public Bookmark CreateBookmark (string name, BookmarkCallback callback, BookmarkScope scope)
		{
			throw new NotImplementedException ();
		}
		public Bookmark CreateBookmark (string name, BookmarkCallback callback, BookmarkScope scope, BookmarkOptions options)
		{
			throw new NotImplementedException ();
		}
		public ReadOnlyCollection<ActivityInstance> GetChildren ()
		{
			return new ReadOnlyCollection<ActivityInstance> (Runtime.GetChildren (Instance));
		}
		public object GetValue (Variable variable)
		{
			if (variable == null)
				throw new ArgumentNullException ("variable");
			try {
				return Instance.ImplementationVariables [variable].Value;
			} catch (KeyNotFoundException ex) {
				throw new InvalidOperationException ("Variable cannot be used");
			}
		}
		public T GetValue<T> (Variable<T> variable)
		{
			return (T) GetValue ((Variable) variable);
		}
		public void MarkCanceled ()
		{
			if (!IsCancellationRequested)
				throw new InvalidOperationException ("Cannot call MarkCanceled if IsCancellationRequested false");
			Instance.MarkCanceled ();
		}
		internal void MarkCanceledBasedOnChildren ()
		{
			Instance.MarkCanceledBasedOnChildren (GetChildren ());
		}
		internal void MarkQuashSchedules ()
		{
			Instance.MarkQuashSchedules ();
		}
		public void RemoveAllBookmarks ()
		{
			Runtime.RemoveAllBookmarks (Instance);
		}
		public bool RemoveBookmark (Bookmark bookmark)
		{
			if (bookmark == null)
				throw new ArgumentNullException ("bookmark");
			return Runtime.RemoveBookmark (bookmark, Instance);
		}
		public bool RemoveBookmark (string name)
		{
			//.net throws ArgNullEx even when empty
			if (String.IsNullOrEmpty (name))
				throw new ArgumentNullException ("name");
			return RemoveBookmark (new Bookmark (name));
		}
		public bool RemoveBookmark (string name, BookmarkScope scope)
		{
			throw new NotImplementedException ();
		}
		public BookmarkResumptionResult ResumeBookmark (Bookmark bookmark, object value)
		{
			return Runtime.ResumeBookmark (bookmark, value);
		}
		public ActivityInstance ScheduleAction (ActivityAction activityAction, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			// FIXME: test
			var param = new Dictionary<string, object> ();
			return Runtime.ScheduleDelegate (activityAction, param, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleAction<T> (ActivityAction<T> activityAction, T argument, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			// FIXME: test
			var param = new Dictionary<string, object> ();
			param.Add ("Argument", argument);
			return Runtime.ScheduleDelegate (activityAction, param, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleAction<T1, T2> (ActivityAction<T1, T2> activityAction, T1 argument1, T2 argument2, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			// FIXME: test
			var param = new Dictionary<string, object> ();
			param.Add ("Argument1", argument1);
			param.Add ("Argument2", argument2);
			return Runtime.ScheduleDelegate (activityAction, param, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleAction<T1, T2, T3> (ActivityAction<T1, T2, T3> activityAction, T1 argument1, T2 argument2, T3 argument3, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			// FIXME: test
			var param = new Dictionary<string, object> ();
			param.Add ("Argument1", argument1);
			param.Add ("Argument2", argument2);
			param.Add ("Argument3", argument3);
			return Runtime.ScheduleDelegate (activityAction, param, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4> (ActivityAction<T1, T2, T3, T4> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5> (ActivityAction<T1, T2, T3, T4, T5> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6> (ActivityAction<T1, T2, T3, T4, T5, T6> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7> (ActivityAction<T1, T2, T3, T4, T5, T6, T7> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> (ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15, T16 argument16, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleActivity (Activity activity)
		{
			return ScheduleActivity (activity, (CompletionCallback) null);
		}
		public ActivityInstance ScheduleActivity (Activity activity, CompletionCallback onCompleted)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");

			return Runtime.ScheduleActivity (activity, Instance, onCompleted, null);
		}
		//FIXME: callback delegate validation
		public ActivityInstance ScheduleActivity (Activity activity, FaultCallback onFaulted)
		{
			return Runtime.ScheduleActivity (activity, Instance, null, onFaulted);
		}
		public ActivityInstance ScheduleActivity (Activity activity, CompletionCallback onCompleted, FaultCallback onFaulted)
		{
			return Runtime.ScheduleActivity (activity, Instance, onCompleted, onFaulted);
		}
		public ActivityInstance ScheduleActivity<TResult> (Activity<TResult> activity, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");

			return Runtime.ScheduleActivity (activity, Instance, onCompleted, onFaulted);
		}
		public ActivityInstance ScheduleDelegate (ActivityDelegate activityDelegate, IDictionary<string, Object> inputParameters, DelegateCompletionCallback onCompleted = null, FaultCallback onFaulted = null)
		{
			return Runtime.ScheduleDelegate (activityDelegate, inputParameters, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleFunc<TResult> (ActivityFunc<TResult> activityFunc, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			return Runtime.ScheduleDelegate (activityFunc, null, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleFunc<T, TResult> (ActivityFunc<T, TResult> activityFunc, T argument, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			var param = new Dictionary<string, object> ();
			param.Add ("Argument", argument);
			return Runtime.ScheduleDelegate (activityFunc, param, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleFunc<T1, T2, TResult> (ActivityFunc<T1, T2, TResult> activityFunc, T1 argument1, T2 argument2, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			var param = new Dictionary<string, object> ();
			param.Add ("Argument1", argument1);
			param.Add ("Argument2", argument2);
			return Runtime.ScheduleDelegate (activityFunc, param, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, TResult> (ActivityFunc<T1, T2, T3, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			var param = new Dictionary<string, object> ();
			param.Add ("Argument1", argument1);
			param.Add ("Argument2", argument2);
			param.Add ("Argument3", argument3);
			return Runtime.ScheduleDelegate (activityFunc, param, onCompleted, onFaulted, Instance);
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, TResult> (ActivityFunc<T1, T2, T3, T4, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, TResult> (ActivityFunc<T1, T2, T3, T4, T5, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> (ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15, T16 argument16, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
		{
			throw new NotImplementedException ();
		}
		public void SetValue (Variable variable, object value)
		{
			if (variable == null)
				throw new ArgumentNullException ("variable");
			try {
				Instance.ImplementationVariables [variable].Value = value;
			} catch (KeyNotFoundException ex) {
				throw new InvalidOperationException ("Variable cannot be used");
			}
		}
		public void SetValue<T> (Variable<T> variable, T value)
		{
			SetValue ((Variable) variable, value);
		}
		public void Track (CustomTrackingRecord record)
		{
			throw new NotImplementedException ();
		}
	}
}
