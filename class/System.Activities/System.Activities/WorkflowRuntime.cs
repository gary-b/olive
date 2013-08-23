using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Activities.Hosting;

namespace System.Activities {
	internal class WorkflowRuntime {
		Activity WorkflowDefinition { get; set; }
		IList<Task> TaskList { get; set; }
		ICollection<Metadata> AllMetadata { get; set; }
		int CurrentInstanceId { get; set; }
		ActivityInstance RootInstance { get; set; }
		Exception TerminateReason { get; set; }
		Exception AbortReason { get; set; }

		internal WorkflowInstanceProxy WorkflowInstanceProxy { get; set; }
		List<KeyValuePair<Type, object>> ExtensionBank { get; set; }
		IDictionary<Type, object> RetrievedExtensions { get; set; }
		List<IDisposable> ExtensionsToDispose { get; set; }

		List<BookmarkRecord> ActiveBookmarks { get; set; }
		Queue<BookmarkResumption> BookmarkResumptionQueue { get; set; }

		internal RuntimeState RuntimeState { get; set;}
		internal Action NotifyPaused { get; set; }
		internal Action<Exception, Activity, string> UnhandledException { get; set; }

		internal WorkflowRuntime (Activity baseActivity)
		{
			if (baseActivity == null)
				throw new ArgumentNullException ("baseActivity");

			WorkflowDefinition = baseActivity;
			AllMetadata = new Collection<Metadata> ();
			TaskList = new List<Task> ();
			RuntimeState = RuntimeState.Ready;
			TerminateReason = null;
			AbortReason = null;
			ActiveBookmarks = new List<BookmarkRecord> ();
			BookmarkResumptionQueue = new Queue<BookmarkResumption> ();
			ExtensionBank = new List<KeyValuePair<Type, object>> ();
			RetrievedExtensions = new Dictionary<Type, object> ();
			ExtensionsToDispose = new List<IDisposable> ();
		}
		internal void RegisterExtensionManager (WorkflowInstanceExtensionManager extman)
		{
			if (AllMetadata.Count == 0) {
				int i = 1;
				BuildCache (WorkflowDefinition, String.Empty, ref i, null, false);
			}
			if (extman == null)
				return;

			extman.MakeReadOnly ();
			AddExtensionsToBank (extman.ExtensionObjects, false);
			var hostProvided = extman.ExtensionProviders.Select (p => p ()).ToList ();
			AddExtensionsToBank (hostProvided, true);
			//extensions coming from Metadata are pre processed before being added to ExtensionBank
			var mdProviders = AllMetadata.SelectMany (
				md => md.DefaultExtensionProviders.Select (
				p => new KeyValuePair<Type, Func<object>> (
				p.GetType ().GetGenericArguments () [0], p))).ToList ();
			//keep 1st of extensions with same type added through metadata
			mdProviders = mdProviders.GroupBy (kvp => kvp.Key).Select(grp => grp.First()).ToList ();
			//remove extensions were other extensions of derived type have been added through metadata
			var dupeWithSelf = mdProviders.Where (
				p => mdProviders.Any (
				p2 => p.Value != p2.Value && p.Key.IsAssignableFrom (p2.Key)))
				.Select (kvp => kvp.Value).ToList ();
			mdProviders.RemoveAll (p => dupeWithSelf.Contains (p.Value));
			//remove extensions where same or derived type added through host
			var dupeWithHost = mdProviders.Where (
				p => ExtensionBank.Any (
				h => p.Key.IsAssignableFrom (h.Key)))
				.Select (kvp => kvp.Value).ToList ();
			mdProviders.RemoveAll (p => dupeWithHost.Contains (p.Value));
			var mdProvided = mdProviders.Select (p => p.Value ()).ToList ();
			AddExtensionsToBank (mdProvided, true);
		}
		void AddExtensionsToBank (IEnumerable<object> exts, bool dispose)
		{
			ExtensionBank.AddRange (exts.Select (o => new KeyValuePair<Type, Object> (o.GetType (), o)));
			if (dispose)
				RecordDisposableExtensions (exts);
			foreach (var ext in exts.Where (o => o is IWorkflowInstanceExtension)) {
				var thisExts = ((IWorkflowInstanceExtension) ext).GetAdditionalExtensions ();
				if (thisExts != null) {
					var list = thisExts.ToList ();
					list.RemoveAll (e => e == null);
					AddExtensionsToBank (list, dispose);
				}
			}
		}
		void RecordDisposableExtensions (IEnumerable<object> exts)
		{
			ExtensionsToDispose.AddRange (exts.Where (e => e is IDisposable).Cast<IDisposable> ());
		}
		internal bool HasExecutingChildren (ActivityInstance instance)
		{
			return TaskList.Any (t => t.Instance.ParentInstance == instance && 
			              t.Instance.State == ActivityInstanceState.Executing);
		}
		internal void Initialize (IDictionary<string, Object> inputs, IList<Handle> ExecutionProps)
		{
			if (AllMetadata.Count == 0) {
				int i = 1;
				BuildCache (WorkflowDefinition, String.Empty, ref i, null, false);
			}

			RootInstance = AddNextAndInitialise (new Task (WorkflowDefinition), null);

			//FIXME: handling of WorkflowInstanceProxy is hackish to bypass need for WorkflowInstance
			var iWIEs = ExtensionBank.Where (e => e.Value is IWorkflowInstanceExtension).ToList ();
			if (iWIEs.Count > 0) {
				if (WorkflowInstanceProxy == null)
					throw new InvalidOperationException ("Must set WorkflowInstanceProxy");
				foreach (var ext in iWIEs)
					((IWorkflowInstanceExtension) ext.Value).SetInstance (WorkflowInstanceProxy);
			}

			if (inputs == null)
				return;

			var inArgs = RootInstance.RuntimeArguments
				.Where ((kvp) => kvp.Key.Direction == ArgumentDirection.In 
					|| kvp.Key.Direction == ArgumentDirection.InOut)
					.ToDictionary ((kvp)=> kvp.Key.Name, (kvp)=>kvp.Value);

			foreach (var input in inputs) {
				if (inArgs.ContainsKey (input.Key))
					inArgs [input.Key].Value = input.Value;
				else 
					throw new ArgumentException ("Key " + input.Key + " in input values not found " +
								     "on Activity " + WorkflowDefinition.ToString (), 
								     "inputs"); //FIXME: error msg
			}
		}
		internal T GetExtension<T> ()
		{
			//default of a Kvp<Type, object> is a Kvp with null props as its a value type
			var ext = RetrievedExtensions.SingleOrDefault (e => e.Key == typeof (T)).Value;
			if (ext == null) {
				ext = ExtensionBank.FirstOrDefault (e => typeof (T).IsAssignableFrom (e.Key)).Value;
				if (ext != null)
					RetrievedExtensions.Add (typeof (T), ext);
			}
			return (T) ext;
		}
		internal IEnumerable<T> GetExtensions<T> ()
		{
			//RetrievedExtensions will have 1 or 0 that match, but need result in list form anyway
			var exts = RetrievedExtensions.Where (e => e.Key == typeof (T)).Select (kvp => kvp.Value).ToList ();
			if (exts.Count == 0)
				exts = ExtensionBank.Where (e => typeof (T).IsAssignableFrom (e.Key)).Select (kvp => kvp.Value).ToList ();
			return exts.Cast<T> ().ToList ();
		}
		internal void DiposeExtensions ()
		{
			ExtensionBank.Clear ();
			RetrievedExtensions.Clear ();
			ExtensionsToDispose.ForEach (e => e.Dispose ());
			ExtensionsToDispose.Clear ();
		}
		internal ActivityInstanceState GetCompletionState ()
		{	
			return RootInstance.State;//FIXME: presuming its not the last to run that we're after
		}
		internal ActivityInstanceState GetCompletionState (out IDictionary<string, object> outputs, 
								   out Exception terminationException)
		{
			if (RuntimeState == RuntimeState.CompletedSuccessfully) {
				var outArgs = RootInstance.RuntimeArguments
					.Where ((kvp) => kvp.Key.Direction == ArgumentDirection.Out 
						|| kvp.Key.Direction == ArgumentDirection.InOut)
						.ToDictionary ((kvp) => kvp.Key.Name, (kvp) => kvp.Value.Value);

				if (outArgs.Count > 0)
					outputs = outArgs;
				else 
					outputs = null;
				terminationException = null;
			} else if (RuntimeState == RuntimeState.Terminated) {
				terminationException = TerminateReason;
				outputs = null;
			} else {
				outputs = null;
				terminationException = null;
			}
			return RootInstance.State; //FIXME: presuming its not the last to run that we're after
		}
		internal Exception GetAbortReason ()
		{
			if (RuntimeState == RuntimeState.Aborted)
				return AbortReason;
			else 
				return null;
		}
		internal void Terminate (Exception reason)
		{
			TerminateReason = reason;
			TaskList.Clear ();
			RootInstance.State = ActivityInstanceState.Faulted;
			RuntimeState = RuntimeState.Terminated;
		}
		internal void Abort (Exception reason)
		{
			AbortReason = reason;
			TaskList.Clear ();
			RuntimeState = RuntimeState.Aborted;
		}
		internal void Abort ()
		{
			Abort (null);
		}
		internal ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance, 
							    CompletionCallback onComplete, FaultCallback onFaulted)
		{
			return RuntimeScheduleActivity (activity, parentInstance, onComplete, onFaulted);
		}
		internal ActivityInstance ScheduleActivity<TResult> (Activity<TResult> activity, ActivityInstance parentInstance, 
								     CompletionCallback<TResult> onComplete, FaultCallback onFaulted)
		{
			return RuntimeScheduleActivity (activity, parentInstance, onComplete, onFaulted);
		}
		ActivityInstance RuntimeScheduleActivity (Activity activity, ActivityInstance parentInstance, 
						   Delegate onComplete, FaultCallback onFaulted)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			if (parentInstance == null)
				throw new ArgumentNullException ("parentInstance");

