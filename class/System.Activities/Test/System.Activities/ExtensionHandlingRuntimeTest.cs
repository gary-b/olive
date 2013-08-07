using System;
using NUnit.Framework;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Linq;
using System.Activities;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class ExtensionHandlingRuntimeTest : WFTestHelper {
		class ExtWriter<T1, T2> : NativeActivity where T1 : class where T2 : class {
			Action<NativeActivityMetadata> CacheMetadataAction { get; set; }
			public ExtWriter	()
			{
			}
			public ExtWriter (Action<NativeActivityMetadata> cacheMetadata)
			{
				CacheMetadataAction = cacheMetadata;
			}
			protected override void CacheMetadata (NativeActivityMetadata metadata)
			{
				if (CacheMetadataAction != null)
					CacheMetadataAction (metadata);
			}
			protected override void Execute (NativeActivityContext context)
			{
				Console.WriteLine (context.GetExtension<T1> ());
				Console.WriteLine (context.GetExtension<T2> ());
			}
		}
		class ExtWriter<T1, T2, T3> : ExtWriter<T1, T2> where T1 : class where T2 : class where T3 : class {
			public ExtWriter ()
			{
			}
			public ExtWriter (Action<NativeActivityMetadata> cacheMetadata) : base (cacheMetadata)
			{
			}
			protected override void Execute (NativeActivityContext context)
			{
				base.Execute (context);
				Console.WriteLine (context.GetExtension<T3> ());
			}
		}
		static WorkflowInstanceHost GetHostToComplete (Activity wf)
		{
			var host = new WorkflowInstanceHost (wf);
			host.NotifyPaused = () =>  {
				if (host.Controller_State == WorkflowInstanceState.Complete)
					host.AutoResetEvent.Set ();
			};
			return host;
		}
		static WorkflowInstanceHost GetHostToIdleOrComplete (Activity wf)
		{
			var host = new WorkflowInstanceHost (wf);
			host.NotifyPaused = () =>  {
				var state = host.Controller_State;
				if (state == WorkflowInstanceState.Complete || state == WorkflowInstanceState.Idle)
				host.AutoResetEvent.Set ();
			};
			return host;
		}
		static void InitRunWait (WorkflowInstanceHost host)
		{
			host.Initialize (null, null);
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
		}
		static void RunAgain (WorkflowInstanceHost host)
		{
			host.AutoResetEvent.Reset ();
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
		}
		static WorkflowInstanceHost GetHostAddingExtManAndObjects<T1, T2> (T1 obj1, T2 obj2) where T1 : class where T2 : class
		{
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (obj1);
			extman.Add (obj2);
			var host = GetHostToComplete (new ExtWriter<T1, T2> ());
			host.RegisterExtensionManager (extman);
			return host;
		}
		static WorkflowInstanceHost GetHostAndAdd2ExtsFromIWFInstExtViaHost<T1, T2> (T1 obj1, T2 obj2) where T1 : class where T2 : class
		{
			var ext = new WorkflowInstanceExt<string> (() => { 
				return new List<object> { obj1, obj2 };
			}, null);

			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (ext);
			var host = GetHostToComplete (new ExtWriter<T1, T2> ());
			host.RegisterExtensionManager (extman);
			return host;
		}
		static WorkflowInstanceHost GetHostAndAdd2ExtsFromIWFInstExtViaMetadata <T1, T2> (T1 obj1, T2 obj2) where T1 : class where T2 : class
		{
			var ext = new WorkflowInstanceExt<string> (() => { 
				return new List<object> { obj1, obj2 };
			}, null);
			var wf = new ExtWriter<T1, T2> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => ext);
			});
			var extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostToComplete (wf);
			host.RegisterExtensionManager (extman);
			return host;
		}
		static WorkflowInstanceHost GetHostAndAdd1ExtInHostAnd1FromIWFInstExtViaMetadata <T1, T2> (T1 hostObj, T2 extObj) where T1 : class where T2 : class
		{
			var ext = new WorkflowInstanceExt<string> (() => { 
				return new List<object> { extObj };
			}, null);
			var wf = new ExtWriter<T1, T2> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => ext);
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (hostObj);
			var host = GetHostToComplete (wf);
			host.RegisterExtensionManager (extman);
			return host;
		}
		static WorkflowInstanceHost GetHostAndAdd1ExtInMetadataAnd1FromIWFInstExtViaMetadata<T1, T2> (T1 metaObj, T2 extObj) where T1 : class where T2 : class
		{
			//doesnt matter if ext provider in root and metaObj added in child or other way about
			var extProvider = new WorkflowInstanceExt<string> (() => { 
				return new List<object> { extObj };
			}, null);
			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => metaObj);
			}, null);
			var wf = new ExtWriter<T1, T2> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => extProvider);
				metadata.AddChild (child);
			});
			var extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostToComplete (wf);
			host.RegisterExtensionManager (extman);
			return host;
		}
		static WorkflowInstanceHost GetHostAddingExtManAndFuncs<T1, T2> (T1 obj1, T2 obj2) where T1: class where T2:class
		{
			var extman = new WorkflowInstanceExtensionManager ();
			bool ran1 = false, ran2 = false;
			extman.Add (() => {
				ran1 = true;
				return obj1;
			});
			extman.Add (() => {
				ran2 = true;
				return obj2;
			});
			var host = GetHostToComplete (new ExtWriter<T1, T2> ());
			host.RegisterExtensionManager (extman);
			Assert.IsTrue (ran1);
			Assert.IsTrue (ran2);
			return host;
		}
		static void AssertHostGetExt_2OfSameType_Added (B b1, B b2, WorkflowInstanceHost host)
		{
			// GetExtension<T> provides access to first string only
			// GetExtensions returns different results depending on whether GetExtension already called

			//GetExtensions first reports 2 extensions passing B selector to GetExtensions
			Assert.AreEqual (2, host.GetExtensions<B> ().Count ());
			//both can be accessed
			Assert.Contains (b1, host.GetExtensions<B> ().ToList ());
			Assert.Contains (b2, host.GetExtensions<B> ().ToList ());
			//GetExtension returns first added b
			Assert.AreSame (b1, host.GetExtension<B> ());
			//now GetExtensions reports 1 extensions passing A selector
			Assert.AreEqual (1, host.GetExtensions<B> ().Count ());
			Assert.AreSame (b1, host.GetExtensions<B> ().Single ());
			// it matches 1st b
			//so no way to access 2nd b any more
		}
		static void AssertContextGetExt_2OfSameType_Added (B b1, B b2, WorkflowInstanceHost host)
		{
			InitRunWait (host);
			Assert.AreEqual (String.Format (("{0}{1}{0}{1}"), b1, Environment.NewLine), host.ConsoleOut);
		}
		static void AssertHostGetExt_SubClassAndParent_Added (B b, A a, WorkflowInstanceHost host)
		{
			// GetExtension<T> provides access to B only
			// GetExtensions returns different results depending on whether GetExtension already called

			//.NET reports 2 extensions to begin with when A selector passed to GetExtensions
			Assert.AreEqual (2, host.GetExtensions<A> ().Count ());
			//both can be accessed
			Assert.Contains (b, host.GetExtensions<A> ().ToList ());
			Assert.Contains (a, host.GetExtensions<A> ().ToList ());
			//passing GetExtension A as selector returns the b (it was added first)
			Assert.AreSame (b, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			//passing B also returns the b
			//now .NET reports 1 extension present for A
			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
			//and there seems to be no way to access the A anymore
			Assert.AreEqual (b, host.GetExtensions<A> ().Single ());
		}
		static void AssertContextGetExt_SubClassAndParent_Added (B b, A a, WorkflowInstanceHost host)
		{
			InitRunWait (host);
			Assert.AreEqual (String.Format ("{0}{1}{0}{1}", b, Environment.NewLine), host.ConsoleOut);
		}
		static void AssertHostGetExt_ParentAndSubClass_Added (A a, B b, WorkflowInstanceHost host)
		{
			// GetExtension<T> provides access to both extensions by key
			// GetExtensions returns different results depending on whether GetExtension already called

			//.NET reports 2 extensions to begin with when A selector passed to GetExtensions
			Assert.AreEqual (2, host.GetExtensions<A> ().ToList ().Count);
			//each can be accessed with GetExtension directly
			Assert.AreSame (a, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			//but now .NET reports only one extension when A selector passed to GetExtensions
			Assert.AreEqual (1, host.GetExtensions<A> ().ToList ().Count);
			Assert.AreSame (a, host.GetExtensions<A> ().Single ()); // it matches the A
			//.NET reports 1 extension present when B selector passed to GetExtensions
			Assert.AreEqual (1, host.GetExtensions<B> ().ToList ().Count);
			Assert.AreSame (b, host.GetExtensions<B> ().Single ());// it matches the str
		}
		static void AssertContextGetExt_ParentAndSubClass_Added (A a, B b, WorkflowInstanceHost host)
		{
			InitRunWait (host);
			Assert.AreEqual (String.Format ("{0}{2}{1}{2}", a, b, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void ExtMan_Add_Order_AddObjectProcessedBeforeAddFunc ()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			string str1 = "str1", str2 = "str2";
			extman.Add (str1);
			extman.Add (() => str2);
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.RegisterExtensionManager (extman);
			Assert.AreSame (str1, host.GetExtension<string> ());

			var extman2 = new WorkflowInstanceExtensionManager ();
			extman2.Add (() => str1);
			extman2.Add (str2);
			var host2 = new WorkflowInstanceHost (new WriteLine ());
			host2.RegisterExtensionManager (extman2);
			Assert.AreSame (str2, host2.GetExtension<string> ());
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ExtMan_Add_Object_NullEx()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add ((object) null);
		}
		[Test]
		public void ExtMan_Add_Object_SameType ()
		{
			B b1 = new B ("b1"), b2 = new B ("b2");
			var host = GetHostAddingExtManAndObjects (b1, b2);
			AssertHostGetExt_2OfSameType_Added (b1, b2, host);
			AssertContextGetExt_2OfSameType_Added (b1, b2, host);
		}
		[Test]
		public void ExtMan_Add_Object_SubClassAndParent ()
		{
			B b = new B ("b");
			A a = new A ("a");
			var host = GetHostAddingExtManAndObjects (b, a);
			AssertHostGetExt_SubClassAndParent_Added (b, a, host);
			AssertContextGetExt_SubClassAndParent_Added (b, a, host);
		}
		[Test]
		public void ExtMan_Add_Object_ParentAndSubclass ()
		{
			var a = new A ("a");
			var b = new B ("b"); 
			var host = GetHostAddingExtManAndObjects (a, b);
			AssertHostGetExt_ParentAndSubClass_Added (a, b, host);
			AssertContextGetExt_ParentAndSubClass_Added (a, b, host);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		public void ExtMan_Add_Func_NullEx()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add<object> (null);
		}
		[Test, ExpectedException (typeof (NullReferenceException))]
		public void ExtMan_Add_Func_ReturnsNullEx()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add<object> (() => null);
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.RegisterExtensionManager (extman);
		}
		[Test]
		public void ExtMan_Add_Func_SameType ()
		{
			B b1 = new B ("b1"), b2 = new B ("b2");

			var host = GetHostAddingExtManAndFuncs (b1, b2);
			AssertHostGetExt_2OfSameType_Added (b1, b2, host);
			AssertContextGetExt_2OfSameType_Added (b1, b2, host);
		}
		[Test]
		public void ExtMan_Add_Func_SubClassAndParent ()
		{
			B b = new B ("b"); 
			A a = new A ("a");
			var host = GetHostAddingExtManAndFuncs (b, a);
			AssertHostGetExt_SubClassAndParent_Added (b, a, host);
			AssertContextGetExt_SubClassAndParent_Added (b, a, host);
		}
		[Test]
		public void ExtMan_Add_Func_ParentAndSubclass ()
		{
			var a = new A ("a");
			var b = new B ("b"); 
			var host = GetHostAddingExtManAndFuncs (a, b);
			AssertHostGetExt_ParentAndSubClass_Added (a, b, host);
			AssertContextGetExt_ParentAndSubClass_Added (a, b, host);
		}
		class Disposable : IDisposable {
			public void Dispose ()
			{
				Console.WriteLine ("Disposed");
			}
		}
		[Test]
		public void ExtMan_Add_IDisposable_DisposeNotCalled ()
		{
			var disposable = new Disposable (); 
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => disposable);
			}, (context) => {
				Console.WriteLine (context.GetExtension<Disposable> ());
			});
			var host = GetHostExecuteWFWithExtman (wf);
			Assert.AreEqual (String.Format ("{0}{1}", disposable, Environment.NewLine), host.ConsoleOut);
		}
		#region AddingExtensionFromCacheMetadata ()
		static WorkflowInstanceHost GetHostExecuteWFWithExtman (Activity wf, WorkflowInstanceExtensionManager extman = null)
		{
			if (extman == null)
				extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostToComplete (wf);
			host.RegisterExtensionManager (extman);
			InitRunWait (host);
			return host;
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_IgnoredIfNoExtMan ()
		{
			string str = "str";
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => str);
			}, (context) => {
				Console.WriteLine (context.GetExtension<string> ());
			});
			var host = GetHostToComplete (wf);
			InitRunWait (host);
			Assert.AreEqual (Environment.NewLine, host.ConsoleOut);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider ()
		{
			string str = "str";
			bool executedInCacheMetadata = false;
			var wf = new NativeActivityRunner ((metadata) => {
				bool ran = false;
				metadata.AddDefaultExtensionProvider (() => { 
					ran = true; 
					return str; 
				});
				executedInCacheMetadata = ran;
			}, (context) => {
				Console.WriteLine (context.GetExtension<string> ());
			});
			var host = GetHostExecuteWFWithExtman (wf);
			Assert.AreEqual ("str" + Environment.NewLine, host.ConsoleOut);
			Assert.IsFalse (executedInCacheMetadata);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_ProcessedInOrderCacheMetadataCalled ()
		{
			string str = "str"; 
			B b = new B ("b1");
			int [] intArr = new int[2];
			int funcOrderCounter = 0, parentFuncOrder = 0, child1FuncOrder = 0, child2FuncOrder = 0;
			int cacheOrderCounter = 0, parentCacheOrder = 0, child1CacheOrder = 0, child2CacheOrder = 0;
			var child2 = new NativeActivityRunner ((metadata) => {
				child2CacheOrder = (++cacheOrderCounter);
				metadata.AddDefaultExtensionProvider (() => { 
					child2FuncOrder = (++funcOrderCounter);
					return intArr;
				} );
			}, null);
			var child1 = new NativeActivityRunner ((metadata) => {
				child1CacheOrder = (++cacheOrderCounter);
				metadata.AddDefaultExtensionProvider (() => { 
					child1FuncOrder = (++funcOrderCounter);
					return b;
				} );
			}, null);
			var parent = new NativeActivityRunner ((metadata) => {
				parentCacheOrder = (++cacheOrderCounter);
				metadata.AddDefaultExtensionProvider (() => { 
					parentFuncOrder = (++funcOrderCounter);
					return str;
				} );
				metadata.AddChild (child2);
				metadata.AddChild (child1);
			}, null);
			var host = GetHostExecuteWFWithExtman (parent);
			Assert.AreEqual (parentCacheOrder, parentFuncOrder);
			Assert.AreEqual (child1CacheOrder, child1FuncOrder);
			Assert.AreEqual (child2CacheOrder, child2FuncOrder);
			Assert.AreSame (str, host.GetExtension<string> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			Assert.AreSame (intArr, host.GetExtension<int[]> ());
		}
		//FIXME: more test
		[Test]
		[Ignore ("Order CacheMetadata called through tree")]
		public void CacheMetadata_OrderCalled ()
		{
			//FIXME: extend to test Pub / Imp ActivityDelegates, 
			//Perhaps also Expressions (would EvaluationOrder matter?), Pub/Imp Variable Defaults, 

			//children executed in lifo manner
			//but implementation children executed first
			int orderCounter = 0, parentOrder = 0, child1Order = 0, child2Order = 0;
			int child1child1Order = 0, child2child1Order = 0;
			int child1child2Order = 0, child2child2Order = 0;

			var child2child2 = new NativeActivityRunner ((metadata) => {
				child2child2Order = (++orderCounter);
			}, null);
			var child1child2 = new NativeActivityRunner ((metadata) => {
				child1child2Order = (++orderCounter);
			}, null);
			var child2child1 = new NativeActivityRunner ((metadata) => {
				child2child1Order = (++orderCounter);
			}, null);
			var child1child1 = new NativeActivityRunner ((metadata) => {
				child1child1Order = (++orderCounter);
			}, null);
			var child2 = new NativeActivityRunner ((metadata) => {
				child2Order = (++orderCounter);
				metadata.AddImplementationChild (child2child2);
				metadata.AddImplementationChild (child2child1);
			}, null);
			var child1 = new NativeActivityRunner ((metadata) => {
				child1Order = (++orderCounter);
				metadata.AddChild (child1child2);
				metadata.AddChild (child1child1);
			}, null);
			var parent = new NativeActivityRunner ((metadata) => {
				parentOrder = (++orderCounter);
				metadata.AddImplementationChild (child2);
				metadata.AddChild (child1);
			}, null);
			WorkflowInvoker.Invoke (parent);
			Assert.AreEqual (1, parentOrder);
			Assert.AreEqual (5, child1Order);
			Assert.AreEqual (6, child1child1Order);
			Assert.AreEqual (7, child1child2Order);
			Assert.AreEqual (2, child2Order);
			Assert.AreEqual (3, child2child1Order);
			Assert.AreEqual (4, child2child2Order);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_NullEx ()
		{
			Exception exception = null;
			var wf = new NativeActivityRunner ((metadata) => {
				try {
					metadata.AddDefaultExtensionProvider<string> (null);
				} catch (Exception ex) {
					exception = ex;
				}
			}, null);
			var host = new WorkflowInstanceHost (wf);
			host.RegisterExtensionManager (new WorkflowInstanceExtensionManager ());
			Assert.IsInstanceOfType (typeof (ArgumentNullException), exception);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_ReturnsNullEx ()
		{
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider<string> (() => null);
			}, null);
			var host = new WorkflowInstanceHost (wf);
			Exception exception = null;
			try {
				host.RegisterExtensionManager (new WorkflowInstanceExtensionManager ());
			} catch (Exception ex) {
				exception = ex;
			}
			Assert.IsInstanceOfType (typeof (NullReferenceException), exception);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_FuncExecutedEvenWhenExtNeverUsed ()
		{
			string str = "str";
			bool ran = false;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					ran = true; 
					return str; 
				});
			}, (context) => {
				Console.WriteLine (ran);
			});
			var host = GetHostExecuteWFWithExtman (wf);
			Assert.AreEqual ("True" + Environment.NewLine, host.ConsoleOut);
			Assert.IsTrue (ran);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_SameType ()
		{
			//only the first func runs
			B b1 = new B ("b1"), b2 = new B ("b2");
			bool b1Ran = false, b2Ran = false;
			var wf = new ExtWriter<A, B> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					b1Ran = true;
					return b1; 
				});
				metadata.AddDefaultExtensionProvider (() => { 
					b2Ran = true;
					return b2; 
				});
			});
			var host = GetHostExecuteWFWithExtman (wf);
			Assert.IsTrue (b1Ran);
			Assert.IsFalse (b2Ran);
			Assert.AreSame (b1, host.GetExtension<B> ());
			Assert.AreSame (b1, host.GetExtensions<B> ().Single ());
			Assert.AreEqual (String.Format ("{0}{2}{0}{2}", b1, b2, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_SubClassAndParent ()
		{
			//only the func for the subclass runs
			B b = new B ("b");
			A a = new A ("a");
			bool bRan = false, aRan = false;
			var wf = new ExtWriter<A, B> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					bRan = true;
					return b;
				});
				metadata.AddDefaultExtensionProvider (() => { 
					aRan = true;
					return a;
				});
			});
			var host = GetHostExecuteWFWithExtman (wf);
			Assert.IsTrue (bRan);
			Assert.IsFalse (aRan);
			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
			Assert.AreSame (b, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			Assert.AreSame (b, host.GetExtensions<A> ().Single ());
			Assert.AreEqual (String.Format ("{1}{2}{1}{2}", a, b, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_ParentAndSubClass ()
		{
			//only the func for the subclass runs
			A a = new A ("a");
			B b = new B ("b");
			bool aRan = false, bRan = false;
			var wf = new ExtWriter<A, B> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					aRan = true;
					return a;
				});
				metadata.AddDefaultExtensionProvider (() => { 
					bRan = true;
					return b;
				});
			});
			var host = GetHostExecuteWFWithExtman (wf);
			Assert.IsFalse (aRan);
			Assert.IsTrue (bRan);
			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
			Assert.AreSame (b, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			Assert.AreSame (b, host.GetExtensions<A> ().Single ());
			Assert.AreEqual (String.Format ("{1}{2}{1}{2}", a, b, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_ParentAndSubClass_DifferentActivities ()
		{
			//only the func for the subclass runs
			A a = new A ("a");
			B b = new B ("b");
			bool aRan = false, bRan = false;
			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					bRan = true;
					return b;
				});
			}, null);
			var wf = new ExtWriter<A, B> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					aRan = true;
					return a;
				});
				metadata.AddChild (child);
			});
			var host = GetHostExecuteWFWithExtman (wf);
			Assert.IsFalse (aRan);
			Assert.IsTrue (bRan);
			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
			Assert.AreSame (b, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			Assert.AreSame (b, host.GetExtensions<A> ().Single ());
			Assert.AreEqual (String.Format ("{1}{2}{1}{2}", a, b, Environment.NewLine), host.ConsoleOut);
		}
		#endregion
		#region check dupes with host
		[Test]
		public void Metadata_AddDefaultExtensionProvider_HostGetExtensionsStillReturnsInaccurately ()
		{
			//only B added in host remains available, func set in metadata doesnt run
			B b1 = new B ("b1"), b2 = new B ("b2"), b3 = new B ("b3");
			bool b2Ran = false;
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					b2Ran = true;
					return b2; 
				});
			}, null);
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (b1);
			extman.Add (b3);
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.IsFalse (b2Ran);
			Assert.AreEqual (2, host.GetExtensions<B> ().Count());
			Assert.AreSame (b1, host.GetExtension<B> ());
			Assert.AreEqual (1, host.GetExtensions<B> ().Count());
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_DupeWithHost_SameType ()
		{
			//only B added in host remains available, func set in metadata doesnt run
			B b1 = new B ("b1"), b2 = new B ("b2");
			bool b2Ran = false;
			var wf = new ExtWriter<A, B> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					b2Ran = true;
					return b2; 
				});
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (b1);
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.IsFalse (b2Ran);
			Assert.AreEqual (1, host.GetExtensions<B> ().Count());
			Assert.AreSame (b1, host.GetExtension<B> ());
			Assert.AreEqual (String.Format ("{0}{2}{0}{2}", b1, b2, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_DupeWithHost_SubClassAndParent ()
		{
			//only subclass extension remains available, parent func doesnt run
			B b = new B ("b");
			A a = new A ("a");
			bool aRan = false;
			var wf = new ExtWriter<A, B> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					aRan = true;
					return a;
				});
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (b);
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.IsFalse (aRan);
			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
			Assert.AreSame (b, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			Assert.AreEqual (String.Format ("{1}{2}{1}{2}", a, b, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void Metadata_AddDefaultExtensionProvider_DupeWithHost_ParentAndSubClass ()
		{
			//both extensions available
			//host.GetExtensions<object> () returns 2 objects before host/context.GetExtension<> ran
			A a = new A ("a");
			B b = new B ("b");
			bool bRan = false;
			var wf = new ExtWriter<A, B> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => { 
					bRan = true;
					return b;
				});
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (a);
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.IsTrue (bRan);
			//Assert.AreEqual (2, host.GetExtensions<object> ().Count ());
			Assert.AreSame (a, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
			Assert.AreSame (a, host.GetExtensions<A> ().Single ());
			Assert.AreEqual (1, host.GetExtensions<B> ().Count ());
			Assert.AreSame (b, host.GetExtensions<B> ().Single ());
			Assert.AreEqual (String.Format ("{0}{2}{1}{2}", a, b, Environment.NewLine), host.ConsoleOut);
		}
		#endregion
		[Test]
		[Ignore ("RequireExtensions")]
		public void Metadata_RequireExtensionOverloads ()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			var str = "str";
			var arr = new int [2];
			extman.Add (str);
			extman.Add (arr);
			var wf = new ExtWriter<string, int[]> ((metadata) => {
				metadata.RequireExtension<String> ();
				metadata.RequireExtension (typeof (int[]));
			});
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.AreEqual (String.Format ("{0}{2}{1}{2}", str, arr, Environment.NewLine), host.ConsoleOut);
		}
		[Test, ExpectedException (typeof (ValidationException))]
		[Ignore ("RequireExtensions")]
		public void Metadata_RequireExtensionT_Missing ()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (new object ());
			var wf = new ExtWriter<string, int[]> ((metadata) => {
				try {
					metadata.RequireExtension<String> ();
				} catch (Exception) {
					//shows method itself doesnt throw
				}
			});
			var host = GetHostExecuteWFWithExtman (wf, extman);
		}
		[Test, ExpectedException (typeof (ValidationException))]
		[Ignore ("RequireExtensions")]
		public void Metadata_RequireExtensionObject_Missing ()
		{
			var wf = new ExtWriter<string, int[]> ((metadata) => {
				metadata.RequireExtension<String> ();
			});
			var host = GetHostExecuteWFWithExtman (wf);
		}
		[Test]
		[Ignore ("RequireExtensions")]
		public void Metadata_RequireExtensionT_IgnoredIfNoExtensionManager ()
		{
			var wf = new ExtWriter<string, int[]> ((metadata) => {
				metadata.RequireExtension<String> ();
			});
			var host = GetHostToComplete (wf);
			//host.RegisterExtensionManager (new WorkflowInstanceExtensionManager ());
			InitRunWait (host);
		}
		#region GetExtension
		class A {
			string name;
			protected A ()
			{
			}
			public A (string name)
			{
				this.name = name;
			}
			public override string ToString ()
			{
				return name;
			}
		}
		class B : A {
			string name;
			protected B ()
			{
			}
			public B (string name)
			{
				this.name = name;
			}
			public override string ToString ()
			{
				return name;
			}
		}
		class C : B {
			string name;
			public C (string name)
			{
				this.name = name;
			}
			public override string ToString ()
			{
				return name;
			}
		}
		[Test]
		public void GetExtension_ParentAndSubclassesAsKeys ()
		{
			var b = new B ("b");
			var c = new C ("c");
			A getA = null, getB = null, getC = null;
			var wf = new NativeActivityRunner (null, (context) => {
				getA = context.GetExtension<A> ();
				getB = context.GetExtension<B> ();
				getC = context.GetExtension<C> ();
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (b);
			extman.Add (c);
			var host = GetHostExecuteWFWithExtman (wf, extman);

			Assert.AreSame (b, getA);
			Assert.AreSame (b, getB);
			Assert.AreSame (c, getC);

			Assert.AreSame (b, host.GetExtension<A> ());
			Assert.AreSame (b, host.GetExtension<B> ());
			Assert.AreSame (c, host.GetExtension<C> ());
		}
		[Test]
		public void GetExtension_NotFound ()
		{
			var a = new A ("a");
			A getA = null, getB = null, getC = null;
			var wf = new NativeActivityRunner (null, (context) => {
				getA = context.GetExtension<A> ();
				getB = context.GetExtension<B> ();
				getC = context.GetExtension<C> ();
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (a);
			var host = GetHostExecuteWFWithExtman (wf, extman);

			Assert.AreSame (a, getA);
			Assert.IsNull (getB);
			Assert.IsNull (getC);

			Assert.AreSame (a, host.GetExtension<A> ());
			Assert.IsNull (host.GetExtension<B> ());
			Assert.IsNull (host.GetExtension<C> ());
		}
		[Test]
		public void GetExtension_NoExtensionManager ()
		{
			A getA = null;
			var wf = new NativeActivityRunner (null, (context) => {
				getA = context.GetExtension<A> ();
			});
			var host = GetHostToComplete (wf);
			InitRunWait (host);
			Assert.IsNull (getA);
			Assert.IsNull (host.GetExtension<A> ());
		}
		#endregion
		[Test]
		public void GetExtensions_ParentAndSubClassesAsKeys_ReturnsAllSubclassesOfTUntilGetExtensionForTCalled1 ()
		{
			B b1 = new B ("b1"), b2 = new B ("b2");
			C c = new C ("c");
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (b1);
			extman.Add (b2);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => c);
			}, null);

			var host = new WorkflowInstanceHost (wf);
			host.RegisterExtensionManager (extman);

			Assert.AreEqual (3, host.GetExtensions<A> ().Count ());
			Assert.AreEqual (3, host.GetExtensions<B> ().Count ());
			Assert.AreEqual (1, host.GetExtensions<C> ().Count ());


			Assert.AreSame (b1, host.GetExtension<B> ());
			Assert.AreSame (c, host.GetExtension<C> ());

			Assert.AreEqual (3, host.GetExtensions<A> ().Count ());
			Assert.AreEqual (1, host.GetExtensions<B> ().Count ());
			Assert.AreEqual (1, host.GetExtensions<C> ().Count ());

			Assert.AreSame (b1, host.GetExtension<A> ());
			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
		}
		[Test]
		public void GetExtensions_ParentAndSubClassesAsKeys_ReturnsAllSubclassesOfTUntilGetExtensionForTCalled2 ()
		{
			//shows entensions added by Func are handled same as those added as objects
			//(also shows funcs executed once in process)

			B b1 = new B ("b1"), b2 = new B ("b2");
			C c = new C ("c");
			int b2Ran = 0;
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (b1);
			extman.Add (() => {
				b2Ran++;
				return b2;
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => c);
			}, null);

			var host = new WorkflowInstanceHost (wf);
			host.RegisterExtensionManager (extman);

			Assert.AreEqual (3, host.GetExtensions<A> ().Count ());
			Assert.AreEqual (3, host.GetExtensions<B> ().Count ());
			Assert.AreEqual (1, host.GetExtensions<C> ().Count ());

			Assert.AreSame (b1, host.GetExtension<A> ());

			Assert.AreEqual (1, host.GetExtensions<A> ().Count ());
			Assert.AreSame (b1, host.GetExtensions<A> ().Single ());
			Assert.AreEqual (3, host.GetExtensions<B> ().Count ());
			Assert.AreEqual (1, host.GetExtensions<C> ().Count ());

			Assert.AreEqual (1, b2Ran);
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void ExtMan_AddObject_AfterMarkReadOnlyCalledEx ()
		{
			//System.InvalidOperationException : WorkflowInstanceExtensionsManager cannot be modified once it 
			//has been associated with a WorkflowInstance.
			var extman = new WorkflowInstanceExtensionManager ();
			extman.MakeReadOnly ();
			extman.Add ("str");
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void ExtMan_AddFunc_AfterMarkReadOnlyCalledEx ()
		{
			//System.InvalidOperationException : WorkflowInstanceExtensionsManager cannot be modified once it 
			//has been associated with a WorkflowInstance.
			var extman = new WorkflowInstanceExtensionManager ();
			extman.MakeReadOnly ();
			extman.Add (() => "str");
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void ExtMan_AddObject_AfterRegistered ()
		{
			//System.InvalidOperationException : WorkflowInstanceExtensionsManager cannot be modified 
			//once it has been associated with a WorkflowInstance.
			var extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostToComplete (new WriteLine ());
			host.RegisterExtensionManager (extman);
			extman.Add ("string");
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void ExtMan_AddFunc_AfterRegistered ()
		{
			//System.InvalidOperationException : WorkflowInstanceExtensionsManager cannot be modified 
			//once it has been associated with a WorkflowInstance.
			var extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostToComplete (new WriteLine ());
			host.RegisterExtensionManager (extman);
			extman.Add (() => "string");
		}
		[Test]
		public void WFInstance_RegisterExtensionManager_NullOk ()
		{
			var host = GetHostToComplete (new WriteLine ());
			host.RegisterExtensionManager (null);
			InitRunWait (host);
		}
		[Test]
		public void WFInstance_RegisterExtensionManager_ReuseOK ()
		{
			A a = new A ("a");
			string str = "str";
			int i = 0;
			var strArr = new string [2];
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (() => { 
				i++; 
				return str; 
			});
			extman.Add (strArr);
			extman.MakeReadOnly ();
			var wf = new ExtWriter<string, string[], A> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => a);
			});
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.AreEqual (String.Format ("{0}{3}{1}{3}{2}{3}", str, strArr, a, Environment.NewLine), host.ConsoleOut);

			//can reuse extman but extensions added through metadata last time not present
			var wf2 = new ExtWriter<string, string[], A> (null);
			var host2 = GetHostExecuteWFWithExtman (wf2, extman);
			Assert.AreEqual (String.Format ("{0}{3}{1}{3}{2}{3}", str, strArr, String.Empty, Environment.NewLine), host2.ConsoleOut);

			// can still add new extensions through cachemetadata when reusing extman
			var intArr = new int[2];
			var wf3 = new ExtWriter<string, string[], int[]> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => intArr);
			});
			var host3 = GetHostExecuteWFWithExtman (wf3, extman);
			Assert.AreEqual (String.Format ("{0}{3}{1}{3}{2}{3}", str, strArr, intArr, Environment.NewLine), host3.ConsoleOut);

			Assert.AreEqual (3, i); // function providing extension from host called each time
		}
		[Test]
		public void WFInstance_RegisterExtensionManager_ExecutesFuncs ()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			var str = "String";
			bool ran = false;
			extman.Add (new Func<String> (() => { 
				ran = true; 
				return str; 
			}));
			Assert.IsFalse (ran);
			var host = GetHostToComplete (new WriteLine ());
			host.RegisterExtensionManager (extman);
			Assert.IsTrue (ran);
			Assert.AreSame (str, host.GetExtension<String> ());
		}
		[Test]
		public void WFInstance_RegisterExtensionManager_CalledAfterInitialize_OKSometimes ()
		{
			var intArr = new int[2];
			var str = "string";
			var wf = new ExtWriter<string, int[]> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => intArr);
			});

			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (str);
			var host = GetHostToComplete (wf);
			host.Initialize (null, null);
			host.RegisterExtensionManager (extman);
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
			Assert.AreEqual (String.Format ("{0}{2}{1}{2}", str,  intArr, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void WFInstance_RegisterExtensionManager_CalledAfterInitialize_IWorkflowInstanceExtensionsNotGivenInstance ()
		{
			bool getAddExt = false, metadataExecuted = false, setInst = false;
			var str = "str";
			var wie = new WorkflowInstanceExt<object> (() => { 
				getAddExt = true;
				return new List<object> { str };
			}, (instance) => {
				setInst = true;
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadataExecuted = true;
				metadata.AddDefaultExtensionProvider (() => wie);
			}, null);

			var extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostToComplete (wf);

			Assert.IsFalse (metadataExecuted, "1");
			Assert.IsFalse (getAddExt, "2");
			Assert.IsFalse (setInst, "3");
			host.Initialize (null, null);
			Assert.IsTrue (metadataExecuted, "4");
			host.RegisterExtensionManager (extman);
			Assert.IsFalse (setInst, "6");
			Assert.IsTrue (getAddExt, "7");
			Assert.AreSame (str, host.GetExtension<string> (), "8");
			Assert.IsFalse (setInst, "9");
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
			Assert.IsFalse (setInst, "10");
		}
		[Test]
		public void WFInstance_DisposeExtensions_ExtManAddFuncs ()
		{
			//exts disposed whether used or not
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (() => new Disposable ());
			extman.Add (() => new Disposable ());
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.RegisterExtensionManager (extman);
			host.DisposeExtensions ();
			Assert.AreEqual (String.Format ("Disposed{0}Disposed{0}", Environment.NewLine), host.ConsoleOut);
			Assert.AreEqual (0, host.GetExtensions<Disposable> ().Count ());
			Assert.IsNull (host.GetExtension<Disposable> ());
		}
		[Test]
		public void WFInstance_DisposeExtensions_ExtMan_Dupe_AddedByObjectAndFunc_StillDisposed ()
		{
			//exts disposed whether used or not
			var extman = new WorkflowInstanceExtensionManager ();
			var d = new Disposable ();
			extman.Add (() => d);
			extman.Add (d);
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.RegisterExtensionManager (extman);
			host.DisposeExtensions ();
			Assert.AreEqual (String.Format ("Disposed{0}", Environment.NewLine), host.ConsoleOut);
			//can't then retrieve
			Assert.AreEqual (0, host.GetExtensions<Disposable> ().Count ());
			Assert.IsNull (host.GetExtension<Disposable> ());
		}
		[Test]
		public void WFInstance_DisposeExtensions_ExtManAddObject_DisposeNotRaisedButObjectRemoved ()
		{
			var extman = new WorkflowInstanceExtensionManager ();
			var d = new Disposable ();
			extman.Add (d);
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.RegisterExtensionManager (extman);
			Assert.AreSame (d, host.GetExtension<Disposable> ());
			Assert.AreEqual (1, host.GetExtensions<Disposable> ().Count ());
			host.DisposeExtensions ();
			Assert.IsEmpty (host.ConsoleOut);
			Assert.AreEqual (0, host.GetExtensions<Disposable> ().Count ());
			Assert.IsNull (host.GetExtension<Disposable> ());
		}
		[Test]
		public void WFInstance_DisposeExtensions_MetadataAddDefaultExtensionProvider ()
		{
			//note: host neither initialized nor ran, but extensions retrieved from cachemetadata
			var extman = new WorkflowInstanceExtensionManager ();
			var wf = new ExtWriter<Disposable, string> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => new Disposable ());
			});
			var host = new WorkflowInstanceHost (wf);
			host.RegisterExtensionManager (extman);
			host.DisposeExtensions ();
			Assert.AreEqual (String.Format ("Disposed{0}", Environment.NewLine), host.ConsoleOut);
			Assert.AreEqual (0, host.GetExtensions<Disposable> ().Count ());
			Assert.IsNull (host.GetExtension<Disposable> ());
		}
		[Test]
		public void WFInstance_DisposeExtensions_IWorkflowInstanceHost_AddedByAddFuncOrMetadata_AdditionalExts_Disposed ()
		{
			var wie1 = new WorkflowInstanceExt<int> (() => { 
				return new List<object> { new Disposable (), new Disposable ()  };
			}, null);

			var wie2 = new WorkflowInstanceExt<string> (() => { 
				return new List<object> { new Disposable ()  };
			}, null);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => wie1);
			}, null);
			var host = new WorkflowInstanceHost (wf);
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (() => wie2);
			host.RegisterExtensionManager (extman);
			Assert.AreEqual (3, host.GetExtensions<Disposable> ().Count ());
			host.DisposeExtensions ();
			Assert.AreEqual (0, host.GetExtensions<Disposable> ().Count ());
			Assert.AreEqual (String.Format ("Disposed{0}Disposed{0}Disposed{0}", Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void WFInstance_DisposeExtensions_IWorkflowInstanceHost_AddedToAddObject_AdditionalExts_NotDisposed ()
		{
			var wie2 = new WorkflowInstanceExt<int> (() => { 
				return new List<object> { new Disposable () };
			}, null);

			var wie1 = new WorkflowInstanceExt<string> (() => { 
				return new List<object> { new Disposable (), wie2 };
			}, null);

			var host = new WorkflowInstanceHost (new WriteLine ());
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie1);
			host.RegisterExtensionManager (extman);
			Assert.AreEqual (2, host.GetExtensions<Disposable> ().Count ());
			host.DisposeExtensions ();
			Assert.AreEqual (0, host.GetExtensions<Disposable> ().Count ());
			Assert.AreEqual (String.Empty, host.ConsoleOut);
		}
		//FIXME: move test
		[Test]
		[Ignore ("Delay Activity")]
		public void DelayActivity ()
		{
			var wf = new Sequence { Activities = { 
					new Delay { Duration = new InArgument<TimeSpan> (TimeSpan.FromSeconds (2))},
					new WriteLine { Text = "Hello\nWorld" }
				}
			};

			var host = GetHostToComplete (wf);
			host.BeginResumeBookmark = (bookmark, value, Timeout, callback, state) => {
				var del = new Func<BookmarkResumptionResult> (() => host.Controller_ScheduleBookmarkResumption (bookmark, value));
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			host.EndResumeBookmark = (result) => {
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				result.AsyncWaitHandle.Close ();
				host.Controller_Run ();
				return retValue;
			};
			var extman = new WorkflowInstanceExtensionManager ();
			host.RegisterExtensionManager (extman); 

			host.Initialize (null, null);
			host.Controller_Run ();
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Runnable, host.Controller_State);
			Thread.Sleep (500);
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			Assert.AreEqual (String.Empty, host.ConsoleOut);
			host.AutoResetEvent.WaitOne ();
			Assert.AreEqual ("Hello\nWorld" + Environment.NewLine, host.ConsoleOut);
			Assert.AreEqual (ActivityInstanceState.Closed, host.Controller_GetCompletionState ());
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
		}
		#region IWorkflowInstanceExtensions
		//extensions are keyed by their type's' name, and only 1 per type allowed, adding unused generic type param
		//to this class means we can have multiple instances in 1 workflow as long as the type args are different
		class WorkflowInstanceExt<T> : IWorkflowInstanceExtension {
			Func<IEnumerable<object>> getAddExtAction;
			Action<WorkflowInstanceProxy> setInstanceFunc;
			public WorkflowInstanceExt (Func<IEnumerable<object>> getAddExt, Action<WorkflowInstanceProxy> setInstance)
			{
				getAddExtAction = getAddExt;
				setInstanceFunc = setInstance;
			}
			public IEnumerable<object> GetAdditionalExtensions ()
			{
				if (getAddExtAction != null)
					return getAddExtAction ();
				return null;
			}
			public void SetInstance (WorkflowInstanceProxy instance)
			{
				if (setInstanceFunc != null)
					setInstanceFunc (instance);
			}
		}
		class MyException : Exception {
		}
		[Test]
		public void IWorkflowInstanceExtensions_SingleWorkflowInstanceProxyUsed ()
		{
			WorkflowInstanceProxy proxy1 = null, proxy2 = null;
			var wie1 = new WorkflowInstanceExt<int[]> (null, (instance) => {
				proxy1 = instance;
			});
			var wie2 = new WorkflowInstanceExt<string> (null, (instance) => {
				proxy2 = instance;
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie1);
			extman.Add (wie2);
			var host = GetHostToComplete (new WriteLine ());
			host.RegisterExtensionManager (extman);
			host.Initialize (null, null);
			Assert.IsNotNull (proxy1);
			Assert.IsNotNull (proxy2);
			Assert.AreSame (proxy1, proxy2);
		}
		[Test]
		public void IWorkflowInstanceExtensions_GetSetCallOrder ()
		{
			int getOrderCounter = 0, rootMD3GetOrder = 0, rootMD1GetOrder = 0, hostObj1GetOrder = 0;
			int hostObj3GetOrder = 0, hostFunc1GetOrder = 0, hostFunc2GetOrder = 0, childMD1GetOrder = 0;
			int hostObj2_1GetOrder = 0, hostObj2_2GetOrder = 0, rootMD2_1GetOrder = 0, rootMD2_2GetOrder = 0;
			int hostObj2_WithAdditionalGetOrder = 0, rootMD2_WithAdditionalGetOrder = 0;
			int setOrderCounter = 0, rootMD3SetOrder = 0, rootMD1SetOrder = 0, hostObj1SetOrder = 0;
			int hostObj3SetOrder = 0, hostFunc1SetOrder = 0, hostFunc2SetOrder = 0, childMD1SetOrder = 0;
			int hostObj2_1SetOrder = 0, hostObj2_2SetOrder = 0, rootMD2_1SetOrder = 0, rootMD2_2SetOrder = 0;
			int hostObj2_WithAdditionalSetOrder = 0, rootMD2_WithAdditionalSetOrder = 0;

			//the generic parameter is ignored in this class, it just provides uniqueness for type
			//when instances added to extensions dictionary keyed by type
			var hostObj1 = new WorkflowInstanceExt<int []> (() => { 
				hostObj1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				hostObj1SetOrder = ++setOrderCounter;
			});
			var hostObj3 = new WorkflowInstanceExt<string []> (() => { 
				hostObj3GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				hostObj3SetOrder = ++setOrderCounter;
			});
			var rootMD1 = new WorkflowInstanceExt<double []> (() => { 
				rootMD1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				rootMD1SetOrder = ++setOrderCounter;
			});
			var rootMD3 = new WorkflowInstanceExt<DateTime []> (() => { 
				rootMD3GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				rootMD3SetOrder = ++setOrderCounter;
			});
			var hostFunc1 = new WorkflowInstanceExt<char []> (() => { 
				hostFunc1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				hostFunc1SetOrder = ++setOrderCounter;
			});
			var hostFunc2 = new WorkflowInstanceExt<decimal []> (() => { 
				hostFunc2GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				hostFunc2SetOrder = ++setOrderCounter;
			});
			var childMD1 = new WorkflowInstanceExt<bool []> (() => { 
				childMD1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				childMD1SetOrder = ++setOrderCounter;
			});
			//these next 2 will be added by another WorkflowInstanceExt
			var hostObj2_1 = new WorkflowInstanceExt<byte []> (() => { 
				hostObj2_1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				hostObj2_1SetOrder = ++setOrderCounter;
			});
			var hostObj2_2 = new WorkflowInstanceExt<short []> (() => { 
				hostObj2_2GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				hostObj2_2SetOrder = ++setOrderCounter;
			});
			var hostObj2_WithAdditional = new WorkflowInstanceExt<StringWriter> (() => { 
				hostObj2_WithAdditionalGetOrder = ++getOrderCounter;
				return new List<object> { hostObj2_1, hostObj2_2 };
			}, (instance) => {
				hostObj2_WithAdditionalSetOrder = ++setOrderCounter;
			});
			var rootMD2_1 = new WorkflowInstanceExt<uint []> (() => { 
				rootMD2_1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				rootMD2_1SetOrder = ++setOrderCounter;
			});
			var rootMD2_2 = new WorkflowInstanceExt<long []> (() => { 
				rootMD2_2GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				rootMD2_2SetOrder = ++setOrderCounter;
			});
			var rootMD2_WithAdditional = new WorkflowInstanceExt<Activity> (() => { 
				rootMD2_WithAdditionalGetOrder = ++getOrderCounter;
				return new List<object> { rootMD2_1, rootMD2_2 };
			}, (instance) => {
				rootMD2_WithAdditionalSetOrder = ++setOrderCounter;
			});

			var child = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => childMD1);
			}, null);

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => rootMD1);
				metadata.AddDefaultExtensionProvider (() => rootMD2_WithAdditional);
				metadata.AddDefaultExtensionProvider (() => rootMD3);
				metadata.AddChild (child);
			}, null);

			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (hostObj1);
			extman.Add (hostObj2_WithAdditional);
			extman.Add (() => hostFunc1);
			extman.Add (hostObj3);
			extman.Add (() => hostFunc2);
			GetHostExecuteWFWithExtman (wf, extman);
			//exts added to host processed first (note Add(Objects) processed before Add(Funcs))
			Assert.AreEqual (1, hostObj1GetOrder);
			Assert.AreEqual (2, hostObj2_WithAdditionalGetOrder);
			Assert.AreEqual (3, hostObj2_1GetOrder); //seems to be called recursively
			Assert.AreEqual (4, hostObj2_2GetOrder);
			Assert.AreEqual (5, hostObj3GetOrder);
			Assert.AreEqual (6, hostFunc1GetOrder);
			Assert.AreEqual (7, hostFunc2GetOrder);
			Assert.AreEqual (8, rootMD1GetOrder);
			Assert.AreEqual (9, rootMD2_WithAdditionalGetOrder);
			Assert.AreEqual (10, rootMD2_1GetOrder); //seems to be called recursively again
			Assert.AreEqual (11, rootMD2_2GetOrder);
			Assert.AreEqual (12, rootMD3GetOrder);
			Assert.AreEqual (13, childMD1GetOrder);

			Assert.AreEqual (1, hostObj1SetOrder);
			Assert.AreEqual (2, hostObj2_WithAdditionalSetOrder);
			Assert.AreEqual (3, hostObj3SetOrder); //this is different from the Get... order
			Assert.AreEqual (4, hostObj2_1SetOrder);
			Assert.AreEqual (5, hostObj2_2SetOrder);
			Assert.AreEqual (6, hostFunc1SetOrder);
			Assert.AreEqual (7, hostFunc2SetOrder);
			Assert.AreEqual (8, rootMD1SetOrder);
			Assert.AreEqual (9, rootMD2_WithAdditionalSetOrder);
			Assert.AreEqual (10, rootMD3SetOrder);
			Assert.AreEqual (11, childMD1SetOrder); //seems that extensions added from metadata in tree have their 
			Assert.AreEqual (12, rootMD2_1SetOrder); //Set... called before those added by Get...
			Assert.AreEqual (13, rootMD2_2SetOrder);
		}
		[Test]
		[Ignore ("MonkeyWrench")]
		public void IWorkflowInstanceExtensions_GetSetCallOrder_Nested ()
		{
			int getOrderCounter = 0, rootMD1GetOrder = 0, rootMD2GetOrder = 0, rootMD1_1GetOrder = 0;
			int rootMD1_2GetOrder = 0, rootMD1_1_1GetOrder = 0, rootMD2_1GetOrder = 0;
			int setOrderCounter = 0, rootMD1SetOrder = 0, rootMD2SetOrder = 0, rootMD1_1SetOrder = 0;
			int rootMD1_2SetOrder = 0, rootMD1_1_1SetOrder = 0, rootMD2_1SetOrder = 0;

			var rootMD2_1 = new WorkflowInstanceExt<Activity> (() => { 
				rootMD2_1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				rootMD2_1SetOrder = ++setOrderCounter;
			});

			var rootMD2 = new WorkflowInstanceExt<Activity> (() => { 
				rootMD2GetOrder = ++getOrderCounter;
				return new List<object> { rootMD2_1 };
			}, (instance) => {
				rootMD2SetOrder = ++setOrderCounter;
			});

			var rootMD1_1_1 = new WorkflowInstanceExt<int[]> (() => { 
				rootMD1_1_1GetOrder = ++getOrderCounter;
				return null;
			}, (instance) => {
				rootMD1_1_1SetOrder = ++setOrderCounter;
			});

			var rootMD1_2 = new WorkflowInstanceExt<int[]> (() => { 
				rootMD1_2GetOrder = ++getOrderCounter;
				return new List<object> { null };
			}, (instance) => {
				rootMD1_2SetOrder = ++setOrderCounter;
			});

			var rootMD1_1 = new WorkflowInstanceExt<int[]> (() => { 
				rootMD1_1GetOrder = ++getOrderCounter;
				return new List<object> { rootMD1_1_1 };
			}, (instance) => {
				rootMD1_1SetOrder = ++setOrderCounter;
			});

			var rootMD1 = new WorkflowInstanceExt<StringWriter> (() => { 
				rootMD1GetOrder = ++getOrderCounter;
				return new List<object> { rootMD1_1, rootMD1_2 };
			}, (instance) => {
				rootMD1SetOrder = ++setOrderCounter;
			});

			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => rootMD1);
				metadata.AddDefaultExtensionProvider (() => rootMD2);
			}, null);

			GetHostExecuteWFWithExtman (wf);

			Assert.AreEqual (1, rootMD1GetOrder);
			Assert.AreEqual (2, rootMD1_1GetOrder);
			Assert.AreEqual (3, rootMD1_2GetOrder);
			Assert.AreEqual (4, rootMD1_1_1GetOrder);
			Assert.AreEqual (5, rootMD2GetOrder);
			Assert.AreEqual (6, rootMD2_1GetOrder);

			Assert.AreEqual (1, rootMD1SetOrder);
			Assert.AreEqual (2, rootMD2SetOrder);
			Assert.AreEqual (3, rootMD1_1SetOrder);
			Assert.AreEqual (4, rootMD1_2SetOrder);
			Assert.AreEqual (5, rootMD1_1_1SetOrder);
			Assert.AreEqual (6, rootMD2_1SetOrder);
		}
		[Test]
		public void IWorkflowInstanceExtensions_CalledWhenExtLeftInaccessible ()
		{
			//metadata funcs to add extensions dont run if there is a dupe already present
			bool aGetCalled = false, bGetCalled = false;
			bool aSetCalled = false, bSetCalled = false;
			bool mRan = false;
			var aExt = new WorkflowInstanceExt<object> (() => { 
				aGetCalled = true;
				return null;
			}, (instance) => {
				aSetCalled = true;
			});
			var bExt = new WorkflowInstanceExt<object> (() => { 
				bGetCalled = true;
				return null;
			}, (instance) => {
				bSetCalled = true;
			});
			var cExt = new WorkflowInstanceExt<object> (() => { 
				return null;
			}, null);
			var wf = new ExtWriter<string, WorkflowInstanceExt<object>> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => {
					mRan = true;
					return cExt;
				});
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (() => aExt);
			extman.Add (() => bExt);
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.IsTrue (aGetCalled);
			Assert.IsTrue (aSetCalled);
			Assert.IsTrue (bGetCalled);
			Assert.IsTrue (bSetCalled);
			Assert.IsFalse (mRan);
		}
		[Test]
		public void IWorkflowInstanceExtensions_UnusedExtensionCanProvideUsedExt ()
		{
			string str = "str";
			var intArr = new int [2];

			var aExt = new WorkflowInstanceExt<object> (() => { 
				return null;
			}, null);
			var bExt = new WorkflowInstanceExt<object> (() => { 
				return new List<object> { str };
			}, null);
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (aExt);
			extman.Add (bExt);
			var host = GetHostExecuteWFWithExtman (new ExtWriter<string, WorkflowInstanceExt<object>> (), extman);
			Assert.AreSame (aExt, host.GetExtension<WorkflowInstanceExt<object>> ());
			Assert.AreSame (str, host.GetExtension<string> ());
			Assert.AreEqual (1, host.GetExtensions<WorkflowInstanceExt<object>> ().Count ());
			Assert.AreEqual (String.Format ("{0}{2}{1}{2}", str, aExt, Environment.NewLine), host.ConsoleOut);
		}

		[Test, ExpectedException (typeof (MyException))]
		public void IWorkflowInstanceExtensions_AddFromHost_AdditionalExtensions_ThrowsException ()
		{
			var wie = new WorkflowInstanceExt<object> (() => { 
				throw new MyException ();
			}, null);

			var host = new WorkflowInstanceHost (new WriteLine ());
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			host.RegisterExtensionManager (extman); //throws from here
		}
		[Test]
		public void IWorkflowInstanceExtensions_AddFromHost_GetAdditionalExtensionsCalledOnRegister ()
		{
			bool getAddExt = false;
			var str = "str";
			var wie = new WorkflowInstanceExt<object> (() => { 
				getAddExt = true;
				return new List<object> { str };
			}, null);
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			Assert.IsFalse (getAddExt);
			var host = new WorkflowInstanceHost (new WriteLine ());
			host.RegisterExtensionManager (extman);
			Assert.IsTrue (getAddExt);
			Assert.AreSame (str, host.GetExtension<string> ());
		}
		[Test, ExpectedException (typeof (MyException))]
		public void IWorkflowInstanceExtensions_AddFromHost_SetInstance_ThrowsException ()
		{
			var wie = new WorkflowInstanceExt<object> (null, (instance) => { 
				throw new MyException ();
			});

			var host = new WorkflowInstanceHost (new WriteLine ());
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			host.RegisterExtensionManager (extman); 
			host.Initialize (null, null); //throws from here
		}
		[Test]
		public void IWorkflowInstanceExtensions_AddFromHost_SetInstanceCalledOnInitialize ()
		{
			bool setInst = false;
			var wie = new WorkflowInstanceExt<object> (null, (instance) => {
				setInst = true;
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			var host = GetHostToComplete (new WriteLine ());
			host.RegisterExtensionManager (extman);
			Assert.IsFalse (setInst);
			host.Initialize (null, null);
			Assert.IsTrue (setInst);
		}
		[Test, ExpectedException (typeof (MyException))]
		public void IWorkflowInstanceExtensions_AddFromMetadata_AdditionalExtensions_ThrowsException ()
		{
			var wie = new WorkflowInstanceExt<object> (() => { 
				throw new MyException ();
			}, null);

			var wf = new ExtWriter<string, int[]> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => wie);
			});

			var host = new WorkflowInstanceHost (wf);
			var extman = new WorkflowInstanceExtensionManager ();
			host.RegisterExtensionManager (extman); //throws from here
		}
		[Test]
		public void IWorkflowInstanceExtensions_AddFromMetadata_GetAdditionalExtensionsCalledOnRegsiter ()
		{
			bool getAddExt = false, metadataExecuted = false;
			var str = "str";
			var wie = new WorkflowInstanceExt<object> (() => { 
				getAddExt = true;
				return new List<object> { str };
			}, null);

			var wf = new NativeActivityRunner ((metadata) => {
				metadataExecuted = true;
				metadata.AddDefaultExtensionProvider (() => wie);
			}, null);

			var extman = new WorkflowInstanceExtensionManager ();
			var host = new WorkflowInstanceHost (wf);
			Assert.IsFalse (metadataExecuted);
			Assert.IsFalse (getAddExt);
			host.RegisterExtensionManager (extman);
			Assert.IsTrue (metadataExecuted);
			Assert.IsTrue (getAddExt);
			Assert.AreSame (str, host.GetExtension<string> ());
		}
		[Test, ExpectedException (typeof (MyException))]
		public void IWorkflowInstanceExtensions_AddFromMetadata_SetInstance_ThrowsException ()
		{
			var wie = new WorkflowInstanceExt<object> (null, (instance) => { 
				throw new MyException ();
			});

			var wf = new ExtWriter<string, int[]> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => wie);
			});

			var host = new WorkflowInstanceHost (wf);
			var extman = new WorkflowInstanceExtensionManager ();
			host.RegisterExtensionManager (extman); 
			host.Initialize (null, null); //throws from here
		}
		[Test]
		public void IWorkflowInstanceExtensions_AddFromMetadata_SetInstanceCalledOnInitialize ()
		{
			bool setInst = false;
			var wie = new WorkflowInstanceExt<object> (null, (instance) => {
				setInst = true;
			});
			var wf = new NativeActivityRunner ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => wie);
			}, null);

			var extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostToComplete (wf);
			host.RegisterExtensionManager (extman);
			Assert.IsFalse (setInst);
			host.Initialize (null, null);
			Assert.IsTrue (setInst);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AvailableInWF ()
		{
			var str = "str";
			var intArr = new int[2];
			var wie = new WorkflowInstanceExt<object> (() => { 
				return new List<object> { str, intArr };
			}, null);

			var wf = new ExtWriter<string, int[]> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => wie);
			});

			var host = GetHostExecuteWFWithExtman (wf);
			Assert.AreEqual (String.Format ("{0}{2}{1}{2}", str, intArr, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_ReturnEmpty ()
		{
			var wie = new WorkflowInstanceExt<object> (() => { 
				return new List<object> ();
			}, null);

			var host = new WorkflowInstanceHost (new WriteLine ());
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			host.RegisterExtensionManager (extman);
			Assert.AreEqual (1, host.GetExtensions <object> ().Count ());
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_ReturnNull ()
		{
			var wie = new WorkflowInstanceExt<object> (() => { 
				return null;
			}, null);

			var host = new WorkflowInstanceHost (new WriteLine ());
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			host.RegisterExtensionManager (extman);
			Assert.AreEqual (1, host.GetExtensions <object> ().Count ());
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_Nested ()
		{
			bool childGetExts = false, childSetInst = false;
			bool grandChildGetExts = false, grandChildSetInst = false;
			var grandChild = new WorkflowInstanceExt<Activity> (() => { 
				grandChildGetExts = true;
				return null;
			}, (instance) => {
				grandChildSetInst = true;
			});
			var child = new WorkflowInstanceExt<StringWriter> (() => { 
				childGetExts = true;
				return new List<object> { grandChild };
			}, (instance) => {
				childSetInst = true;
			});
			var parent = new WorkflowInstanceExt<string> (() => { 
				return new List<object> { child };
			}, null);

			var wf = new ExtWriter<WorkflowInstanceExt<StringWriter>, WorkflowInstanceExt<Activity>> ((metadata) => {
				metadata.AddDefaultExtensionProvider (() => parent);
			});
			var extman = new WorkflowInstanceExtensionManager ();
			var host = GetHostExecuteWFWithExtman (wf, extman);
			Assert.IsTrue (childGetExts);
			Assert.IsTrue (childSetInst);
			Assert.IsTrue (grandChildGetExts);
			Assert.IsTrue (grandChildSetInst);
			Assert.AreEqual (String.Format ("{0}{2}{1}{2}", child, grandChild, Environment.NewLine), host.ConsoleOut);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_SameType ()
		{
			B b1 = new B ("b1"), b2 = new B ("b2");
			var host = GetHostAndAdd2ExtsFromIWFInstExtViaHost (b1, b2);
			AssertHostGetExt_2OfSameType_Added (b1, b2, host);
			AssertContextGetExt_2OfSameType_Added (b1, b2, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_SubClassAndParent ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd2ExtsFromIWFInstExtViaHost (b, a);
			AssertHostGetExt_SubClassAndParent_Added (b, a, host);
			AssertContextGetExt_SubClassAndParent_Added (b, a, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_ParentAndSubClass ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd2ExtsFromIWFInstExtViaHost (a, b);
			AssertHostGetExt_ParentAndSubClass_Added (a, b, host);
			AssertContextGetExt_ParentAndSubClass_Added (a, b, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_SameType ()
		{
			B b1 = new B ("b1"), b2 = new B ("b2");
			var host = GetHostAndAdd2ExtsFromIWFInstExtViaMetadata (b1, b2);
			AssertHostGetExt_2OfSameType_Added (b1, b2, host);
			AssertContextGetExt_2OfSameType_Added (b1, b2, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_SubClassAndParent ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd2ExtsFromIWFInstExtViaMetadata (b, a);
			AssertHostGetExt_SubClassAndParent_Added (b, a, host);
			AssertContextGetExt_SubClassAndParent_Added (b, a, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_ParentAndSubClass ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd2ExtsFromIWFInstExtViaMetadata (a, b);
			AssertHostGetExt_ParentAndSubClass_Added (a, b, host);
			AssertContextGetExt_ParentAndSubClass_Added (a, b, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_DupeWithHost_SameType ()
		{
			B b1 = new B ("b1"), b2 = new B ("b2");
			var host = GetHostAndAdd1ExtInHostAnd1FromIWFInstExtViaMetadata (b1, b2);
			AssertHostGetExt_2OfSameType_Added (b1, b2, host);
			AssertContextGetExt_2OfSameType_Added (b1, b2, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_DupeWithHost_SubClassAndParent ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd1ExtInHostAnd1FromIWFInstExtViaMetadata (b, a);
			AssertHostGetExt_SubClassAndParent_Added (b, a, host);
			AssertContextGetExt_SubClassAndParent_Added (b, a, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_DupeWithHost_ParentAndSubClass ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd1ExtInHostAnd1FromIWFInstExtViaMetadata (a, b);
			AssertHostGetExt_ParentAndSubClass_Added (a, b, host);
			AssertContextGetExt_ParentAndSubClass_Added (a, b, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_DupeWithMetadata_SameType ()
		{
			B b1 = new B ("b1"), b2 = new B ("b2");
			var host = GetHostAndAdd1ExtInMetadataAnd1FromIWFInstExtViaMetadata (b1, b2);
			AssertHostGetExt_2OfSameType_Added (b1, b2, host);
			AssertContextGetExt_2OfSameType_Added (b1, b2, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_DupeWithMetadata_SubClassAndParent ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd1ExtInMetadataAnd1FromIWFInstExtViaMetadata (b, a);
			AssertHostGetExt_SubClassAndParent_Added (b, a, host);
			AssertContextGetExt_SubClassAndParent_Added (b, a, host);
		}
		[Test]
		public void IWorkflowInstanceExtensions_AdditionalExtensions_AddedFromMetadata_DupeWithMetadata_ParentAndSubClass ()
		{
			A a = new A ("a");
			B b = new B ("b");
			var host = GetHostAndAdd1ExtInMetadataAnd1FromIWFInstExtViaMetadata (a, b);
			AssertHostGetExt_ParentAndSubClass_Added (a, b, host);
			AssertContextGetExt_ParentAndSubClass_Added (a, b, host);
		}
		#endregion
		#region WorkflowInstanceProxy
		[Test]
		public void WorkflowInstanceProxy_SuppliedWithCorrect_ID_WorkflowDefinition ()
		{
			WorkflowInstanceProxy proxy = null;
			var wie = new WorkflowInstanceExt<object> (null, (instance) => {
				proxy = instance;
			});
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			var wf = new WriteLine ();
			var host = GetHostToComplete (wf);
			host.RegisterExtensionManager (extman);
			host.Initialize (null, null);
			Assert.IsNotNull (proxy);
			Assert.AreEqual (host.Id, proxy.Id);
			Assert.AreEqual (wf, proxy.WorkflowDefinition);
		}
		[Test]
		public void WorkflowInstanceProxy_ResumeBookmark_WFIdle ()
		{
			WorkflowInstanceProxy proxy = null;
			Bookmark myBookmark = null;
			var wie = new WorkflowInstanceExt<object> (null, (instance) => {
				proxy = instance;
			});
			var wf = new NativeActivityRunner (null, (context) => {
				myBookmark = context.CreateBookmark ((ctx, bk, value) => { Console.WriteLine (value); });
			});
			wf.InduceIdle = true;
			var host = GetHostToIdleOrComplete (wf);
			host.BeginResumeBookmark = (bookmark, value, Timeout, callback, state) => {
				var del = new Func<BookmarkResumptionResult> (() => host.Controller_ScheduleBookmarkResumption (bookmark, value));
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			host.EndResumeBookmark = (result) => {
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				result.AsyncWaitHandle.Close ();
				host.Controller_Run ();
				return retValue;
			};
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (new WorkflowInstanceExt<object> (null, null));
			extman.Add (wie); // extension can still use proxy even though it wont be retrievable
			host.RegisterExtensionManager (extman); 
			Assert.AreNotSame (wie, host.GetExtension<WorkflowInstanceExt<object>>());
			host.Initialize (null, null);
			host.Controller_Run ();
			host.AutoResetEvent.WaitOne ();
			host.AutoResetEvent.Reset ();
			Assert.AreEqual (WorkflowInstanceState.Idle, host.Controller_State);
			Assert.AreEqual (ActivityInstanceState.Executing, host.Controller_GetCompletionState ());
			var asyncResult = proxy.BeginResumeBookmark (myBookmark, "resumed", null, null); //leaving callback null
			proxy.EndResumeBookmark (asyncResult); // blocks until bookmark resumed
			host.AutoResetEvent.WaitOne (); // my end resume bookmark implementation calls run again
			Assert.AreEqual (WorkflowInstanceState.Complete, host.Controller_State);
			Assert.AreEqual (ActivityInstanceState.Closed, host.Controller_GetCompletionState ());
			Assert.AreEqual ("resumed" + Environment.NewLine, host.ConsoleOut);
		}
		[Test]
		public void WorkflowInstanceProxy_ResumeBookmark_WFNotIdle ()
		{
			//Ive implemented logic to make bookmark run again in EndResumeBookmark
			WorkflowInstanceProxy proxy = null;
			Bookmark myBookmark = null;
			var wie = new WorkflowInstanceExt<object> (null, (instance) => {
				proxy = instance;
			});
			var wf = new NativeActivityRunner (null, (context) => {
				myBookmark = context.CreateBookmark ((ctx, bk, value) => { Console.WriteLine (value); });
				var asyncResult = proxy.BeginResumeBookmark (myBookmark, "resumed", (ar) => {
					proxy.EndResumeBookmark (ar);
				}, null); 
			});
			wf.InduceIdle = true;
			var host = GetHostToComplete (wf);
			host.BeginResumeBookmark = (bookmark, value, Timeout, callback, state) => {
				var del = new Func<BookmarkResumptionResult> (() => {
					return host.Controller_ScheduleBookmarkResumption (bookmark, value);
				});
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			host.EndResumeBookmark = (result) => {
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				result.AsyncWaitHandle.Close ();
				if (retValue == BookmarkResumptionResult.Success)
					host.Controller_Run ();
				return retValue;
			};
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			host.RegisterExtensionManager (extman); 
			InitRunWait (host);
			Assert.AreEqual ("resumed" + Environment.NewLine, host.ConsoleOut);
		}
		[Test]
		[Ignore ("MonkeyWrench")]
		public void WorkflowInstanceProxy_EndResumeBookmark_CalledWhenWFCantGoIdle_WHYThisException ()
		{
			//System.InvalidOperationException: The workflow runtime is currently executing a workflow and operations can only 
			//be performed while the workflow is paused.  Access to WorkflowInstance must be synchronized by the caller.
			WorkflowInstanceProxy proxy = null;
			Bookmark myBookmark = null;
			BookmarkResumptionResult br = (BookmarkResumptionResult)(-1);
			Exception exception = null;
			bool endResumeRan = false, endInvokeRan = false, postBeginResume = false, postBeginResumeWhenExecutes = false;
			var wie = new WorkflowInstanceExt<object> (null, (instance) => {
				proxy = instance;
			});
			var wf = new NativeActivityRunner (null, (context) => {
				myBookmark = context.CreateBookmark ((ctx, bk, value) => { Console.WriteLine (value); });
				var asyncResult = proxy.BeginResumeBookmark (myBookmark, "resumed", null, null); 
				postBeginResume = true;
				br = proxy.EndResumeBookmark (asyncResult);
			});
			wf.InduceIdle = true;
			var host = GetHostToIdleOrComplete (wf);
			host.NotifyUnhandledException = (ex, act, id) => {
				exception = ex;
				host.AutoResetEvent.Set ();
			};
			host.BeginResumeBookmark = (bookmark, value, Timeout, callback, state) => {
				postBeginResumeWhenExecutes = postBeginResume;
				var del = new Func<BookmarkResumptionResult> (() => {
					return host.Controller_ScheduleBookmarkResumption (bookmark, value);
				});
				var result = del.BeginInvoke (callback, state);
				return result;
			};
			host.EndResumeBookmark = (result) => {
				endResumeRan = true;
				var retValue = ((Func<BookmarkResumptionResult>)((AsyncResult)result).AsyncDelegate).EndInvoke (result);
				endInvokeRan = true;
				result.AsyncWaitHandle.Close ();
				if (retValue == BookmarkResumptionResult.Success)
					host.Controller_Run ();

				return retValue;
			};
			var extman = new WorkflowInstanceExtensionManager ();
			extman.Add (wie);
			host.RegisterExtensionManager (extman); 
			InitRunWait (host);
			Assert.AreEqual ((BookmarkResumptionResult)(-1), br);
			Assert.IsTrue (endResumeRan);
			Assert.IsFalse (endInvokeRan);
			Assert.IsFalse (postBeginResumeWhenExecutes);
			Assert.IsInstanceOfType (typeof (InvalidOperationException), exception);
		}
		#endregion
	}
}

