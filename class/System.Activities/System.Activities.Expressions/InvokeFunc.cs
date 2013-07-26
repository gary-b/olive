using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Activities;
using System.Activities.Debugger;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Windows.Markup;

namespace System.Activities.Expressions
{
	[ContentProperty ("Func")]
	public sealed class InvokeFunc<TResult> : NativeActivity<TResult>
	{
		public ActivityFunc<TResult> Func { get; set; }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			metadata.AddDelegate (Func);
			var rtResult = new RuntimeArgument ("Result", typeof (TResult), ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			metadata.Bind (Result, rtResult);
		}

		protected override void Execute (NativeActivityContext context)
		{
			if (Func != null)
				context.ScheduleFunc (Func, Callback);
		}

		void Callback (NativeActivityContext context, ActivityInstance completedInstance, TResult value)
		{
			context.SetValue (Result, value);
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T> Argument { get; set; }
		public ActivityFunc<T, TResult> Func { get; set; }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			metadata.AddDelegate (Func);
			var rtResult = new RuntimeArgument ("Result", typeof (TResult), ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			metadata.Bind (Result, rtResult);

			var rtArg = new RuntimeArgument ("Argument", typeof (T), ArgumentDirection.In);
			metadata.AddArgument (rtArg);
			metadata.Bind (Argument, rtArg);
		}

		protected override void Execute (NativeActivityContext context)
		{
			if (Func != null)
				context.ScheduleFunc (Func, context.GetValue (Argument), Callback);
		}

		void Callback (NativeActivityContext context, ActivityInstance completedInstance, TResult value)
		{
			context.SetValue (Result, value);
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		public ActivityFunc<T1, T2, TResult> Func { get; set; }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			metadata.AddDelegate (Func);
			var rtResult = new RuntimeArgument ("Result", typeof (TResult), ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			metadata.Bind (Result, rtResult);

			var rtArg1 = new RuntimeArgument ("Argument1", typeof (T1), ArgumentDirection.In);
			metadata.AddArgument (rtArg1);
			metadata.Bind (Argument1, rtArg1);

			var rtArg2 = new RuntimeArgument ("Argument2", typeof (T2), ArgumentDirection.In);
			metadata.AddArgument (rtArg2);
			metadata.Bind (Argument2, rtArg2);
		}

		protected override void Execute (NativeActivityContext context)
		{
			if (Func != null) {
				context.ScheduleFunc (Func, context.GetValue (Argument1), 
						      	context.GetValue (Argument2), Callback);
			}
		}

		void Callback (NativeActivityContext context, ActivityInstance completedInstance, TResult value)
		{
			context.SetValue (Result, value);
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		public ActivityFunc<T1, T2, T3, TResult> Func { get; set; }

		protected override void CacheMetadata (NativeActivityMetadata metadata)
		{
			metadata.AddDelegate (Func);
			var rtResult = new RuntimeArgument ("Result", typeof (TResult), ArgumentDirection.Out);
			metadata.AddArgument (rtResult);
			metadata.Bind (Result, rtResult);

			var rtArg1 = new RuntimeArgument ("Argument1", typeof (T1), ArgumentDirection.In);
			metadata.AddArgument (rtArg1);
			metadata.Bind (Argument1, rtArg1);

			var rtArg2 = new RuntimeArgument ("Argument2", typeof (T2), ArgumentDirection.In);
			metadata.AddArgument (rtArg2);
			metadata.Bind (Argument2, rtArg2);

			var rtArg3 = new RuntimeArgument ("Argument3", typeof (T3), ArgumentDirection.In);
			metadata.AddArgument (rtArg3);
			metadata.Bind (Argument3, rtArg3);
		}

		protected override void Execute (NativeActivityContext context)
		{
			if (Func != null) {
				context.ScheduleFunc (Func, context.GetValue (Argument1), 
							context.GetValue (Argument2), 
							context.GetValue (Argument3), Callback);
			}
		}

		void Callback (NativeActivityContext context, ActivityInstance completedInstance, TResult value)
		{
			context.SetValue (Result, value);
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T10> Argument10 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T10> Argument10 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T11> Argument11 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T10> Argument10 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T11> Argument11 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T12> Argument12 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T10> Argument10 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T11> Argument11 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T12> Argument12 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T13> Argument13 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T10> Argument10 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T11> Argument11 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T12> Argument12 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T13> Argument13 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T14> Argument14 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T10> Argument10 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T11> Argument11 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T12> Argument12 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T13> Argument13 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T14> Argument14 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T15> Argument15 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}

	[ContentProperty ("Func")]
	public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> : NativeActivity<TResult>
	{
		[RequiredArgumentAttribute]
		public InArgument<T1> Argument1 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T2> Argument2 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T3> Argument3 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T4> Argument4 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T5> Argument5 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T6> Argument6 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T7> Argument7 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T8> Argument8 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T9> Argument9 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T10> Argument10 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T11> Argument11 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T12> Argument12 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T13> Argument13 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T14> Argument14 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T15> Argument15 { get; set; }
		[RequiredArgumentAttribute]
		public InArgument<T16> Argument16 { get; set; }
		public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> Func { get; set; }

		protected override void Execute (NativeActivityContext context)
		{
			throw new NotImplementedException ();
		}
	}
}