			var task = new Task (activity);
			task.CompletionCallback = onComplete;
			task.FaultCallback = onFaulted;
			return AddNextAndInitialise (task, parentInstance);
		}
		internal ActivityInstance ScheduleDelegate (ActivityDelegate activityDelegate, 
							    IDictionary<string, object> param,
							    Delegate onCompleted,
							    FaultCallback onFaulted,
							    ActivityInstance parentInstance)
		{
			// FIXME: test how .net handles nulls, param entries that dont match, are omitted etc
			if (activityDelegate == null)
				throw new ArgumentNullException ("activityDelegate");
			if (parentInstance == null)
				throw new ArgumentNullException ("parentInstance");

			/* still return activityInstance when handler empty like .NET
			// (only tested this with ScheduleAction)
			if (activityDelegate.Handler == null) {
				return new ActivityInstance (AnActivityGoesHere, "0", true, 
							     ActivityInstanceState.Closed, parentInstance);
			}*/

			var task = new Task (activityDelegate.Handler);
			task.CompletionCallback = onCompleted;
			task.FaultCallback = onFaulted;
			var instance = AddNextAndInitialise (task, parentInstance);
			int pCount = (param == null) ? 0 : param.Count;
			var expectedArguments = instance.RuntimeDelegateArguments.Where (kvp => kvp.Key.Direction == ArgumentDirection.In);
			int expectedCount = expectedArguments.Count (kvp => kvp.Key.Direction == ArgumentDirection.In);

			if (pCount != expectedCount) {
				throw new ArgumentException (String.Format (
					"The supplied input parameter count {0} does not match the expected count of {1}.",
					pCount, expectedCount), "param");
			}
			foreach (var expectedKvp in expectedArguments) {
				try {
					var pPair = param.Where (pKvp => pKvp.Key == expectedKvp.Key.Name).Single ();
					if (!expectedKvp.Key.Type.IsAssignableFrom (pPair.Value.GetType ())) {
						throw new ArgumentException (String.Format (
							"Expected an input parameter value of type '{0}' for parameter named '{1}'.",
							expectedKvp.Key.Type.Name, expectedKvp.Key.Name), "param");
					}
					expectedKvp.Value.Value = pPair.Value; //expectedKvp.Value is a Location, set its Value
				} catch (InvalidOperationException ex) {
					throw new ArgumentException (String.Format (
						"Expected input parameter named '{0}' was not found.",
						expectedKvp.Key.Name), "param");
				}
			}
			return instance;
		}
		internal BookmarkResumptionResult ScheduleHostBookmarkResumption (Bookmark bookmark, object value)
		{
			//FIXME: check for idle status
			var result = ResumeBookmark (bookmark, value);
			if (result == BookmarkResumptionResult.Success)
				RuntimeState = RuntimeState.Ready; //FIXME: best state? Same as before wf ran 
			return result;
		}
		internal void CancelChildren (ActivityInstance instance)
		{
			//FIXME: temporary and very incorrect implementation so StateMachine can be implemented
			var childTasks = new Collection<Task> ();
			GetDescendantTasks (TaskList.Single (t => t.Instance == instance), childTasks);

			foreach (var t in childTasks.Reverse ()) { //FIXME: test order of prop unregisters
				RemoveBookmarksAndResumptions (t);
				t.Instance.Properties.Unregister (true);
				t.Instance.State = ActivityInstanceState.Faulted;
				TaskList.Remove (t);
			}
		}
		ActivityInstance AddNextAndInitialise (Task task, ActivityInstance parentInstance)
		{
			// will be the next run
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Uninitialized)
				throw new InvalidOperationException ("task already initialized");
			TaskList.Add (task);
			return Initialise (task, parentInstance);
		}
		void Remove (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Ran)
				throw new InvalidOperationException ("task not ran");

			TaskList.Remove (task);
		}
		Task GetNext ()
		{
			int idx = TaskList.Count -1;
			// skip Tasks with activities that are blocked  (This includes Activities with an active bookmark
			// that is blocking and Activities with a pending bookmark resumption regardless of whether it was blocking)
			while (idx >= 0 && TaskList [idx].State == TaskState.Ran && IsBlocked (TaskList [idx]))
				idx--;
			if (idx == -1)
				return null;
			else 
				return TaskList [idx];
		}
		internal void Run ()
		{
			RuntimeState = RuntimeState.Executing;
			Task task;
			try {
				while ((task = GetNext ()) != null || BookmarkResumptionQueue.Count != 0) {
					if (task == null) {
						ExecuteBookmark (BookmarkResumptionQueue.Dequeue ());
						continue;
					}
					switch (task.State) {
						case TaskState.Uninitialized:
							throw new Exception ("Tasks should be intialised when added to TaskList");
						case TaskState.Initialized:
							Execute (task);
							break;
						case TaskState.Ran:
							if (!task.Instance.IsCompleted) { //thus at Executing Status
								Teardown (task);
								try {
									task.Instance.Properties.Unregister (false);
								} catch (Exception ex) {
									RaiseFault (task.Instance, ex);
								}
							}
							if (task.CompletionCallback != null)
								ExecuteCallback (task);
							Remove (task);
							break;
						default:
							throw new Exception ("Invalid TaskState found in TaskList");
					}
				}
			} catch (UnhandledRuntimeException ure) {
				//this type of exception is returned by the Execute... methods
				RuntimeState = RuntimeState.UnhandledException;
				if (UnhandledException != null)
					UnhandledException (ure.InnerException, ure.RaisingActivity, ure.ActivityInstanceId);
				return;
			}
			if (TaskList.Count > 0)
				RuntimeState = RuntimeState.Idle;
			else
				RuntimeState = RuntimeState.CompletedSuccessfully;

			if (NotifyPaused != null)
				NotifyPaused ();
		}
		void RaiseFault (ActivityInstance raisingInstance, Exception ex)
		{
			var task = TaskList.Single (t => t.Instance == raisingInstance);
			TerminateTaskSubTree (task); //calls tlscleanup and unregister for exec props
			if (BubbleFaultToHandler (task, ref ex))
				return;
			throw new UnhandledRuntimeException (ex, raisingInstance);
		}
		bool BubbleFaultToHandler (Task task, ref Exception ex)
		{
			// returns true if handled, false if not
			// ex may be replaced with further exceptions raised from fault handlers
			// ExceptionSource on host still points to the first Activity to fault if unhandled
			var taskScheduledWithHandler = FindNextFaultHandler (task);
			if (taskScheduledWithHandler == null)
				return false;
			Task taskOwnsHandler = TaskList.Single (t=> t.Instance == taskScheduledWithHandler.Instance.ParentInstance);

			var context = new NativeActivityFaultContext (taskOwnsHandler.Instance, this);
			TLSSetup (taskOwnsHandler.Instance);
			bool raised = false;
			try {
				taskScheduledWithHandler.FaultCallback (context, ex, taskScheduledWithHandler.Instance);
			} catch (Exception ex2) {
				raised = true;
				ex = ex2;
				TerminateTaskSubTree (taskOwnsHandler);
			}
			if (!raised) {
				TLSCleanup (taskOwnsHandler.Instance);
				if (context.Handled)
					return true;
			}
			return BubbleFaultToHandler (taskOwnsHandler, ref ex);
		}
		Task FindNextFaultHandler (Task task)
		{
			if (task.Instance.ParentInstance == null)
				return null;
			if (task.FaultCallback != null)
				return task;
			Task parentTask = TaskList.Single (t=> t.Instance == task.Instance.ParentInstance);
			return FindNextFaultHandler (parentTask);
		}
		void TerminateTaskSubTree (Task task)
		{
			if (task.Instance.ParentInstance != null) {
				//root ActivityInstance doesnt go to faulted on unhandled exception
				task.Instance.State = ActivityInstanceState.Faulted;
			}
			//leaving task in tasklist so its completion callback will still run if it was scheduled with one
			RemoveBookmarksAndResumptions (task);
			var coll = new Collection<Task> ();
			GetDescendantTasks (task, coll);
			foreach (var t in coll.Reverse ()) { //FIXME: test order of prop unregisters
				RemoveBookmarksAndResumptions (t);
				t.Instance.Properties.Unregister (true);
				t.Instance.State = ActivityInstanceState.Faulted;
				TaskList.Remove (t);
			}
			TLSCleanup (task.Instance);
			task.Instance.Properties.Unregister (true);
		}
		void GetDescendantTasks (Task task, ICollection<Task> coll)
		{
			var children = TaskList.Where (t=> t.Instance.ParentInstance == task.Instance).ToList ();
			foreach (var t in children) {
				coll.Add (t);
				GetDescendantTasks (t, coll);
			}
		}
		void RemoveBookmarksAndResumptions (Task task)
		{
			//FIXME
			BookmarkResumptionQueue = new Queue<BookmarkResumption> (
				BookmarkResumptionQueue.Where (r => r.Instance != task.Instance));
			ActiveBookmarks.RemoveAll (br => br.Instance == task.Instance);
		}
		void ExecuteCallback (Task task)
		{
			//FIXME: this handles callbacks set from ScheduledActivity(..), ScheduledActivity<t>(..)
			//ScheduledDelegate(..), ScheduleFunc (..), ScheduleAction ()
			//Seperate the logic for handling into Task subclasses perhaps?
			if (task == null)
				throw new ArgumentNullException ("task");
			var context = new NativeActivityContext (task.Instance.ParentInstance, this);
			var callbackType = task.CompletionCallback.GetType ();
			TLSSetup (task.Instance.ParentInstance);
			bool raised = false;
			try {
				if (callbackType == typeof (CompletionCallback)) {
					// ScheduleActivity and ScheduleAction can pass this type of cb, handled same
					task.CompletionCallback.DynamicInvoke (context, task.Instance);
				} else if (callbackType == typeof (DelegateCompletionCallback)) {
					//only ScheduleDelegate can pass this type of cb
					//FIXME: the BoundArgument null check is so only DelOutArgs explicitly set by 
					//user are returned like in .NET, unsure if its the best solution though
					var outDict = task.Instance.RuntimeDelegateArguments
						.Where (kvp => kvp.Key.Direction == ArgumentDirection.Out &&
							kvp.Key.BoundArgument != null) 
							.ToDictionary (kvp=> kvp.Key.Name, kvp=>kvp.Value.Value);
					task.CompletionCallback.DynamicInvoke (context, task.Instance, outDict);
				} else if (callbackType.GetGenericTypeDefinition () == typeof (CompletionCallback<>)) {
					//ScheduleActivity<T> and ScheduleFunc<..> can pass this cb type
					object result;
					//check if argument to use as result has been set (only applicable to ActivityFunc)
					var resultRDA = task.Instance.ResultRuntimeDelegateArgument;
					if (resultRDA != null) {
						result = task.Instance.RuntimeDelegateArguments [resultRDA].Value;
					} else if (typeof (ActivityWithResult).IsAssignableFrom (task.Activity.GetType ())) {
						//all ScheduleActivity<T> set cbs go down this route and some ScheduleFunc<..>
						var resultRuntimeArgKvp = task.Instance.RuntimeArguments
							.Single ((kvp)=> kvp.Key.Name == Argument.ResultValue &&
								 kvp.Key.Direction == ArgumentDirection.Out);
						//ActivityFunc's Handler may be set to Activity that returns wrong type
						var cbTypeArg = callbackType.GetGenericArguments () [0];
						if (cbTypeArg.IsAssignableFrom (resultRuntimeArgKvp.Key.Type)) { 
							result = resultRuntimeArgKvp.Value.Value;
						} else {
							result = cbTypeArg.IsValueType ? Activator.CreateInstance (cbTypeArg) : null;
						}
					} else { //only ScheduleFunc<..> cb may accompany a non-ActivityWithResult subclass
						var cbTypeArg = callbackType.GetGenericArguments () [0];
						result = cbTypeArg.IsValueType ? Activator.CreateInstance (cbTypeArg) : null;
					}
					task.CompletionCallback.DynamicInvoke (context, task.Instance, result);
				} else {
					throw new NotSupportedException ("Runtime error, invalid callback delegate");
				}
			} catch (TargetInvocationException ex) {
				raised = true;
				RaiseFault (task.Instance.ParentInstance, ex.InnerException);
			}
			if (!raised)
				TLSCleanup (task.Instance.ParentInstance);
		}
		void ExecuteBookmark (BookmarkResumption bookmarkResumption)
		{
			if (bookmarkResumption == null)
				throw new ArgumentNullException ("bookmarkResumption");

			if (bookmarkResumption.Callback == null)
				return;

			var context = new NativeActivityContext (bookmarkResumption.Instance, this);
			TLSSetup (bookmarkResumption.Instance);
			bool raised = false;
			try {
				bookmarkResumption.Callback (context, bookmarkResumption.Bookmark, bookmarkResumption.Value);
			} catch (Exception ex) {
				raised = true;
				RaiseFault (bookmarkResumption.Instance, ex);
			}
			if (!raised)
				TLSCleanup (bookmarkResumption.Instance);
		}
		internal void AddBookmark (BookmarkRecord bookmarkRecord)
		{
			if (bookmarkRecord == null)
				throw new ArgumentNullException ("bookmarkRecord");

			if (ActiveBookmarks.Any (br => br.Bookmark == bookmarkRecord.Bookmark ||
						 (br.Bookmark.Name != String.Empty 
			 && br.Bookmark.Equals (bookmarkRecord.Bookmark))))
				throw new InvalidOperationException (String.Format ("A bookmark with the name '{0}' " +
										    "already exists.", bookmarkRecord.Bookmark.Name));
			ActiveBookmarks.Add (bookmarkRecord);
		}
		internal ReadOnlyCollection<BookmarkInfo> GetBookmarks ()
		{
			var infoList = ActiveBookmarks.Where (r=> r.Bookmark.Name != String.Empty)
				.Select (r => new BookmarkInfo (r)).ToList ();
			return new ReadOnlyCollection<BookmarkInfo> (infoList);
		}
		internal BookmarkResumptionResult ResumeBookmark (Bookmark bookmark, object value)
		{
			if (bookmark == null)
				throw new ArgumentNullException ("bookmark");

			//FIXME: unsure when BookmarkResumptionResult.NotReady should be used

			var record = ActiveBookmarks.SingleOrDefault (r => r.Bookmark.Equals (bookmark)); //FIXME: test

			if (record == null)
				return BookmarkResumptionResult.NotFound;

			var resumption = new BookmarkResumption (record, value);
			BookmarkResumptionQueue.Enqueue (resumption);
			if (!record.IsMultiResume)
				ActiveBookmarks.Remove (record);
			return BookmarkResumptionResult.Success;
		}
		internal bool RemoveBookmark (Bookmark bookmark, ActivityInstance callingInstance)
		{
			if (bookmark == null)
				throw new ArgumentNullException ("bookmark");
			if (callingInstance == null)
				throw new ArgumentNullException ("callingInstance");

			var record = ActiveBookmarks.SingleOrDefault (r => r.Bookmark.Equals (bookmark)); //FIXME: test;
			if (record == null)
				return false;
			if (record.Instance != callingInstance)
				throw new InvalidOperationException ("Bookmarks can only be removed by the activity " +
								     "instance that created them.");
			ActiveBookmarks.Remove (record);
			return true;
		}
		internal void RemoveAllBookmarks (ActivityInstance callingInstance)
		{
			var records = ActiveBookmarks.Where (r => r.Instance == callingInstance).ToList ();
			foreach (var r in records)
				ActiveBookmarks.Remove (r);
		}
		bool IsBlocked (Task task)
		{
			if (ActiveBookmarks.Any (br => br.Instance == task.Instance && br.IsBlocking))
				return true;
			if (BookmarkResumptionQueue.Any (bres => bres.Instance == task.Instance))
				return true;
			if (TaskList.Any (t => t.Instance.ParentInstance == task.Instance && IsBlocked (t)))
				return true;

			return false;
		}
		//exceptions from TLS... methods should not be propagated through fault handling system
		void TLSSetup (ActivityInstance instance)
		{
			instance.Properties.SetupWorkflowThread ();
		}
		void TLSCleanup (ActivityInstance instance)
		{
			instance.Properties.CleanupWorkflowThread ();
		}
		ActivityInstance Initialise (Task task, ActivityInstance parentInstance)
		{
			Logger.Log ("Initializing {0}", task.Activity.DisplayName);

			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Uninitialized)
				throw new InvalidOperationException ("Initialized");
			CurrentInstanceId++;
			var metadata = AllMetadata.Single (m => m.Environment.Root == task.Activity);
			var instance = new ActivityInstance (task.Activity, CurrentInstanceId.ToString (), 
							     ActivityInstanceState.Executing, parentInstance, 
							     metadata.Environment.IsImplementation, this);
			task.Instance = instance;

			// these need to be created before any DelegateArgumentValues initialised
			foreach (var rda in metadata.Environment.RuntimeDelegateArguments) {
				var loc = ConstructLocationT (rda.Type);
				instance.RuntimeDelegateArguments.Add (rda, loc);
			}
			instance.ResultRuntimeDelegateArgument = metadata.Environment.ResultRuntimeDelegateArgument;

			Logger.Log ("Initializing {0}\tRuntimeDelegateArguments Initialised", task.Activity.DisplayName);
			foreach (var rtArg in metadata.Environment.RuntimeArguments) {
				if (rtArg.Direction == ArgumentDirection.Out || 
				    rtArg.Direction == ArgumentDirection.InOut) {
					var aEnv = metadata.Environment as ActivityEnvironment; 
					if (aEnv != null && aEnv.Bindings.ContainsKey (rtArg) &&
					    aEnv.Bindings [rtArg] != null && aEnv.Bindings [rtArg].Expression != null) {
						CompletionCallback<object> cb;
						if (rtArg.Direction == ArgumentDirection.Out) {
							cb = (context, completeInstance, value) => {
								var retLoc = ((Location) value);
								retLoc.MakeDefault (); // FIXME: erroneous
								instance.RuntimeArguments.Add (rtArg, retLoc);
							};
						} else if (rtArg.Direction == ArgumentDirection.InOut) {
							cb = (context, completeInstance, value) => {
								var retLoc = ((Location) value);
								instance.RuntimeArguments.Add (rtArg, retLoc);
							};
						} else
							throw new Exception ("shouldnt see me");
						RuntimeScheduleActivity (aEnv.Bindings [rtArg].Expression, instance, cb, null);
					} else {
						// create a new location to hold temp values while activity executing
						var loc = ConstructLocationT (rtArg.Type);
						instance.RuntimeArguments.Add (rtArg, loc);
					}
				} else if (rtArg.Direction == ArgumentDirection.In) {
					var loc = ConstructLocationT (rtArg.Type);
					var aEnv = metadata.Environment as ActivityEnvironment; 
					if (aEnv != null && aEnv.Bindings.ContainsKey (rtArg)) {
						if ( aEnv.Bindings [rtArg] != null && aEnv.Bindings [rtArg].Expression != null) {
							CompletionCallback<object> cb; 
							cb = (context, completeInstance, value) => {
								loc.Value = value;
							};
							RuntimeScheduleActivity (aEnv.Bindings [rtArg].Expression, instance, cb, null);
						}
					}
					instance.RuntimeArguments.Add (rtArg, loc);
				} else {
					throw new Exception ("ArgumentDirection unknown");
				}

			}
			Logger.Log ("Initializing {0}\tRuntimeArguments Initialised", task.Activity.DisplayName);
			foreach (var pubVar in metadata.Environment.PublicVariables) {
				var loc = InitialiseVariable (pubVar, instance);
				instance.PublicVariables.Add (pubVar, loc);
			}
			Logger.Log ("Initializing {0}\tPublicVariables Initialised", task.Activity.DisplayName);
			foreach (var impVar in metadata.Environment.ImplementationVariables) {
				var loc = InitialiseVariable (impVar, instance);
				instance.ImplementationVariables.Add (impVar, loc);
			}
			Logger.Log ("Initializing {0}\tImplementationVariables Initialised", task.Activity.DisplayName);

			task.State = TaskState.Initialized;

			return instance;
		}
		Location InitialiseVariable (Variable variable, ActivityInstance instance)
		{
			var loc = ConstructLocationT (variable.Type);
			loc.IsConst = ((variable.Modifiers & VariableModifiers.ReadOnly) == VariableModifiers.ReadOnly);
			if (variable.Default != null) {
				CompletionCallback<object> cb = (context, completeInstance, value) => {
					loc.SetConstValue (value);
				};
				RuntimeScheduleActivity (variable.Default, instance, cb, null);
			}
			return loc;
		}
		Location ConstructLocationT (Type type)
		{
			Type locationTType = typeof (Location<>);
			Type [] genericParams = { type };
			Type constructed = locationTType.MakeGenericType (genericParams);
			return (Location) Activator.CreateInstance (constructed);
		}
		void Execute (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State == TaskState.Uninitialized)
				throw new InvalidOperationException ("Uninitialized");

			Logger.Log ("Executing {0}", task.Activity.DisplayName);
			TLSSetup (task.Instance);
			bool raised = false;
			try {
				task.Activity.RuntimeExecute (task.Instance, this);
				task.State = TaskState.Ran;
			} catch (Exception ex) {
				raised = true;
				task.State = TaskState.Ran; //maybe not needed
				RaiseFault (task.Instance, ex);
			}
			if (!raised)
				TLSCleanup (task.Instance);
		}
		void Teardown (Task task)
		{
			Logger.Log ("Tearing down {0}", task.Activity.DisplayName);

			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State == TaskState.Uninitialized)
				throw new InvalidOperationException ("Uninitialized");

			task.Instance.State = ActivityInstanceState.Closed;
			var activeBookmarks = ActiveBookmarks.Where (r => r.Instance == task.Instance).ToList ();
			foreach (var record in activeBookmarks) {
				if (record.IsBlocking)
					throw new Exception ("teardown for act with blocking bookmarks");
				ActiveBookmarks.Remove (record);
			}
		}
		Metadata BuildCache (Activity activity, string baseOfId, ref int pubChildNo, LocationReferenceEnvironment parentEnv, 
		                     bool isImplementation)
		{
			Logger.Log ("BuildCache called for {0}, IsImplementation: {1}", activity.DisplayName, isImplementation);

			if (activity == null)
				throw new NullReferenceException ("activity");
			if (baseOfId == null)
				throw new NullReferenceException ("baseId");

			activity.Id = (baseOfId == String.Empty) ? pubChildNo.ToString () : baseOfId + "." + pubChildNo;

			var metadata = activity.GetMetadata (parentEnv);
			metadata.Environment.IsImplementation = isImplementation;
			AllMetadata.Add (metadata);
			int impChildNo = 0;
			foreach (var del in metadata.ImplementationDelegates.Reverse ()) {
				if (del.Handler != null) {
					++impChildNo;
					var handlerMd = BuildCache (del.Handler, activity.Id, ref impChildNo, metadata.Environment, true);
					handlerMd.InjectRuntimeDelegateArguments (del.GetRuntimeDelegateArguments ());
					handlerMd.InjectResultRuntimeDelegateArgument (del.GetResultArgument ());
				}
			}
			foreach (var del in metadata.Delegates.Reverse ()) {
				if (del.Handler != null) {
					++pubChildNo;
					var handlerMd = BuildCache (del.Handler, baseOfId, ref pubChildNo, metadata.Environment, false);
					handlerMd.InjectRuntimeDelegateArguments (del.GetRuntimeDelegateArguments ());
					handlerMd.InjectResultRuntimeDelegateArgument (del.GetResultArgument ());
				}
			}
			foreach (var impVar in metadata.Environment.ImplementationVariables.Reverse ()) {
				if (impVar.Default != null) {
					//++no;
					++impChildNo;
					BuildCache (impVar.Default, activity.Id, ref impChildNo, metadata.Environment, true);
				}
			}
			foreach (var pVar in metadata.Environment.PublicVariables.Reverse ()) {
				if (pVar.Default != null) {
					++pubChildNo;
					BuildCache (pVar.Default, baseOfId, ref pubChildNo, metadata.Environment, false);
				}
			}
			foreach (var item in metadata.Environment.Bindings.Reverse ()) {
				//locref is key, arg value
				if (item.Value != null && item.Value.Expression != null) {
					//isImp.. param affects incidental access to variables from Expression Activities
					++pubChildNo;
					BuildCache (item.Value.Expression, baseOfId, ref pubChildNo, metadata.Environment, false); 
				}
			}
			foreach (var child in metadata.ImplementationChildren.Reverse ()) {
				++impChildNo;
				BuildCache (child, activity.Id, ref impChildNo, metadata.Environment, true);
			}
			foreach (var child in metadata.Children.Reverse ()) {
				++pubChildNo;
				BuildCache (child, baseOfId, ref pubChildNo, metadata.Environment, false);
			}
			return metadata;
		}
	}
	internal class UnhandledRuntimeException : Exception
	{
		const string msg = "No FaultHandler handled the fault successfully";
		internal ActivityInstance RaisingInstance { get; private set; }
		internal String ActivityInstanceId {
			get { return RaisingInstance.Id; }
		}
		internal Activity RaisingActivity {
			get { return RaisingInstance.Activity; }
		}
		internal UnhandledRuntimeException (Exception ex, ActivityInstance raisingInstance) : base (msg, ex)
		{
			RaisingInstance = raisingInstance;
			//InnerException = ex;
		}
	}
	internal enum RuntimeState {
		Ready,
		Executing,
		CompletedSuccessfully,
		UnhandledException,
		Aborted,
		Terminated,
		Idle
	}
	internal class Task {
		internal TaskState State { get; set; }
		internal Activity Activity { get; private set; }
		internal ActivityInstance Instance { get; set; }
		internal Delegate CompletionCallback { get; set; }
		internal FaultCallback FaultCallback { get; set; }
		internal Task (Activity activity)
		{
			if (activity == null)
				throw new ArgumentNullException ();

			Activity = activity;
			State = TaskState.Uninitialized;
		}
		public override string ToString ()
		{
			return Activity.ToString ();
		}
	}
	internal enum TaskState {
		Uninitialized,
		Initialized,
		Ran,
	}
	internal class BookmarkRecord {
		internal Bookmark Bookmark { get; private set; }
		internal BookmarkOptions Options { get; private set; }
		internal BookmarkCallback Callback { get; private set; }
		internal ActivityInstance Instance { get; private set; }
		internal bool IsBlocking {
			get { return !Options.HasFlag (BookmarkOptions.NonBlocking); }
		}
		internal bool IsMultiResume {
			get { return Options.HasFlag (BookmarkOptions.MultipleResume); }
		}

		internal BookmarkRecord (Bookmark bookmark, BookmarkOptions options, BookmarkCallback callback,
					 ActivityInstance instance)
		{
			if (bookmark == null)
				throw new ArgumentNullException ("bookmark");
			if (instance == null)
				throw new ArgumentNullException ("instance");
			//FIXME: bookmarkoptions will need validated too

			Bookmark = bookmark;
			Options = options;
			Callback = callback;
			Instance = instance;
		}
	}
	internal class BookmarkResumption {
		internal Bookmark Bookmark { get; private set; }
		internal BookmarkCallback Callback { get; private set; }
		internal ActivityInstance Instance { get; private set; }
		internal Object Value { get; private set; }

		internal BookmarkResumption (BookmarkRecord bookmarkRecord, object value)
		{
			if (bookmarkRecord == null)
				throw new ArgumentNullException ("bookmarkRecord");

			Bookmark = bookmarkRecord.Bookmark;
			Callback = bookmarkRecord.Callback;
			Instance = bookmarkRecord.Instance;
			Value = value;
		}
	}
	public static class Logger {
		//use this class to output ad hoc logging info you provide by calling Logger.Log (..)
		static Logger ()
		{
			Log ("\nExecution Started on {0} at {1} \n{2}{2}", DateTime.Now.ToShortDateString (),
			     DateTime.Now.ToShortTimeString (),
			     "----------------------------------------------------------------------------");
		}
		public static void Log (string format, params object[] args)
		{
			//var sw = new StreamWriter (@"WFLog.txt", true);
			//sw.WriteLine (String.Format(format, args));
			//sw.Close();
		}
	}
}

