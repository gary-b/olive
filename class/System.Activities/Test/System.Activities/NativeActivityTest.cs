using NUnit.Framework;
using System;
using System.Activities.Statements;
using System.Activities;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class NativeActivityTest	{
		class NativeActivityMock : NativeActivity {
			new public Func<Activity> Implementation {
				get { return base.Implementation; }
				set { base.Implementation = value; }
			}
			new public bool CanInduceIdle {
				get { return base.CanInduceIdle; }
			}
			protected override void Execute (NativeActivityContext context)
			{
			}
		}

		#region Properties
		[Test]
		public void Implementation_Get ()
		{
			var nativeActivity = new NativeActivityMock ();
			Assert.IsNull (nativeActivity.Implementation);
		}
		[Test, ExpectedException (typeof (NotSupportedException))]
		public void Implementation_Set_Ex ()
		{
			var nativeActivity = new NativeActivityMock ();
			Assert.IsNull (nativeActivity.Implementation);
			nativeActivity.Implementation = () => new WriteLine ();
		}
		[Test]
		public void CanInduceIdle ()
		{
			var nativeActivity = new NativeActivityMock ();
			Assert.IsFalse (nativeActivity.CanInduceIdle);
		}
		#endregion
	}
}

