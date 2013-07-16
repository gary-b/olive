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

		List<BookmarkRecord> ActiveBookmarks { get; set; }
		Queue<BookmarkResumption> BookmarkResumptionQueue { get; set; }

		internal RuntimeState RuntimeState { get; set;}
		internal Action NotifyPaused { get; set; }
		internal Action<Exception, Activity, string> UnhandledException { get; set; }

		internal WorkflowRuntime (Activity baseActivity) : this (baseActivity, null)
		{
		}
		internal WorkflowRuntime (Activity baseActivity, IDictionary<string, object> inputs)
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

			BuildCache (WorkflowDefinition, String.Empty, 1, null, false);
			RootInstance = AddNextAndInitialise (new Task (WorkflowDefinition), null);

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
					                             "on Activity " + baseActivity.ToString (), "inputs"); //FIXME: error msg
			}
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
		internal ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance)
		{
			return ScheduleActivity (activity, parentInstance, null);
		}
		internal ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance, 
		                                            CompletionCallback onComplete)
		{
			return ScheduleActivity (activity, parentInstance, (Delegate) onComplete);
		}
		internal ActivityInstance ScheduleActivity<TResult> (Activity<TResult> activity, ActivityInstance parentInstance, 
		                                                     CompletionCallback<TResult> onComplete, FaultCallback onFaulted)
		{
			return ScheduleActivity (activity, parentInstance, onComplete);
		}
		ActivityInstance ScheduleActivity (Activity activity, ActivityInstance parentInstance, 
		                                   Delegate onComplete)
		{
			if (activity == null)
				throw new ArgumentNullException ("activity");
			if (parentInstance == null)
				throw new ArgumentNullException ("parentInstance");

			var task = new Task (activity);
			task.CompletionCallback = onComplete;
			return AddNextAndInitialise (task, parentInstance);
		}
		internal ActivityInstance ScheduleDelegate (ActivityDelegate activityDelegate, 
		                                            IDictionary<string, object> param,
		                                            CompletionCallback onCompleted,
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
			var instance = AddNextAndInitialise (task, parentInstance);
			int pCount = (param == null) ? 0 : param.Count;
			int expectedCount = instance.RuntimeDelegateArguments.Count;

			if (pCount != expectedCount) {
				throw new ArgumentException (String.Format (
					"The supplied input parameter count {0} does not match the expected count of {1}.",
					pCount, expectedCount), "param");
			}
			foreach (var expectedKvp in instance.RuntimeDelegateArguments) {
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
			if (bookmark == null)
				throw new ArgumentNullException ("bookmark");

			var bookmarkRecord = ActiveBookmarks.SingleOrDefault (r => r.Bookmark.Equals (bookmark)); //FIXME: test
			if (bookmarkRecord == null)
				return BookmarkResumptionResult.NotFound;

			var resumption = new BookmarkResumption (bookmarkRecord, value);
			BookmarkResumptionQueue.Enqueue (resumption);
			if (!bookmarkRecord.IsMultiResume)
				ActiveBookmarks.Remove (bookmarkRecord);
			RuntimeState = RuntimeState.Ready; //FIXME: best state? Same as before wf ran 
			return BookmarkResumptionResult.Success;
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
			/* skip Tasks with activities that are blocked waiting bookmark resumption
			 * (This includes Activities with bookmarks that are blocking and those with non blocking
			 * bookmarks with a bookmarkresumption pending for them)
			 */
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

			while ((task = GetNext ()) != null || BookmarkResumptionQueue.Count != 0) {
				try {
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
						Teardown (task); //FIXME: this will be run twice if theres a comp..cb
						if (task.CompletionCallback != null) {
							ExecuteCallback (task);
							task.CompletionCallback = null; // avoid infinite loop
							break; // let any newly scheduled activities run
						} 
						Remove (task);
						break;
						default:
						throw new Exception ("Invalid TaskState found in TaskList");
					}
				} catch (Exception ex) {
					RuntimeState = RuntimeState.UnhandledException;
					if (UnhandledException != null) {
						UnhandledException (ex, task.Activity, task.Activity.Id);
						return; 
					} // else
					throw ex;
				}
			}
			if (TaskList.Count > 0)
				RuntimeState = RuntimeState.Idle;
			else
				RuntimeState = RuntimeState.CompletedSuccessfully;

			if (NotifyPaused != null)
				NotifyPaused ();
		}
		void ExecuteCallback (Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			var context = new NativeCallbackActivityContext (task.Instance.ParentInstance, 
			                                                 this, task.CompletionCallback);
			var callbackType = task.CompletionCallback.GetType ();
			try {
				if (callbackType == typeof (CompletionCallback)) {
					task.CompletionCallback.DynamicInvoke (context, task.Instance);
				} else if (callbackType.GetGenericTypeDefinition () == typeof (CompletionCallback<>)) {
					var result = task.Instance.RuntimeArguments
						.Single ((kvp)=> kvp.Key.Name == Argument.ResultValue &&
						         kvp.Key.Direction == ArgumentDirection.Out).Value.Value;
					task.CompletionCallback.DynamicInvoke (context, task.Instance, result);
				} else {
					throw new NotSupportedException ("Runtime error, invalid callback delegate");
				}
			} catch (TargetInvocationException ex) {
				throw ex.InnerException;
			}
		}
		void ExecuteBookmark (BookmarkResumption bookmarkResumption)
		{
			if (bookmarkResumption == null)
				throw new ArgumentNullException ("bookmarkResumption");

			if (bookmarkResumption.Callback == null)
				return;

			var context = new NativeCallbackActivityContext (bookmarkResumption.Instance, 
			                                                 this, bookmarkResumption.Callback);

			bookmarkResumption.Callback (context, bookmarkResumption.Bookmark, bookmarkResumption.Value);
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
		internal BookmarkResumptionResult ResumeBookmark (Bookmark bookmark, object value, 
		                                                  ActivityInstance callingInstance)
		{
			if (bookmark == null)
				throw new ArgumentNullException ("bookmark");
			if (callingInstance == null)
				throw new ArgumentNullException ("callingInstance");

			//FIXME: unsure when BookmarkResumptionResult.NotReady should be used

			var record = ActiveBookmarks.SingleOrDefault (r => r.Bookmark.Equals (bookmark)); //FIXME: test

			if (record == null)
				return BookmarkResumptionResult.NotFound;

			var task =  TaskList.Single (t => t.Instance == callingInstance);
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
		ActivityInstance Initialise (Task task, ActivityInstance parentInstance)
		{
			Logger.Log ("Initializing {0}", task.Activity.DisplayName);

			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State != TaskState.Uninitialized)
				throw new InvalidOperationException ("Initialized");
			CurrentInstanceId++;
			var metadata = AllMetadata.Single (m => m.Environment.Root == task.Activity);
			var instance = new ActivityInstance (task.Activity, CurrentInstanceId.ToString (), false, 
			                                     ActivityInstanceState.Executing, parentInstance, 
			                                     metadata.Environment.IsImplementation);
			task.Instance = instance;

			// these need to be created before any DelegateArgumentValues initialised
			foreach (var rda in metadata.Environment.RuntimeDelegateArguments) {
				var loc = ConstructLocationT (rda.Type);
				instance.RuntimeDelegateArguments.Add (rda, loc);
			}
			Logger.Log ("Initializing {0}\tRuntimeDelegateArguments Initialised", task.Activity.DisplayName);
			foreach (var rtArg in metadata.Environment.RuntimeArguments) {
				if (rtArg.Direction == ArgumentDirection.Out || 
				    rtArg.Direction == ArgumentDirection.InOut) {
					var aEnv = metadata.Environment as ActivityEnvironment; 
					if (aEnv != null && aEnv.Bindings.ContainsKey (rtArg) &&
					    aEnv.Bindings [rtArg] != null && aEnv.Bindings [rtArg].Expression != null) {
						// create task to get location to be used as I Value

						//var getLocTask = new Task (aEnv.Bindings [rtArg].Expression
						//AddNextAndInitialise (getLocTask, instance);
						CompletionCallback<object> cb;
						if (rtArg.Direction == ArgumentDirection.Out) {
							cb = (context, completeInstance, value) => {
								var retLoc = ((Location) value);
								retLoc.MakeDefault (); // FIXME: erroneous
								instance.RuntimeArguments.Add (rtArg, 
								                               retLoc);
							};
						} else if (rtArg.Direction == ArgumentDirection.InOut) {
							cb = (context, completeInstance, value) => {
								var retLoc = ((Location) value);
								instance.RuntimeArguments.Add (rtArg, 
								                               retLoc);
							};
						} else
							throw new Exception ("shouldnt see me");
						ScheduleActivity (aEnv.Bindings [rtArg].Expression, instance, cb);
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
							ScheduleActivity (aEnv.Bindings [rtArg].Expression, instance, cb);
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
				ScheduleActivity (variable.Default, instance, cb);
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
			task.Activity.RuntimeExecute (task.Instance, this);
			task.State = TaskState.Ran;
		}
		void Teardown (Task task)
		{
			Logger.Log ("Tearing down {0}", task.Activity.DisplayName);

			if (task == null)
				throw new ArgumentNullException ("task");
			if (task.State == TaskState.Uninitialized)
				throw new InvalidOperationException ("Uninitialized");

			task.Instance.State = ActivityInstanceState.Closed;
			task.Instance.IsCompleted = true;
			var activeBookmarks = ActiveBookmarks.Where (r => r.Instance == task.Instance).ToList ();
			foreach (var record in activeBookmarks) {
				if (record.IsBlocking)
					throw new Exception ("teardown for act with blocking bookmarks");
				ActiveBookmarks.Remove (record);
			}
		}
		Metadata BuildCache (Activity activity, string baseOfId, int no, LocationReferenceEnvironment parentEnv, 
		                     bool isImplementation)
		{
			Logger.Log ("BuildCache called for {0}, IsImplementation: {1}", activity.DisplayName, isImplementation);

			if (activity == null)
				throw new NullReferenceException ("activity");
			if (baseOfId == null)
				throw new NullReferenceException ("baseId");

			activity.Id = (baseOfId == String.Empty) ? no.ToString () : baseOfId + "." + no;

			var metadata = activity.GetMetadata (parentEnv);
			metadata.Environment.IsImplementation = isImplementation;
			AllMetadata.Add (metadata);

			foreach (var item in metadata.Environment.Bindings) {
				//locref is key, arg value
				if (item.Value != null && item.Value.Expression != null) {
					//isImp.. param affects incidental access to variables from Expression Activities
					BuildCache (item.Value.Expression, baseOfId, ++no, metadata.Environment, false); 
				}
			}
			int childNo = 0;
			foreach (var child in metadata.ImplementationChildren)
				BuildCache (child, activity.Id, ++childNo, metadata.Environment, true);

			foreach (var child in metadata.Children)
				BuildCache (child, baseOfId, ++no, metadata.Environment, false);

			foreach (var pVar in metadata.Environment.PublicVariables) {
				if (pVar.Default != null)
					BuildCache (pVar.Default, baseOfId, ++no, metadata.Environment, false);
			}
			foreach (var impVar in metadata.Environment.ImplementationVariables) {
				if (impVar.Default != null)
					BuildCache (impVar.Default, baseOfId, ++no, metadata.Environment, false);
			}
			foreach (var del in metadata.Delegates) {
				if (del.Handler != null) {
					var handlerMd = BuildCache (del.Handler, baseOfId, ++no, metadata.Environment, false);
					handlerMd.InjectRuntimeDelegateArguments (del.GetRuntimeDelegateArguments ());
				}
			}
			foreach (var del in metadata.ImplementationDelegates) {
				if (del.Handler != null) {
					var handlerMd = BuildCache (del.Handler, activity.Id, ++childNo, metadata.Environment, true);
					handlerMd.InjectRuntimeDelegateArguments (del.GetRuntimeDelegateArguments ());
				}
			}
			return metadata;
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
		BlockedButHasBookmarkResumptions
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

