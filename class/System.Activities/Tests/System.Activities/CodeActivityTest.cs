using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;
using System.Collections.ObjectModel;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	[TestFixture]
	class CodeActivityTest {
		class CodeActivityMock : CodeActivity {
			new public Func<Activity> Implementation {
				get { return base.Implementation; }
				set { base.Implementation = value; }
			}

			protected override void Execute (CodeActivityContext context)
			{
				throw new NotImplementedException ();
			}
		}

		#region Properties
		[Test]
		public void Implementation_Get ()
		{
			var codeActivity = new CodeActivityMock ();
			Assert.IsNull (codeActivity.Implementation);
		}
		[Test, ExpectedException (typeof (NotSupportedException))]
		public void Implementation_SetEx ()
		{
			var codeActivity = new CodeActivityMock ();
			Assert.IsNull (codeActivity.Implementation);
			codeActivity.Implementation = () => new WriteLine ();
		}
		#endregion

		#region Methods
		/* see tests in CodeActivityMetadata
		[Test]
		public void CacheMetadata ()
		{
			
		}
		*/
		#endregion

	}
}
